using UnityEngine;

namespace Vanquish.Simulation.Flight
{
    /// <summary>
    /// Phase 0/1 flight physics component shared by missiles and drones. Applies
    /// thrust along the current facing direction (while isThrusting) and a simple
    /// quadratic drag force opposing velocity. Deliberately simplified (no lift/AoA
    /// model) — sufficient for tier 0/1 multirotor drones and unguided/pursuit
    /// missiles; a full aerodynamic model for fixed-wing/jet designs is future work.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class FlightBody : MonoBehaviour
    {
        [Header("Data-Driven Stats (populated from part definitions at spawn time)")]
        [Tooltip("Total assembled mass in kg — set from summed part masses, mirrors Rigidbody.mass.")]
        public float massKg = 50f;

        [Tooltip("Thrust force in Newtons, applied along transform.forward while thrusting.")]
        public float thrustNewtons = 500f;

        [Tooltip("Quadratic drag coefficient — force = dragCoefficient * speed^2, opposing velocity.")]
        public float dragCoefficient = 0.05f;

        [Tooltip("Maximum lateral acceleration in G the airframe can sustain (from airframe part).")]
        public float maxGForce = 20f;

        [Header("Gravity")]
        [Tooltip(
            "Whether this body is affected by gravity. Multirotor drones leave this false — " +
            "player/AI-commanded acceleration (ApplySteering) is the only force acting on them " +
            "besides drag, matching a real multirotor's auto-hover (no gravity to fight, no thrust " +
            "to balance against it). Missiles typically also leave this false — they're under " +
            "active thrust/guidance for their whole flight, so gravity would just be an extra force " +
            "to compensate for with no gameplay benefit at this simplified tier.")]
        public bool useGravity = false;

        [Header("Flight Model")]
        [Tooltip(
            "Missile/plane-style bodies orient their nose to face current velocity (via ApplySteering). " +
            "Multirotor-style bodies are omnidirectional — they hover and strafe freely without needing " +
            "to face their direction of travel, so this should be false for them (a separate purely-visual " +
            "tilt component can still bank the model for readability — see QuadcopterTiltVisual).")]
        public bool orientToVelocity = true;

        [Header("Runtime State")]
        public bool isThrusting = true;

        private Rigidbody _rigidbody;

        private const float GRAVITY_MPS2 = 9.81f;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.mass = massKg;
            _rigidbody.useGravity = useGravity;
        }

        /// <summary>
        /// Call once at spawn to (re)configure this body from assembled design stats.
        /// </summary>
        public void Configure(float mass, float thrust, float drag, float maxG, bool gravity = false, bool orientToVel = true)
        {
            massKg = mass;
            thrustNewtons = thrust;
            dragCoefficient = drag;
            maxGForce = maxG;
            useGravity = gravity;
            orientToVelocity = orientToVel;
            if (_rigidbody != null)
            {
                _rigidbody.mass = massKg;
                _rigidbody.useGravity = useGravity;
            }
        }

        /// <summary>
        /// Apply a steering acceleration (world space), clamped to maxGForce.
        /// Guidance laws (missiles) and direct player input (multirotor drones) both
        /// call this each tick with their desired acceleration.
        /// </summary>
        public void ApplySteering(Vector3 desiredAcceleration)
        {
            float maxAccel = maxGForce * GRAVITY_MPS2;
            Vector3 clamped = Vector3.ClampMagnitude(desiredAcceleration, maxAccel);
            _rigidbody.AddForce(clamped * _rigidbody.mass, ForceMode.Force);

            // Align facing with velocity direction for missiles/fixed-wing drones.
            // Quadcopter-style bodies skip this (orientToVelocity = false) since
            // they're expected to strafe/hover omnidirectionally without spinning to
            // face travel direction.
            if (orientToVelocity && _rigidbody.linearVelocity.sqrMagnitude > 0.25f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_rigidbody.linearVelocity.normalized, Vector3.up);
                _rigidbody.MoveRotation(Quaternion.RotateTowards(_rigidbody.rotation, targetRotation, 180f * Time.fixedDeltaTime));
            }
        }

        private void FixedUpdate()
        {
            if (isThrusting)
            {
                _rigidbody.AddForce(transform.forward * thrustNewtons, ForceMode.Force);
            }

            // Simple quadratic drag opposing current velocity.
            Vector3 velocity = _rigidbody.linearVelocity;
            float speed = velocity.magnitude;
            if (speed > 0.01f)
            {
                Vector3 dragForce = -velocity.normalized * (dragCoefficient * speed * speed);
                _rigidbody.AddForce(dragForce, ForceMode.Force);
            }
        }
    }
}
