using UnityEngine;
using Vanquish.Simulation.Flight;

namespace Vanquish.Simulation.Guidance
{
    /// <summary>
    /// Drives a FlightBody's steering using a pluggable IGuidanceLaw against a target
    /// transform. Phase 0 prototype: target is assigned directly rather than resolved
    /// via a seeker/sensor system (that arrives in Phase 2 alongside detection).
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
    }
}
