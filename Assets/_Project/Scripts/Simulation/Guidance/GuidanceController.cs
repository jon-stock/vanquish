using UnityEngine;
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
        /// successfully spoofed this missile.
        /// </summary>
        private bool CheckCountermeasureBreaksLock()
        {
            var countermeasures = target.GetComponent<CountermeasureController>();
            if (countermeasures == null)
                return false;

            float distance = Vector3.Distance(transform.position, target.position);
            if (distance > countermeasures.threatRangeMeters)
                return false;

            if (!countermeasures.TryAutoDeployDecoy())
                return false;

            Debug.Log($"[Guidance] {name}'s lock on {target.name} broken by a decoy countermeasure.");
            SetTarget(null);
            return true;
        }
    }
}
