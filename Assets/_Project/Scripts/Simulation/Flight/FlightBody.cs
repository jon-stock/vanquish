using UnityEngine;

namespace Vanquish.Simulation.Flight
{
    /// <summary>
    /// Phase 0 prototype flight physics component shared by missiles and drones.
    /// Applies thrust along the current facing direction and a simple quadratic drag
    /// force opposing velocity. Deliberately simplified (no lift/AoA model yet) —
    /// sufficient to validate that data-driven mass/thrust/drag stats produce
    /// believable flight before investing in a full aerodynamic model in Phase 2.
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
        [Tooltip("Whether this body is affected by gravity. Since this prototype has no lift/AoA model yet, " +
                 "unpowered lift-dependent bodies should leave this false until Phase 2's aerodynamic model " +
                 "can actually counteract gravity; ballistic/rocket-style bodies can enable it.")]
        public bool useGravity = false;

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
        public void Configure(float mass, float thrust, float drag, float maxG)
        {
            massKg = mass;
            thrustNewtons = thrust;
            dragCoefficient = drag;
            maxGForce = maxG;
            if (_rigidbody != null)
                _rigidbody.mass = massKg;
        }

        /// <summary>
        /// Apply a steering acceleration (world space), clamped to maxGForce.
        /// Guidance laws call this each tick with their computed steering vector.
        /// </summary>
        public void ApplySteering(Vector3 desiredAcceleration)
        {
            float maxAccel = maxGForce * GRAVITY_MPS2;
            Vector3 clamped = Vector3.ClampMagnitude(desiredAcceleration, maxAccel);
            _rigidbody.AddForce(clamped * _rigidbody.mass, ForceMode.Force);

            // Align facing with velocity direction for missiles/fixed-wing drones. This
            // transform's rotation IS the true physics heading (thrust applies along
            // transform.forward) — any model/mesh-only orientation quirks belong on a
            // child visual object, never baked into this rotation.
            if (_rigidbody.linearVelocity.sqrMagnitude > 0.25f)
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
