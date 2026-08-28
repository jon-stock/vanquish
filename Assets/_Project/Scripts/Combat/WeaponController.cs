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

        [Tooltip("Local offset from the drone's origin that missiles spawn from, e.g. slightly ahead/below.")]
        public Vector3 launchOffset = new Vector3(0f, -0.3f, 1f);

        private float _cooldownTimer;

        public bool CanFire => ammoRemaining > 0 && _cooldownTimer <= 0f;

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
            Debug.Log($"[Combat] {ownerTeam} '{name}' fired at '{target.name}' (ammo left: {ammoRemaining}) t={Time.time:F1}s");
            return true;
        }
    }
}
