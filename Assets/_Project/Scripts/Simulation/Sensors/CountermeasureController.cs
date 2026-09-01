using UnityEngine;

namespace Vanquish.Simulation.Sensors
{
    /// <summary>
    /// Decoy/flare-chaff defense against an inbound missile's lock, per PLAN.md
    /// Phase 2C's countermeasure decoys item: "give a currently-locked missile a
    /// chance to break lock ... needs a 'counter-fire' player/AI action and a check
    /// in the guidance/seeker update loop." Added by VehicleFactory to any drone
    /// whose DroneLoadout carries an (optional) CountermeasureDefinition — decoy
    /// equipment logically belongs to the unit defending against a missile, not the
    /// missile itself, so this reuses the same CountermeasureDefinition part type
    /// already defined for MissileLoadout's own (RCS/IR/maxG-focused) countermeasure
    /// slot rather than inventing a parallel data type.
    ///
    /// The "AI action" half of that requirement is TryAutoDeployDecoy, checked from
    /// GuidanceController.FixedUpdate against any unit it's currently locked onto —
    /// automatic self-defense, needing no player input, so AI-controlled drones
    /// benefit too. TryDeployDecoy is exposed separately for a future manual
    /// player-triggered "pop countermeasures now" key bind (not wired to an input
    /// binding yet — out of scope for this pass, but the API is ready for it).
    /// </summary>
    public class CountermeasureController : MonoBehaviour
    {
        [Tooltip("Remaining decoy charges — each deploy attempt (successful or not) consumes one.")]
        public int decoyChargesRemaining;

        [Range(0f, 1f)]
        [Tooltip("Probability a single deployed decoy successfully spoofs the inbound lock.")]
        public float decoySuccessChance;

        [Tooltip("An inbound missile must be within this range before auto-defense considers deploying " +
            "a decoy against it — no point popping flares at a missile still far away.")]
        public float threatRangeMeters = 600f;

        [Tooltip("Minimum time between automatic decoy deployments, so a single lingering missile " +
            "doesn't burn through every charge in one continuous engagement.")]
        public float autoDeployCooldownSeconds = 3f;

        private float _autoDeployCooldownTimer;

        private void Update()
        {
            if (_autoDeployCooldownTimer > 0f)
                _autoDeployCooldownTimer -= Time.deltaTime;
        }

        /// <summary>
        /// Attempts to deploy one decoy charge. Always consumes a charge if any remain
        /// (a used decoy is used, win or lose) and rolls decoySuccessChance to decide
        /// whether it actually spoofs the lock. Returns true if the lock should break.
        /// </summary>
        public bool TryDeployDecoy()
        {
            if (decoyChargesRemaining <= 0)
                return false;

            decoyChargesRemaining--;
            return Random.value < decoySuccessChance;
        }

        /// <summary>
        /// Auto-defense entry point for GuidanceController: only actually attempts a
        /// deploy (and starts the cooldown) if the cooldown has elapsed, so this is
        /// safe to call every physics tick a missile is locked on without needing the
        /// caller to track timing itself.
        /// </summary>
        public bool TryAutoDeployDecoy()
        {
            if (_autoDeployCooldownTimer > 0f)
                return false;

            _autoDeployCooldownTimer = autoDeployCooldownSeconds;
            return TryDeployDecoy();
        }
    }
}
