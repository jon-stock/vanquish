using UnityEngine;
using UnityEngine.InputSystem;
using Vanquish.Core;
using Vanquish.Simulation.Flight;

namespace Vanquish.Combat
{
    /// <summary>
    /// Player control. Left click fires at the nearest enemy contact known to the
    /// player's team (i.e. detected either by this drone or by a scout).
    ///
    /// Supports two entirely different control schemes depending on the design's
    /// propulsion (Phase 2B), decided once at spawn time from FlightBody.isThrusting
    /// (already set correctly for this design before this component's Awake runs —
    /// see VehicleFactory.SpawnDrone/CombatPlayerLoadoutApplier), not hardcoded here:
    ///
    /// - Multirotor (requiresForwardFlight = false, e.g. electric quadcopter/
    ///   hexacopter): hovers and strafes omnidirectionally. WASD/Space/Shift map to
    ///   camera-relative acceleration via FlightBody.ApplySteering — not fixed world
    ///   axes, since the camera can be freely orbited (see Phase0ChaseCamera) and
    ///   fixed-axis "forward" became disorientating the moment the camera faced a
    ///   different direction than the world's +Z. This is the only propulsion
    ///   source, and releasing all keys actively brakes to a hover rather than
    ///   coasting.
    /// - Fixed-wing/jet (requiresForwardFlight = true): a real roll-to-turn
    ///   stick-and-throttle flight model instead of a direct yaw stick — A/D roll
    ///   (bank), W/S pitch (climb/dive), Space/Shift throttle. There's no direct yaw
    ///   input at all — turning comes from banking and pulling up into the turn, same
    ///   as a real aircraft (and most arcade flight games), which FlightBody's
    ///   angle-of-attack aerodynamic lift model (see its useAerodynamicLift) makes
    ///   physically meaningful: lift acts perpendicular to the actual relative
    ///   airflow, so banking tilts lift sideways and curves the flight path.
    ///   Rotation is driven directly via Rigidbody.MoveRotation, and
    ///   FlightBody.alignVelocityToForward is enabled (not orientToVelocity, which
    ///   is disabled here) so the flight path gradually follows wherever the player
    ///   points the nose. A first attempt reused ApplySteering's velocity-chasing
    ///   orientToVelocity model (the same one missile/AI guidance uses) with a
    ///   direct-yaw input, but that made turns too subtle/momentum-dominated to feel
    ///   responsive under direct player control, and gave no throttle at all — this
    ///   replaces that entirely. AI/missile guidance is unaffected (still uses
    ///   orientToVelocity — see VehicleFactory/InterceptorAI), since only this
    ///   component ever sets alignVelocityToForward/disables orientToVelocity, and
    ///   only for the player's own drone.
    ///
    ///   Fixed-wing flight-model rework: Space/Shift now move a real throttle lever
    ///   (FlightBody.throttleFraction, 0-1) instead of adding an ad-hoc extra force on
    ///   top of a constant baseline thrust — FlightBody's own FixedUpdate applies
    ///   thrustNewtons*throttleFraction every tick, so idling the throttle back
    ///   genuinely reduces thrust rather than just removing a bonus. Roll/pitch input
    ///   is also now scaled by ComputeControlAuthority, a function of current
    ///   airspeed: control surfaces need airflow to work, so a slow/stalled aircraft
    ///   is genuinely sluggish to control (not just slow to look at) — this is the
    ///   other half of "maneuvering works correctly," alongside FlightBody's real
    ///   stall behavior.
    /// </summary>
    [RequireComponent(typeof(FlightBody))]
    public class PlayerDroneController : MonoBehaviour
    {
        [Header("Multirotor control (requiresForwardFlight = false)")]
        public float steeringForce = 60f;

        [Tooltip("Braking acceleration applied to actively cancel velocity when no movement key is held, " +
                 "simulating a quadcopter's auto-hover rather than coasting/drifting on weak passive drag alone.")]
        public float brakingForce = 80f;

