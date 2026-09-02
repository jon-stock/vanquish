using UnityEngine;
using Vanquish.Combat;
using Vanquish.Simulation.Flight;
using Vanquish.Simulation.Guidance;
using Vanquish.Simulation.Sensors;

namespace Vanquish.AI
{
    public enum PatrolEngageState
    {
        Patrol,
        Engage,
    }

    /// <summary>
    /// Phase 2D: shared patrol -> acquire-target -> engage loop for drone-based CPU
    /// archetypes. Movement/firing plumbing (steer via PursuitGuidance + FlightBody,
    /// fire the weapon once in range, wander a random waypoint while no target is
    /// known) is identical across archetypes — only *which* contact to chase differs.
    /// Factoring that shared plumbing out here means each archetype is a small concrete
    /// subclass that only overrides AcquireTarget's targeting policy, instead of either
    /// (a) one shared MonoBehaviour branching on an archetype enum — the "mega-
    /// controller with branching modes" this sub-milestone's own technical note warns
    /// against — or (b) every new archetype re-implementing identical boilerplate, which
    /// is exactly what the original Phase 1 EnemyDroneAI/ScoutPatrol split did (and
    /// which InterceptorAI itself did until this refactor, before ScoutHunterAI made the
    /// duplication concrete instead of hypothetical). Each archetype remains its own
    /// distinct, GetComponent-distinguishable MonoBehaviour type.
    /// </summary>
    [RequireComponent(typeof(FlightBody))]
    [RequireComponent(typeof(Rigidbody))]
    public abstract class DroneCombatAI : MonoBehaviour
    {
        public float patrolRadius = 300f;
        public float engageRangeMeters = 400f;
        public float patrolArrivalRadius = 20f;
        public Vector3 arenaCenter = Vector3.zero;

        public PatrolEngageState CurrentState { get; private set; } = PatrolEngageState.Patrol;

        protected FlightBody Flight { get; private set; }
        protected Rigidbody Body { get; private set; }
        protected WeaponController Weapon { get; private set; }

        private readonly PursuitGuidance _guidance = new PursuitGuidance();
        private Vector3 _patrolTarget;

        protected virtual void Awake()
        {
            Flight = GetComponent<FlightBody>();
            Body = GetComponent<Rigidbody>();
            Weapon = GetComponent<WeaponController>();
            PickNewPatrolPoint();
        }

        protected virtual void FixedUpdate()
        {
            DetectableSignature target = AcquireTarget();

            Vector3 desiredPosition;
            if (target != null)
            {
                CurrentState = PatrolEngageState.Engage;
                desiredPosition = target.Position;

                float distance = Vector3.Distance(transform.position, desiredPosition);
                if (Weapon != null && distance <= engageRangeMeters && Weapon.CanFire)
                    Weapon.Fire(target.transform);
            }
            else
            {
                CurrentState = PatrolEngageState.Patrol;
                if (Vector3.Distance(transform.position, _patrolTarget) < patrolArrivalRadius)
                    PickNewPatrolPoint();
                desiredPosition = _patrolTarget;
            }

            Vector3 steering = _guidance.ComputeSteering(
                transform.position, Body.linearVelocity,
                desiredPosition, Vector3.zero, Time.fixedDeltaTime);
            Flight.ApplySteering(steering);
        }

        /// <summary>
        /// Archetype-specific target-selection policy — the one thing that actually
        /// differs between archetypes built on this base class. Return null to patrol
        /// instead of engaging (e.g. no known contact matches this archetype's role
        /// preference at all).
        /// </summary>
        protected abstract DetectableSignature AcquireTarget();

        private void PickNewPatrolPoint()
        {
            Vector2 offset = Random.insideUnitCircle * patrolRadius;
            _patrolTarget = arenaCenter + new Vector3(offset.x, 0f, offset.y);
        }
    }
}
