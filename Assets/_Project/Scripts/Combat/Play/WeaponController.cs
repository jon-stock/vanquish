using System;
using UnityEngine;
using Vanquish.Combat;

namespace Vanquish.Combat.Play
{
    /// <summary>
    /// Lives on a player/AI-controlled drone; fires missiles at a target. Ammo is
    /// NOT tracked locally — it reads/spends directly from the owning
    /// <see cref="EngagementController"/>'s attacker <see cref="Stockpile"/>, so the
    /// visual/flight layer and the Phase 0/1 stockpile-economy logic share one
    /// source of truth instead of two ammo counters that could drift apart. Damage/
    /// payload values for the spawned missile are likewise read straight from that
    /// stockpile entry's rawDamage/payloadSize (see StockpileEntry's own comments on
    /// why those live there for now).
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        public EngagementController engagementController;
        public string missilePartId;
        public Transform target;
        public float fireCooldownSeconds = 2.5f;

        [Tooltip("Local offset from the drone's origin that missiles spawn from, e.g. slightly ahead/below.")]
        public Vector3 launchOffset = new Vector3(0f, -0.3f, 1f);

        private float _cooldownTimer;

        /// <summary>Raised after a successful Fire() — HUD/visual code can listen to update ammo readouts.</summary>
        public event Action OnFired;

        public bool CanFire =>
            _cooldownTimer <= 0f &&
            engagementController != null &&
            engagementController.Attacker != null &&
            engagementController.Attacker.CanCommit(missilePartId);

        private void Update()
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;
        }

        /// <summary>Fire at this weapon's currently assigned <see cref="target"/>.</summary>
        public bool Fire() => Fire(target);

        public bool Fire(Transform fireTarget)
        {
            if (!CanFire || fireTarget == null)
                return false;

            StockpileEntry entry = engagementController.Attacker.GetEntry(missilePartId);
            if (entry == null)
                return false;

            if (!engagementController.TryCommitAttackerUnit(missilePartId))
                return false;

            Vector3 spawnPos = transform.TransformPoint(launchOffset);
            GameObject missile = MissileFactory.SpawnMissile(spawnPos, transform.rotation, fireTarget, entry.rawDamage, entry.payloadSize);

            // The missile spawns very close to (and can overlap) its own launching
            // drone's collider. Without this, Unity's physics solver applies a
            // separation impulse on the next physics step that sends both the
            // missile and the drone that fired it tumbling.
            var missileCollider = missile.GetComponent<Collider>();
            var ownCollider = GetComponent<Collider>();
            if (missileCollider != null && ownCollider != null)
                Physics.IgnoreCollision(missileCollider, ownCollider, true);

            _cooldownTimer = fireCooldownSeconds;
            OnFired?.Invoke();
            return true;
        }
    }
}