        [Header("Fixed-wing/jet control (requiresForwardFlight = true)")]
        [Tooltip("A/D roll (bank) rate, degrees/second, at full control authority (see " +
                 "controlAuthorityReferenceSpeedMetersPerSecond) — actual rate is scaled down at low airspeed.")]
        public float rollRateDegreesPerSecond = 120f;
        [Tooltip("W/S pitch rate, degrees/second, at full control authority.")]
        public float pitchRateDegreesPerSecond = 60f;
        [Tooltip("How fast Space/Shift move the throttle lever from idle (0) to full (1) and back, per second. " +
                 "1.0 means a full idle-to-full-throttle sweep takes one second.")]
        public float throttleChangeRatePerSecond = 0.6f;
        [Tooltip("Airspeed (m/s) at which roll/pitch control authority reaches 100%. Below this, control " +
                 "surfaces get progressively less effective (control authority scales with speed^2, matching " +
                 "real dynamic-pressure-dependent control effectiveness) — a slow or near-stalled aircraft " +
                 "should feel noticeably harder to steer than one at cruise speed, not just visually slower. " +
                 "A future pass could derive this from the design's own stats (e.g. a fraction of its stall " +
                 "speed) rather than one flat value shared by every fixed-wing design; left as a simple " +
                 "per-controller constant for this rework.")]
        public float controlAuthorityReferenceSpeedMetersPerSecond = 35f;

        private FlightBody _flightBody;
        private WeaponController _weapon;
        private Rigidbody _rigidbody;

        /// <summary>True for fixed-wing/jet propulsion drones — captured at spawn time from
        /// FlightBody.isThrusting, which VehicleFactory/CombatPlayerLoadoutApplier already set
        /// correctly from the design's PropulsionDefinition.requiresForwardFlight.</summary>
        private bool _isFixedWingStyle;

        private void Awake()
        {
            _flightBody = GetComponent<FlightBody>();
            _isFixedWingStyle = _flightBody.isThrusting;
            if (!_isFixedWingStyle)
            {
                // Multirotor: player input is the only propulsion source, matching
                // every prior Phase 1/2 build's quadcopter behavior exactly (explicit
                // assignment kept, even though VehicleFactory/CombatPlayerLoadoutApplier
                // should already have this false for multirotor propulsion, so this
                // component's own behavior doesn't silently depend on that elsewhere).
                _flightBody.isThrusting = false;
            }
            else
            {
                // Fixed-wing/jet, player-piloted: drive rotation directly (roll/pitch
                // stick input below) instead of FlightBody's velocity-chasing
                // orientToVelocity, which felt unresponsive under direct player
                // control — disable that and enable the inverse (flight path follows
                // the nose) instead. isThrusting/useAerodynamicLift stay as
                // VehicleFactory configured them — FlightBody's own FixedUpdate keeps
                // applying the airframe's constant baseline thrust and lift every
                // tick; this component only adds throttle/rotation on top of that.
                _flightBody.orientToVelocity = false;
                _flightBody.alignVelocityToForward = true;
            }
            _weapon = GetComponent<WeaponController>();
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (_isFixedWingStyle)
            {
                FixedUpdateFixedWing();
            }
            else
            {
                FixedUpdateMultirotor();
            }
        }

        private void FixedUpdateMultirotor()
        {
            Vector2 rawInput = Vector2.zero;
            float verticalInput = 0f;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed) rawInput += Vector2.up;
                if (kb.sKey.isPressed) rawInput += Vector2.down;
                if (kb.aKey.isPressed) rawInput += Vector2.left;
                if (kb.dKey.isPressed) rawInput += Vector2.right;
                if (kb.spaceKey.isPressed) verticalInput += 1f;
                if (kb.leftShiftKey.isPressed) verticalInput -= 1f;
            }

            Vector3 input = CameraRelativeDirection(rawInput) + Vector3.up * verticalInput;

