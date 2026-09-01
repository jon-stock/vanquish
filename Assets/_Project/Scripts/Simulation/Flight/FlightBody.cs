using UnityEngine;

namespace Vanquish.Simulation.Flight
{
    /// <summary>
    /// Flight physics component shared by missiles and drones. Always applies thrust
    /// along the current facing direction (while isThrusting) and a simple quadratic
    /// drag force opposing velocity. Missiles and multirotor drones use just that —
    /// no lift/AoA model, deliberately simplified, sufficient to validate that
    /// data-driven mass/thrust/drag stats produce believable flight. Fixed-wing/jet
    /// drones additionally get a real (if simplified) aerodynamic model — see
    /// useAerodynamicLift — since a constant-thrust-only body can't stay airborne
    /// against gravity the way an aircraft's wings do.
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
        [Tooltip("Whether this body is affected by gravity. Multirotor drones and missiles leave this " +
                 "false (no lift model, and missiles are always under active thrust/guidance for their " +
                 "whole flight so gravity would just be an extra force to compensate for with no benefit). " +
                 "Fixed-wing/jet drones enable this via the lift-aware Configure overload below, since " +
                 "useAerodynamicLift gives them a real force to counteract it with.")]
        public bool useGravity = false;

        [Header("Flight Model")]
        [Tooltip("Plane/rocket-style bodies (missiles, fixed-wing/jet drones) always orient their nose to face " +
                 "current velocity. Quadcopter/multirotor-style bodies (electric propeller drones) are " +
                 "omnidirectional — they hover and strafe freely without needing to face their direction of " +
                 "travel, so this should be false for them.")]
        public bool orientToVelocity = true;

        [Header("Runtime State")]
        public bool isThrusting = true;

        [Header("Aerodynamic Lift (fixed-wing/jet only)")]
        [Tooltip("Enables a simplified lift model: a force along transform.up, scaling with speed^2 and " +
            "liftCoefficient, applied every FixedUpdate. Off (false, the default) for missiles and " +
            "multirotor drones — missiles are thrust/steering-vectored and don't need lift, and " +
            "multirotors get vertical lift for free from vectored thrust (see the class comment); this is " +
            "specifically the 'real aerodynamic model' Phase 1's simplification note above said Phase 2 " +
            "would need for fixed-wing/jet drones. useGravity is force-enabled alongside this (there's no " +
            "point counteracting gravity with lift if gravity is off) and force-disabled when this is off.")]
        public bool useAerodynamicLift = false;

        [Tooltip("Lift coefficient — from the design's wing part (WingOrPropellerDefinition.liftCoefficient). " +
            "Deliberately no separate tunable 'stall speed': lift = liftCoefficient * speed^2 already falls " +
            "off steeply at low speed on its own (quadratic in speed), producing a natural, ungimmicked " +
            "stall/nose-drop when flying too slowly to generate enough lift to counteract gravity, without " +
            "an extra magic-number threshold that could get out of sync with mass/thrust/drag.")]
        public float liftCoefficient = 1f;

        [Header("Player-Piloted Fixed-Wing Only")]
        [Tooltip("When true, applies a damping force against any velocity component perpendicular to " +
            "transform.forward, so the flight path gradually follows wherever the nose is pointed (driven " +
            "directly by player input — see PlayerDroneController) rather than the reverse. This is the " +
            "opposite relationship from orientToVelocity (nose chases velocity, used by missile/AI " +
            "guidance) — never enable both true at once, they fight for control of the same relationship. " +
            "Represents a real aircraft's aerodynamic tendency to fly the direction it's pointed rather " +
            "than slip sideways indefinitely; without this, direct rotation control (as a player expects) " +
            "would let the plane skid sideways forever whenever its heading doesn't match its momentum.")]
        public bool alignVelocityToForward = false;

        [Tooltip("How strongly alignVelocityToForward damps sideways/vertical-relative-to-nose velocity. " +
            "Higher = flight path snaps to the nose direction faster (more arcade-y); lower = more drift/slip.")]
        public float velocityAlignmentStrength = 2f;

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
        /// Missile/multirotor spawn code should keep using this overload — it leaves
        /// useAerodynamicLift off (and useGravity off), unchanged from before this
        /// overload existed. Fixed-wing/jet drone spawn code should use the other
        /// Configure overload below instead, which also engages the lift model.
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
        /// Fixed-wing/jet drone variant of Configure: also engages the aerodynamic lift
        /// model and enables gravity (there's no reason to fly against gravity without
        /// lift to counteract it, and no reason to have lift with gravity off).
        /// </summary>
        public void Configure(float mass, float thrust, float drag, float maxG, float liftCoeff)
        {
            Configure(mass, thrust, drag, maxG);
            useAerodynamicLift = true;
            liftCoefficient = liftCoeff;
            useGravity = true;
            if (_rigidbody != null)
                _rigidbody.useGravity = true;
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
            // child visual object, never baked into this rotation. Quadcopter-style
            // bodies skip this entirely (see orientToVelocity) since they're expected
            // to strafe/hover omnidirectionally without spinning to face travel direction.
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

            Vector3 velocity = _rigidbody.linearVelocity;
            float speed = velocity.magnitude;

            // Simple quadratic drag opposing current velocity.
            if (speed > 0.01f)
            {
                Vector3 dragForce = -velocity.normalized * (dragCoefficient * speed * speed);
                _rigidbody.AddForce(dragForce, ForceMode.Force);
            }

            // Simplified aerodynamic lift: a force along transform.up, quadratic in
            // speed. Deliberately uncapped at the low end (see liftCoefficient's
            // tooltip re: natural stall behavior) but clamped at the high end to
            // maxGForce so a fast jet doesn't generate an unbounded climb force —
            // same physical limit ApplySteering already respects for lateral maneuvers.
            if (useAerodynamicLift)
            {
                float liftMagnitude = liftCoefficient * speed * speed;
                float maxLiftForce = maxGForce * GRAVITY_MPS2 * _rigidbody.mass;
                liftMagnitude = Mathf.Min(liftMagnitude, maxLiftForce);
                _rigidbody.AddForce(transform.up * liftMagnitude, ForceMode.Force);
            }

            // Player-piloted fixed-wing: damp velocity components that don't match the
            // (player-controlled) nose direction, so the flight path gradually follows
            // wherever the nose is pointed instead of the plane skidding sideways
            // indefinitely. See this field's tooltip for why this is the inverse of
            // orientToVelocity rather than a variant of it.
            if (alignVelocityToForward && speed > 0.01f)
            {
                Vector3 forwardComponent = Vector3.Project(velocity, transform.forward);
                Vector3 lateralComponent = velocity - forwardComponent;
                _rigidbody.AddForce(-lateralComponent * velocityAlignmentStrength * _rigidbody.mass, ForceMode.Force);
            }
        }
    }
}
