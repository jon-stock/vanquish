using UnityEngine;
using UnityEngine.InputSystem;
using Vanquish.Core;
using Vanquish.Simulation.Flight;

namespace Vanquish.Combat
{
    /// <summary>
    /// Phase 1 minimal player control: WASD steering mapped to camera-relative
    /// horizontal acceleration (via FlightBody.ApplySteering, reusing the exact same
    /// steering path missiles and AI use) — not fixed world axes, since the camera
    /// can now be freely orbited (see Phase0ChaseCamera) and fixed-axis "forward"
    /// became disorientating the moment the camera faced a different direction than
    /// the world's +Z. Space fires at the nearest enemy contact known to the
    /// player's team (i.e. detected either by this drone or by a scout). Disables
    /// FlightBody's constant thrust since player input is the only propulsion source
    /// here, unlike missiles/AI which always thrust forward toward a target.
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
            _flightBody.isThrusting = false;
            _weapon = GetComponent<WeaponController>();
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
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
            // Fire on left mouse click rather than Space, since Space is now used for
            // altitude (Space = up, Left Shift = down).
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
