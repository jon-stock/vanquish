using UnityEngine;
using Vanquish.Core;
using Vanquish.Simulation.Flight;
using Vanquish.Simulation.Guidance;
using Vanquish.Simulation.Sensors;

namespace Vanquish.Combat
{
    public enum EnemyAIState
    {
        Patrol,
        Engage,
    }

    /// <summary>
    /// Minimal Phase 1 CPU opponent: patrols random points within the arena until its
    /// team becomes aware of a player unit (via its own sensor or, in later phases, a
    /// shared enemy scout), then closes in and fires when in range. Reuses
    /// PursuitGuidance as a plain steering law rather than as a full GuidanceController
    /// component, since the "target" here is a moving waypoint or a live contact,
    /// not a fixed Transform assigned once at spawn.
    /// </summary>
    [RequireComponent(typeof(FlightBody))]
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyDroneAI : MonoBehaviour
    {
        public float patrolRadius = 300f;
        public float engageRangeMeters = 400f;
        public float patrolArrivalRadius = 20f;
        public Vector3 arenaCenter = Vector3.zero;

        public EnemyAIState CurrentState { get; private set; } = EnemyAIState.Patrol;

        private FlightBody _flightBody;
        private Rigidbody _rigidbody;
        private WeaponController _weapon;
        private readonly PursuitGuidance _guidance = new PursuitGuidance();
        private Vector3 _patrolTarget;

        private void Awake()
        {
            _flightBody = GetComponent<FlightBody>();
            _rigidbody = GetComponent<Rigidbody>();
            _weapon = GetComponent<WeaponController>();
            PickNewPatrolPoint();
        }

        private void FixedUpdate()
        {
            DetectableSignature enemyContact = TeamAwareness.Instance != null
                ? TeamAwareness.Instance.GetNearestKnownEnemy(Team.Enemy, transform.position)
                : null;

            Vector3 desiredPosition;
            if (enemyContact != null)
            {
                CurrentState = EnemyAIState.Engage;
                desiredPosition = enemyContact.Position;

                float distance = Vector3.Distance(transform.position, desiredPosition);
                if (_weapon != null && distance <= engageRangeMeters && _weapon.CanFire)
                    _weapon.Fire(enemyContact.transform);
            }
            else
            {
                CurrentState = EnemyAIState.Patrol;
                if (Vector3.Distance(transform.position, _patrolTarget) < patrolArrivalRadius)
                    PickNewPatrolPoint();
                desiredPosition = _patrolTarget;
            }

            Vector3 steering = _guidance.ComputeSteering(
                transform.position, _rigidbody.linearVelocity,
                desiredPosition, Vector3.zero, Time.fixedDeltaTime);
            _flightBody.ApplySteering(steering);
        }

        private void PickNewPatrolPoint()
        {
            Vector2 offset = Random.insideUnitCircle * patrolRadius;
            _patrolTarget = arenaCenter + new Vector3(offset.x, 0f, offset.y);
        }
    }
}
