using UnityEngine;

namespace Vanquish.Simulation.Flight
{
    /// <summary>
    /// Altitude-hold autopilot implementing PLAN.md Phase 2B's altitude control modes
    /// (Subsystem Design Deep Dive §5) via ICommandReceiver: AbsoluteMSL holds a fixed
    /// world Y; RelativeAGL holds Y_ground + desiredAltitude using GroundSampler's
    /// downward raycast (which falls back to the flat y=0 placeholder ground every
    /// current scene builder uses, per this class's own technical note in PLAN.md,
    /// until Phase 2E's real terrain exists). Applies its correction as a vertical
    /// steering force through FlightBody.ApplySteering — the same force-application
    /// path player input (PlayerDroneController) and AI guidance (InterceptorAI) already
    /// use — clamped to maxClimbRateMetersPerSecond so a large altitude error doesn't
    /// snap the craft upward instantly.
    ///
    /// Opt-in component: does not run automatically on every drone. A future AI
    /// archetype (Phase 2D) or scripted loiter/waypoint objective can add this
    /// alongside FlightBody to get altitude-hold without reimplementing the climb-rate
    /// math, while PlayerDroneController's manual Space/Shift vertical control remains
    /// unchanged for the player's own drone.
    /// </summary>
    [RequireComponent(typeof(FlightBody))]
    [RequireComponent(typeof(Rigidbody))]
    public class AltitudeController : MonoBehaviour, ICommandReceiver
    {
        [Header("Altitude Command")]
        public AltitudeMode mode = AltitudeMode.RelativeAGL;
        public float desiredAltitudeMeters = 50f;

        [Header("Climb-Rate Limiting")]
        [Tooltip("Maximum vertical speed the airframe will command while closing an altitude error, m/s.")]
        public float maxClimbRateMetersPerSecond = 10f;

        [Tooltip("How aggressively vertical acceleration closes the gap between current and desired vertical " +
            "speed — higher settles faster but risks overshoot/oscillation.")]
        public float verticalAccelGain = 4f;

        private FlightBody _flightBody;
        private Rigidbody _rigidbody;

        private void Awake()
        {
            _flightBody = GetComponent<FlightBody>();
            _rigidbody = GetComponent<Rigidbody>();
        }

        /// <summary>ICommandReceiver: issue a new altitude command, taking effect next FixedUpdate.</summary>
        public void SetAltitudeCommand(float desiredAltitude, AltitudeMode altitudeMode)
        {
            desiredAltitudeMeters = desiredAltitude;
            mode = altitudeMode;
        }

        /// <summary>
        /// Resolves a desired altitude command to an absolute world-space target Y.
        /// Pure function (no Physics/MonoBehaviour dependency) so it's headlessly
        /// testable without a scene — see Phase2BValidation.
        /// </summary>
        public static float ComputeTargetWorldAltitude(AltitudeMode mode, float desiredAltitudeMeters, float groundHeightMeters)
        {
            return mode == AltitudeMode.RelativeAGL ? groundHeightMeters + desiredAltitudeMeters : desiredAltitudeMeters;
        }

        /// <summary>
        /// Computes the vertical acceleration to command this tick: converts the
        /// current height error into a desired vertical speed (clamped to
        /// maxClimbRateMetersPerSecond — the climb-rate limiting Deep Dive §5 calls
        /// for), then converts the gap between that and the current vertical speed
        /// into an acceleration. Pure function, headlessly testable.
        /// </summary>
        public static float ComputeVerticalAccel(float currentWorldY, float currentVerticalSpeed, float targetWorldY,
            float maxClimbRateMetersPerSecond, float verticalAccelGain)
        {
            float heightError = targetWorldY - currentWorldY;
            float desiredVerticalSpeed = Mathf.Clamp(heightError, -maxClimbRateMetersPerSecond, maxClimbRateMetersPerSecond);
            return (desiredVerticalSpeed - currentVerticalSpeed) * verticalAccelGain;
        }

        private void FixedUpdate()
        {
            if (_flightBody == null || _rigidbody == null)
                return;

            float groundHeight = GroundSampler.SampleGroundHeight(transform.position);
            float targetWorldY = ComputeTargetWorldAltitude(mode, desiredAltitudeMeters, groundHeight);
            float verticalAccel = ComputeVerticalAccel(transform.position.y, _rigidbody.linearVelocity.y, targetWorldY,
                maxClimbRateMetersPerSecond, verticalAccelGain);

            _flightBody.ApplySteering(Vector3.up * verticalAccel);
        }
    }
}
