using UnityEngine;
using Vanquish.Simulation.Flight;
using Vanquish.Simulation.Guidance;

namespace Vanquish.Combat
{
    /// <summary>
    /// Phase 1 scout drone behavior: flies a slow patrol loop within the arena. Its
    /// value comes entirely from its DetectionSensor (long range, sharesContactsWithTeam)
    /// feeding TeamAwareness — the player's strike drone and HUD radar see anything
    /// the scout finds, without needing to close the distance themselves.
    /// </summary>
    [RequireComponent(typeof(FlightBody))]
    [RequireComponent(typeof(Rigidbody))]
    public class ScoutPatrol : MonoBehaviour
    {
        public float patrolRadius = 350f;
        public float patrolArrivalRadius = 25f;
        public Vector3 arenaCenter = Vector3.zero;

        private FlightBody _flightBody;
        private Rigidbody _rigidbody;
        private readonly PursuitGuidance _guidance = new PursuitGuidance { steeringGain = 200f };
        private Vector3 _patrolTarget;

        private void Awake()
        {
            _flightBody = GetComponent<FlightBody>();
            _rigidbody = GetComponent<Rigidbody>();
            PickNewPatrolPoint();
        }

        private void FixedUpdate()
        {
            if (Vector3.Distance(transform.position, _patrolTarget) < patrolArrivalRadius)
                PickNewPatrolPoint();

            Vector3 steering = _guidance.ComputeSteering(
                transform.position, _rigidbody.linearVelocity,
                _patrolTarget, Vector3.zero, Time.fixedDeltaTime);
            _flightBody.ApplySteering(steering);
        }

        private void PickNewPatrolPoint()
        {
            Vector2 offset = Random.insideUnitCircle * patrolRadius;
            _patrolTarget = arenaCenter + new Vector3(offset.x, 0f, offset.y);
        }
    }
}
