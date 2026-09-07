using UnityEngine;
using Vanquish.Simulation.Flight;

namespace Vanquish.Combat.Play
{
    /// <summary>
    /// Player control for a multirotor (quadcopter/hexacopter) drone — hovers and
    /// strafes omnidirectionally. WASD/Space/Shift map to camera-relative
    /// acceleration via FlightBody.ApplySteering (not fixed world axes, since the
    /// camera can be freely orbited — see ChaseCamera). Releasing all keys actively
    /// brakes to a hover rather than coasting. Left mouse fires the equipped weapon.
    ///
    /// Adapted from the pre-pivot project's PlayerDroneController — trimmed to just
    /// the multirotor control scheme (this pivot's Phase 1 focus is tier 0/1
    /// quadcopters/hexacopters specifically, not fixed-wing/jet drones) and ported
    /// from the new Input System to legacy UnityEngine.Input so it works with this
    /// project's default input configuration without adding a package dependency.
    /// </summary>
    [RequireComponent(typeof(FlightBody))]
    public class PlayerDroneController : MonoBehaviour
    {
        public float steeringForce = 60f;

        [Tooltip("Braking acceleration applied to actively cancel velocity when no movement key is held, " +
                 "simulating a quadcopter's auto-hover rather than coasting/drifting on weak passive drag alone.")]
        public float brakingForce = 80f;

        private FlightBody _flightBody;
        private WeaponController _weapon;
        private Rigidbody _rigidbody;

        private void Awake()
        {
            _flightBody = GetComponent<FlightBody>();
            // Player input is the only propulsion source for a multirotor — no
            // constant forward thrust, matching a real quadcopter's vectored-thrust hover.
            _flightBody.isThrusting = false;
            _weapon = GetComponent<WeaponController>();
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            Vector2 rawInput = Vector2.zero;
            float verticalInput = 0f;

            if (Input.GetKey(KeyCode.W)) rawInput += Vector2.up;
            if (Input.GetKey(KeyCode.S)) rawInput += Vector2.down;
            if (Input.GetKey(KeyCode.A)) rawInput += Vector2.left;
            if (Input.GetKey(KeyCode.D)) rawInput += Vector2.right;
            if (Input.GetKey(KeyCode.Space)) verticalInput += 1f;
            if (Input.GetKey(KeyCode.LeftShift)) verticalInput -= 1f;

            Vector3 input = CameraRelativeDirection(rawInput) + Vector3.up * verticalInput;

            if (input.sqrMagnitude > 0.001f)
            {
                _flightBody.ApplySteering(input.normalized * steeringForce);
            }
            else if (_rigidbody.linearVelocity.sqrMagnitude > 0.01f)
            {
                // Active hover-hold: cancel current velocity rather than coasting on
                // weak passive drag alone, which otherwise makes the drone feel
                // sluggish/wallowy, drifting for a long time after releasing keys.
                Vector3 brakeDirection = -_rigidbody.linearVelocity.normalized;
                float brakeMagnitude = Mathf.Min(brakingForce, _rigidbody.linearVelocity.magnitude / Time.fixedDeltaTime);
                _flightBody.ApplySteering(brakeDirection * brakeMagnitude);
            }
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
            if (_weapon == null || !Input.GetMouseButtonDown(0))
                return;

            _weapon.Fire();
        }
    }
}
