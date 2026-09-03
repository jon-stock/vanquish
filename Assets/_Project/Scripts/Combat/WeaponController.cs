using UnityEngine;
using Vanquish.Core;
using Vanquish.Data.Missiles;

namespace Vanquish.Combat
{
    /// <summary>
    /// Lives on a drone; holds its missile loadout/ammo and spawns missiles via
    /// VehicleFactory when fired. Both the player's manual fire input and enemy AI's
    /// engage-state logic call Fire() the same way.
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        public MissileLoadout missileLoadout;
        public int ammoRemaining = 4;
        public float fireCooldownSeconds = 2.5f;
        public Team ownerTeam = Team.Player;

        [Tooltip("Depth pass (direct user feedback: \"the craft should actually get more missiles, with " +
            "multiple being able to be in flight at once with the right missile tech (seekers)\"): how many " +
            "missiles fired from THIS weapon can be independently guided in the air simultaneously — " +
            "set from the missile's seeker type at spawn time (see VehicleFactory.ComputeMaxConcurrentInFlight). " +
            "A wire/SARH/laser-guided round needs the launcher's continuous guidance/illumination for its " +
            "whole flight (effectively 1 at a time); a true fire-and-forget seeker (active radar, imaging IR, " +
            "multi-spectral) needs nothing further from the launcher, so several can fly at once. Before this " +
            "existed, nothing capped concurrent missiles at all — cooldown/ammo were the only gates, so any " +
            "seeker could already have unlimited missiles in the air simultaneously as long as ammo/cooldown allowed.")]
        public int maxConcurrentInFlight = 1;

        [Tooltip("Local offset from the drone's origin that missiles spawn from, e.g. slightly ahead/below.")]
        public Vector3 launchOffset = new Vector3(0f, -0.3f, 1f);

        private float _cooldownTimer;
        private int _inFlightCount;

        public bool CanFire => ammoRemaining > 0 && _cooldownTimer <= 0f && _inFlightCount < maxConcurrentInFlight;

        /// <summary>
        /// Phase 3B: raised after a successful Fire() (ammoRemaining already
        /// decremented). MountedMissileVisuals listens to this to remove one visibly
        /// mounted missile from the drone's hardpoints per shot, so the model's
        /// visible missile count stays in sync with actual remaining ammo instead of
        /// always showing a full rack.
        /// </summary>
        public event System.Action OnFired;

        private void Update()
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;
        }

        public bool Fire(Transform target)
        {
            if (!CanFire || missileLoadout == null || !missileLoadout.IsComplete)
                return false;

            Vector3 spawnPos = transform.TransformPoint(launchOffset);
            GameObject missile = VehicleFactory.SpawnMissile(missileLoadout, spawnPos, transform.rotation, target, ownerTeam);

            // The missile spawns very close to (and can overlap) its own launching
            // drone's collider. Our damage logic already ignores friendly fire, but
            // Unity's physics solver doesn't know that — without this, it applies a
            // separation impulse on the next physics step that visibly sends both the
            // missile and the drone that fired it tumbling.
            var missileCollider = missile.GetComponent<Collider>();
            var ownCollider = GetComponent<Collider>();
            if (missileCollider != null && ownCollider != null)
                Physics.IgnoreCollision(missileCollider, ownCollider, true);

            ammoRemaining--;
            _cooldownTimer = fireCooldownSeconds;
            _inFlightCount++;
            missile.AddComponent<MissileLifecycleNotifier>().owner = this;

            Debug.Log($"[Combat] {ownerTeam} '{name}' fired at '{target.name}' (ammo left: {ammoRemaining}, " +
                $"{_inFlightCount}/{maxConcurrentInFlight} in flight) t={Time.time:F1}s");
            OnFired?.Invoke();
            return true;
        }

        /// <summary>Called by MissileLifecycleNotifier when a missile this weapon fired is destroyed
        /// (hit, or any other future despawn reason) — frees up a concurrent-in-flight slot.</summary>
        internal void NotifyMissileResolved()
        {
            _inFlightCount = Mathf.Max(0, _inFlightCount - 1);
        }
    }

    /// <summary>Tiny tracker added to every spawned missile so its owning WeaponController's
    /// concurrent-in-flight count is freed up whenever the missile GameObject is destroyed,
    /// regardless of why (impact today; any future despawn reason later) — see
    /// WeaponController.maxConcurrentInFlight's own tooltip.</summary>
    public class MissileLifecycleNotifier : MonoBehaviour
    {
        public WeaponController owner;

        private void OnDestroy()
        {
            if (owner != null)
                owner.NotifyMissileResolved();
        }
    }
}
