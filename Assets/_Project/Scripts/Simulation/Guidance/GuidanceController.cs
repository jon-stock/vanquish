using UnityEngine;
using Vanquish.Combat;
using Vanquish.Simulation.Flight;
using Vanquish.Simulation.Sensors;

namespace Vanquish.Simulation.Guidance
{
    /// <summary>
    /// Drives a FlightBody's steering using a pluggable IGuidanceLaw against a target
    /// transform. Phase 0 prototype: target is assigned directly rather than resolved
    /// via a seeker/sensor system (that arrives in Phase 2 alongside detection).
    ///
    /// Phase 2C: also checks the locked target for a CountermeasureController each
    /// tick (see that class's doc comment) — if the target auto-deploys a decoy and
    /// it successfully spoofs this missile, the lock breaks (target set to null) and
    /// guidance stops correcting, letting the missile fly straight/ballistic from
    /// whatever heading it had — the concrete, testable form of "jamming/
    /// countermeasures visibly affect whether a shot connects" from Phase 2C's exit
    /// criteria.
    /// </summary>
    [RequireComponent(typeof(FlightBody))]
    public class GuidanceController : MonoBehaviour
    {
        public Transform target;

        [Tooltip("If true, uses PursuitGuidance by default. Replace via SetGuidanceLaw for other laws once implemented.")]
        public bool useDefaultPursuit = true;

        [Tooltip("Depth pass (direct user feedback: 'the basic missile always hits the target'): before this " +
            "existed, the terminal guidance law ran unconditionally every tick regardless of range — a seeker's " +
            "own detectionRangeMeters/fieldOfViewDegrees were computed and stored on MissileRuntimeStats but " +
            "never actually gated whether guidance could see/correct toward the target at all. Set from the " +
            "missile's own seeker stats at spawn (VehicleFactory) — defaults to 'unconstrained' (infinite " +
            "range, full circle) so anything that doesn't explicitly configure these (tests, older code paths) " +
            "keeps the old unconditional-homing behavior rather than silently going ballistic.")]
        public float seekerRangeMeters = float.PositiveInfinity;

        [Tooltip("Half-angle of the seeker's detection cone, in degrees (matches SeekerDefinition." +
            "fieldOfViewDegrees's own convention) — outside this cone off the nose, the seeker can't see the " +
            "target at all this tick, same real consequence real IR/radar/optical seekers have: a target that " +
            "maneuvers hard enough to get outside the seeker's cone (especially once out near seekerRangeMeters, " +
            "where angular tracking is hardest) breaks the shot.")]
        public float seekerFieldOfViewDegrees = 180f;

        [Tooltip("SeekerDefinition.countermeasureSusceptibility (0-1) — how easily THIS specific seeker is " +
            "spoofed by a decoy, factored into the defending unit's own decoySuccessChance so seeker quality " +
            "genuinely matters against countermeasures (a Multi-Spectral seeker shrugs off the same flare a " +
            "basic IR seeker falls for). Defaults to 1 (fully susceptible, i.e. the old behavior where only " +
            "the defender's own decoySuccessChance mattered) for anything that doesn't explicitly set it.")]
        [Range(0f, 1f)]
        public float countermeasureSusceptibility = 1f;

        private FlightBody _flightBody;
        private IGuidanceLaw _guidanceLaw;
        private Rigidbody _targetRigidbody;

        private void Awake()
        {
            _flightBody = GetComponent<FlightBody>();
            if (useDefaultPursuit)
                _guidanceLaw = new PursuitGuidance();
        }

        private void Start()
        {
            if (target != null)
                _targetRigidbody = target.GetComponent<Rigidbody>();
        }

        public void SetGuidanceLaw(IGuidanceLaw law) => _guidanceLaw = law;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            _targetRigidbody = target != null ? target.GetComponent<Rigidbody>() : null;
        }

        private void FixedUpdate()
        {
            if (_guidanceLaw == null || target == null)
                return;

            if (CheckCountermeasureBreaksLock())
                return;

            if (!SeekerCanSeeTarget())
                return; // out of range or off-boresight this tick — flies ballistic, no correction

            Vector3 targetVelocity = _targetRigidbody != null ? _targetRigidbody.linearVelocity : Vector3.zero;
            Vector3 selfVelocity = GetComponent<Rigidbody>().linearVelocity;

            Vector3 steering = _guidanceLaw.ComputeSteering(
                transform.position,
                selfVelocity,
                target.position,
                targetVelocity,
                Time.fixedDeltaTime);

            _flightBody.ApplySteering(steering);
        }

        /// <summary>
        /// Queries the current target for a CountermeasureController and, if it's
        /// within threat range and its auto-defense cooldown has elapsed, lets it
        /// attempt a decoy deploy. Returns true (and breaks the lock) if the decoy
        /// successfully spoofed this missile — weighted by this missile's own
        /// seeker's countermeasureSusceptibility, not just the defender's raw
        /// decoySuccessChance (see that field's own tooltip).
        /// </summary>
        private bool CheckCountermeasureBreaksLock()
        {
            var countermeasures = target.GetComponent<CountermeasureController>();
            if (countermeasures == null)
                return false;

            float distance = Vector3.Distance(transform.position, target.position);
            if (distance > countermeasures.threatRangeMeters)
                return false;

            if (!countermeasures.TryAutoDeployDecoy(countermeasureSusceptibility))
                return false;

            Debug.Log($"[Guidance] {name}'s lock on {target.name} broken by a decoy countermeasure.");
            CountermeasureVisualEffect.SpawnFlareBurst(target.position);
            SetTarget(null);
            return true;
        }

        /// <summary>
        /// Range + field-of-view gate — see seekerRangeMeters/seekerFieldOfViewDegrees'
        /// own tooltips for why this exists. Pure geometry against the missile's
        /// current nose direction (transform.forward), not the guidance law's
        /// internal state, so it applies uniformly regardless of which IGuidanceLaw
        /// is active.
        /// </summary>
        private bool SeekerCanSeeTarget()
        {
            Vector3 toTarget = target.position - transform.position;
            float distance = toTarget.magnitude;
            if (distance > seekerRangeMeters)
                return false;

            if (seekerFieldOfViewDegrees >= 180f || distance < 0.01f)
                return true;

            float angleOffBoresight = Vector3.Angle(transform.forward, toTarget);
            return angleOffBoresight <= seekerFieldOfViewDegrees;
        }
    }
}
