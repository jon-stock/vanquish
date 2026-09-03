using UnityEngine;

namespace Vanquish.Simulation.Flight
{
    /// <summary>
    /// Flight physics component shared by missiles and drones. Always applies thrust
    /// along the current facing direction (while isThrusting) and a simple quadratic
    /// drag force opposing velocity. Missiles and multirotor drones use just that —
    /// no lift/AoA model, deliberately simplified, sufficient to validate that
    /// data-driven mass/thrust/drag stats produce believable flight. Fixed-wing/jet
    /// drones additionally get a real angle-of-attack-driven aerodynamic model — see
    /// useAerodynamicLift — since a constant-thrust-only body can't stay airborne
    /// against gravity the way an aircraft's wings do, and a naive "lift = speed^2"
    /// model (this class's previous fixed-wing implementation) can't produce a real
    /// stall or let banking curve the flight path correctly.
    ///
    /// Fixed-wing flight-model rework (see PLAN.md's "Fixed-Wing Flight Model Rework"
    /// sub-milestone): the previous version of this class computed fixed-wing lift as
    /// a flat `liftCoefficient * speed^2` along transform.up regardless of attitude —
    /// no angle of attack, no stall, and (for the player specifically, whose nose
    /// direction is NOT locked to velocity via orientToVelocity) no physical reason a
    /// steep climb should ever lose lift. This version computes a real angle of
    /// attack (the angle between the nose and the actual velocity vector, in the
    /// pitch plane) each tick and looks up a lift-curve factor from it (see
    /// ComputeLiftFactor) — flying at the wing's tuned referenceAoADegrees produces
    /// exactly liftCoefficient*speed^2 of lift (unchanged from before at that one
    /// angle), but AoA above criticalAoADegrees now genuinely stalls (lift collapses)
    /// and AoA below zeroLiftAoADegrees now genuinely produces negative lift
    /// (downforce), matching a real airfoil. For AI/missile-style bodies
    /// (orientToVelocity = true), the nose is kept aligned to velocity every tick, so
    /// AoA stays near zero and this mostly behaves like the old flat model — the real
    /// difference is entirely for the player's manually-piloted attitude, which is
    /// exactly where "maneuvering works correctly" (stalls when it should, turns via
    /// banking rather than skidding) actually matters.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class FlightBody : MonoBehaviour
    {
        [Header("Data-Driven Stats (populated from part definitions at spawn time)")]
        [Tooltip("Total assembled mass in kg — set from summed part masses, mirrors Rigidbody.mass.")]
        public float massKg = 50f;

        [Tooltip("Thrust force in Newtons at full throttle, applied along transform.forward while thrusting " +
                 "(scaled by throttleFraction — see below).")]
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

        [Tooltip("Throttle lever, 0 (idle, no thrust) to 1 (full rated thrustNewtons). Defaults to 1 so " +
            "missiles/AI-controlled drones (which never touch this) behave exactly as before this field " +
            "existed — full constant thrust whenever isThrusting is true. PlayerDroneController's fixed-wing " +
            "control scheme is the one thing that actually varies this at runtime, replacing the old " +
            "\"add an extra ad-hoc force on top of constant thrust\" hack with a real throttle lever.")]
        [Range(0f, 1f)]
        public float throttleFraction = 1f;

        [Header("Aerodynamic Lift (fixed-wing/jet only)")]
        [Tooltip("Enables the angle-of-attack aerodynamic model (lift + induced drag, computed every " +
            "FixedUpdate from the current AoA — see ComputeLiftFactor). Off (false, the default) for " +
            "missiles and multirotor drones — missiles are thrust/steering-vectored and don't need lift, and " +
            "multirotors get vertical lift for free from vectored thrust (see the class comment). useGravity " +
            "is force-enabled alongside this and force-disabled when this is off.")]
        public bool useAerodynamicLift = false;

        [Tooltip("Lift coefficient from the design's wing part (WingOrPropellerDefinition.liftCoefficient). " +
            "Tuned such that flying at exactly referenceAoADegrees produces liftCoefficient*speed^2 of lift " +
            "(ComputeLiftFactor returns 1 at that angle) — see WingOrPropellerDefinition's own tooltips.")]
        public float liftCoefficient = 1f;

        [Tooltip("Angle of attack (degrees) at which this design's wing generates zero lift. See " +
            "WingOrPropellerDefinition.zeroLiftAoADegrees.")]
        public float zeroLiftAoADegrees = -2f;

        [Tooltip("Angle of attack (degrees) liftCoefficient was tuned at. See WingOrPropellerDefinition.referenceAoADegrees.")]
        public float referenceAoADegrees = 5f;

        [Tooltip("Angle of attack (degrees) beyond which the wing stalls. See WingOrPropellerDefinition.criticalAoADegrees.")]
        public float criticalAoADegrees = 16f;

        [Tooltip("Lift-induced drag factor. See WingOrPropellerDefinition.inducedDragFactor.")]
        public float inducedDragFactor = 0.02f;

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

        /// <summary>Angle of attack computed on the most recent FixedUpdate, in degrees. Read-only —
        /// exposed purely for telemetry/HUD readouts (e.g. the fixed-wing prototype rig's overlay) and
        /// headless inspection; FixedUpdate is the only writer.</summary>
        public float CurrentAngleOfAttackDegrees { get; private set; }

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
        /// Fixed-wing/jet drone variant of Configure: also engages the angle-of-attack
        /// aerodynamic lift model and enables gravity (there's no reason to fly against
        /// gravity without lift to counteract it, and no reason to have lift with
        /// gravity off). Throttle starts at full (1) — a constant-cruise-thrust default
        /// matching every non-player (AI/missile-style) body's behavior; only
        /// PlayerDroneController's fixed-wing control scheme varies it after spawn.
        /// </summary>
        public void Configure(float mass, float thrust, float drag, float maxG, float liftCoeff,
            float zeroLiftAoA, float referenceAoA, float criticalAoA, float inducedDrag)
        {
            Configure(mass, thrust, drag, maxG);
            useAerodynamicLift = true;
            liftCoefficient = liftCoeff;
            zeroLiftAoADegrees = zeroLiftAoA;
            referenceAoADegrees = referenceAoA;
            criticalAoADegrees = criticalAoA;
            inducedDragFactor = inducedDrag;
            throttleFraction = 1f;
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

        /// <summary>
        /// Angle of attack: the signed angle, in the aircraft's pitch plane, between
        /// the nose (forward) and the actual velocity vector — i.e. how far the nose is
        /// pitched above (positive) or below (negative) the direction the aircraft is
        /// actually moving. Sideslip (yaw mismatch between nose and velocity) is
        /// deliberately excluded by projecting velocity onto the forward/up plane
        /// first, matching the standard aerospace definition of AoA (as distinct from
        /// sideslip angle, which this simplified model doesn't separately track). Pure
        /// function — headlessly testable without a Rigidbody/scene; sign convention
        /// verified in Phase3GFixedWingValidation.ValidateAngleOfAttackSign.
        /// </summary>
        public static float ComputeAngleOfAttackDegrees(Vector3 forward, Vector3 right, Vector3 velocity)
        {
            if (velocity.sqrMagnitude < 0.01f)
                return 0f;

            Vector3 velocityInPitchPlane = Vector3.ProjectOnPlane(velocity, right);
            if (velocityInPitchPlane.sqrMagnitude < 0.0001f)
                return 0f;

            return Vector3.SignedAngle(forward, velocityInPitchPlane.normalized, right);
        }

        /// <summary>
        /// Simplified lift-curve lookup: returns a multiplier on liftCoefficient*speed^2
        /// as a function of the current angle of attack. Returns exactly 1 at
        /// referenceAoADegrees (where the wing part's liftCoefficient was tuned), rises
        /// linearly above/below that toward criticalAoADegrees, and beyond
        /// criticalAoADegrees (in either direction — see the negative-side mirroring
        /// below) collapses toward a lower post-stall plateau over an additional 10
        /// degrees rather than instantly to zero, matching how a real airfoil's lift
        /// curve actually behaves post-stall (a partial, not total, loss of lift). Pure
        /// function — headlessly testable, no Rigidbody/scene required.
        /// </summary>
        public static float ComputeLiftFactor(float aoaDegrees, float zeroLiftAoADegrees, float referenceAoADegrees,
            float criticalAoADegrees)
        {
            const float postStallFalloffRangeDegrees = 10f;
            const float postStallPlateauFraction = 0.35f;

            float aoaSpan = Mathf.Max(0.01f, referenceAoADegrees - zeroLiftAoADegrees);
            float slopePerDegree = 1f / aoaSpan;

            // Mirror the critical angle onto the negative side around zeroLiftAoADegrees
            // rather than around 0 — e.g. zeroLift=-2, reference=5, critical=16 gives a
            // negative-side stall onset at -2-(16-5) = -13. Simplification: real airfoils
            // usually stall earlier (in magnitude) on the negative/inverted side than the
            // positive side, but a symmetric mirror is a reasonable Phase-appropriate
            // approximation and keeps this a 3-input curve instead of 4.
            float negativeStallOnset = zeroLiftAoADegrees - (criticalAoADegrees - referenceAoADegrees);

            if (aoaDegrees > criticalAoADegrees)
            {
                float peakFactor = (criticalAoADegrees - zeroLiftAoADegrees) * slopePerDegree;
                float overshoot = aoaDegrees - criticalAoADegrees;
                float falloff = Mathf.Lerp(1f, postStallPlateauFraction, Mathf.Clamp01(overshoot / postStallFalloffRangeDegrees));
                return peakFactor * falloff;
            }

            if (aoaDegrees < negativeStallOnset)
            {
                float peakFactor = (negativeStallOnset - zeroLiftAoADegrees) * slopePerDegree;
                float overshoot = negativeStallOnset - aoaDegrees;
                float falloff = Mathf.Lerp(1f, postStallPlateauFraction, Mathf.Clamp01(overshoot / postStallFalloffRangeDegrees));
                return peakFactor * falloff;
            }

            return (aoaDegrees - zeroLiftAoADegrees) * slopePerDegree;
        }

        private void FixedUpdate()
        {
            if (isThrusting)
            {
                _rigidbody.AddForce(transform.forward * (thrustNewtons * throttleFraction), ForceMode.Force);
            }

            Vector3 velocity = _rigidbody.linearVelocity;
            float speed = velocity.magnitude;
            float speedSquared = speed * speed;

            if (useAerodynamicLift)
            {
                // Real angle-of-attack-driven lift + induced drag — see the class doc
                // comment and ComputeLiftFactor for what changed vs. the old flat model.
                CurrentAngleOfAttackDegrees = ComputeAngleOfAttackDegrees(transform.forward, transform.right, velocity);
                float liftFactor = ComputeLiftFactor(CurrentAngleOfAttackDegrees, zeroLiftAoADegrees, referenceAoADegrees, criticalAoADegrees);

                if (speed > 0.01f)
                {
                    // Lift acts perpendicular to the actual relative airflow (velocity),
                    // not perpendicular to the fuselage — projecting transform.up onto
                    // the plane perpendicular to velocity keeps this correct at
                    // significant AoA/sideslip and (critically) is what makes banking
                    // redirect lift sideways and curve the flight path into a coordinated
                    // turn, rather than lift always just fighting gravity regardless of
                    // bank angle.
                    Vector3 liftDirection = Vector3.ProjectOnPlane(transform.up, velocity).normalized;
                    if (liftDirection.sqrMagnitude < 0.001f)
                        liftDirection = transform.up;

                    float maxLiftForce = maxGForce * GRAVITY_MPS2 * _rigidbody.mass;
                    float liftMagnitude = Mathf.Clamp(liftCoefficient * speedSquared * liftFactor, -maxLiftForce, maxLiftForce);
                    _rigidbody.AddForce(liftDirection * liftMagnitude, ForceMode.Force);

                    // Parasite drag (existing convention, from airframe+wing dragCoefficient)
                    // plus lift-induced drag — proportional to liftFactor^2, so hard
                    // maneuvering/high-AoA flight costs real speed, same as a real aircraft
                    // bleeding energy in a hard turn.
                    float totalDragCoefficient = dragCoefficient + inducedDragFactor * liftFactor * liftFactor;
                    Vector3 dragForce = -velocity.normalized * (totalDragCoefficient * speedSquared);
                    _rigidbody.AddForce(dragForce, ForceMode.Force);
                }
            }
            else
            {
                CurrentAngleOfAttackDegrees = 0f;

                // Simple quadratic drag opposing current velocity — missiles/multirotors,
                // unchanged from before this rework.
                if (speed > 0.01f)
                {
                    Vector3 dragForce = -velocity.normalized * (dragCoefficient * speedSquared);
                    _rigidbody.AddForce(dragForce, ForceMode.Force);
                }
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