            if (input.sqrMagnitude > 0.001f)
            {
                _flightBody.ApplySteering(input.normalized * steeringForce);
            }
            else if (_rigidbody.linearVelocity.sqrMagnitude > 0.01f)
            {
                // Active hover-hold: cancel current velocity rather than coasting on
                // weak passive drag alone, which otherwise made the drone feel
                // sluggish/wallowy — it would keep drifting for a long time after
                // releasing movement keys.
                Vector3 brakeDirection = -_rigidbody.linearVelocity.normalized;
                float brakeMagnitude = Mathf.Min(brakingForce, _rigidbody.linearVelocity.magnitude / Time.fixedDeltaTime);
                _flightBody.ApplySteering(brakeDirection * brakeMagnitude);
            }
        }

        /// <summary>
        /// Roll-to-turn stick-and-throttle control: A/D roll (bank), W/S pitch
        /// (climb/dive), Space/Shift throttle. Deliberately no direct yaw input —
        /// turning comes from banking + pulling into the turn, same as a real
        /// aircraft; FlightBody's angle-of-attack lift model is what makes banking
        /// actually curve the flight path (lift redirects with the banked "up" axis),
        /// combined with alignVelocityToForward pulling the velocity vector to follow
        /// the (rolled+pitched) nose over time. Rotation is driven directly via
        /// Rigidbody.MoveRotation rather than going through FlightBody.ApplySteering,
        /// since ApplySteering's job (velocity-chasing orientToVelocity + a lateral
        /// force) is designed for missile/AI guidance, not direct player stick
        /// input, and orientToVelocity was explicitly disabled for the player in
        /// Awake() so it doesn't fight this.
        /// </summary>
        private void FixedUpdateFixedWing()
        {
            float rollInput = 0f;
            float pitchInput = 0f;
            float throttleInput = 0f;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.dKey.isPressed) rollInput += 1f; // D: roll/bank right
                if (kb.aKey.isPressed) rollInput -= 1f; // A: roll/bank left
                if (kb.wKey.isPressed) pitchInput += 1f; // W: nose up / climb
                if (kb.sKey.isPressed) pitchInput -= 1f; // S: nose down / dive
                // Fix (direct user feedback: "throttle is backwards: shift should be
                // up"): swapped from the multirotor scheme's Space=up/Shift=down
                // (which makes sense for vertical strafing) — a fixed-wing throttle
                // lever is a different control than vertical movement, and Shift
                // conventionally maps to "more power" in flight sims.
                if (kb.leftShiftKey.isPressed) throttleInput += 1f;
                if (kb.spaceKey.isPressed) throttleInput -= 1f;
            }

            // Real throttle lever (FlightBody.throttleFraction) instead of an ad-hoc
            // extra force on top of constant thrust — see this field's own tooltip and
            // the class doc comment. FlightBody's FixedUpdate is what actually applies
            // thrustNewtons*throttleFraction every tick; this just moves the lever.
            if (throttleInput != 0f)
            {
                _flightBody.throttleFraction = Mathf.Clamp01(
                    _flightBody.throttleFraction + throttleInput * throttleChangeRatePerSecond * Time.fixedDeltaTime);
            }

            if (rollInput != 0f || pitchInput != 0f)
            {
                // Control authority scales with airspeed — a stalled/slow aircraft is
                // genuinely sluggish to steer, not just slow to look at. See
                // ComputeControlAuthority's own doc comment.
                float authority = ComputeControlAuthority(_rigidbody.linearVelocity.magnitude, controlAuthorityReferenceSpeedMetersPerSecond);
                Quaternion roll = Quaternion.AngleAxis(-rollInput * rollRateDegreesPerSecond * authority * Time.fixedDeltaTime, transform.forward);
                Quaternion pitch = Quaternion.AngleAxis(-pitchInput * pitchRateDegreesPerSecond * authority * Time.fixedDeltaTime, transform.right);
                _rigidbody.MoveRotation(roll * pitch * _rigidbody.rotation);
            }
        }

        /// <summary>
        /// Control-surface effectiveness as a function of airspeed: 0 at zero speed,
        /// rising to 1 at referenceSpeed and clamped there (no "super-effective"
        /// controls above cruise speed — a simplification vs. real aircraft, which
        /// can actually over-control at very high speed, but not a concern at this
        /// game's speed range). Scales with speed^2 (matching dynamic pressure,
        /// q = 0.5*rho*v^2, the real physical driver of control-surface effectiveness)
        /// rather than linearly, so the drop-off near stall speed is sharp rather than
        /// gradual — a near-stalled aircraft should feel noticeably harder to
        /// wrestle, not just a little softer. Pure function — headlessly testable.
        /// </summary>
        public static float ComputeControlAuthority(float speedMetersPerSecond, float referenceSpeedMetersPerSecond)
        {
            if (referenceSpeedMetersPerSecond <= 0.01f)
                return 1f;
            float ratio = speedMetersPerSecond / referenceSpeedMetersPerSecond;
            return Mathf.Clamp01(ratio * ratio);
        }

        /// <summary>
        /// Converts raw WASD input (y = forward/back, x = left/right) into a
        /// world-space direction relative to the current camera's facing, flattened
        /// onto the horizontal plane so camera pitch doesn't tilt movement into the
        /// ground/sky.
        /// </summary>
        private static Vector3 CameraRelativeDirection(Vector2 rawInput)
        {
            Camera cam = Camera.main;
            if (cam == null)
                return new Vector3(rawInput.x, 0f, rawInput.y);

            Vector3 camForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;

            return camForward * rawInput.y + camRight * rawInput.x;
        }

        private void Update()
        {
            // Fire on left mouse click rather than Space, since Space is used for
            // altitude/throttle depending on flight model (see FixedUpdate above).
            if (_weapon == null || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
                return;

            var target = TeamAwareness.Instance != null
                ? TeamAwareness.Instance.GetNearestKnownEnemy(Team.Player, transform.position)
                : null;

            if (target != null)
                _weapon.Fire(target.transform);
        }
    }
}
