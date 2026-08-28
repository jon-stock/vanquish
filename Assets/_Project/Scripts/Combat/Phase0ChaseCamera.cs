using UnityEngine;
using UnityEngine.InputSystem;

namespace Vanquish.Combat
{
    /// <summary>
    /// Test-harness/MVP camera: keeps both a primary followed transform and a
    /// secondary target framed by following their midpoint and backing off distance
    /// based on how far apart they are (for Phase 0's missile-vs-target test, or
    /// Phase 1 combat where both are set to the same transform — see
    /// Phase1CombatSceneBuilder). Supports right-click-drag to orbit around the
    /// followed point and scroll wheel to zoom, since a fixed unmovable camera angle
    /// made it hard to actually see what was happening during combat.
    /// </summary>
    public class Phase0ChaseCamera : MonoBehaviour
    {
        public Transform missile;
        public Transform target;

        public Vector3 baseOffsetDirection = new Vector3(-1f, 0.6f, -0.6f);
        public float minDistance = 40f;
        public float distancePadding = 30f;
        public float followSmoothing = 3f;

        [Header("Orbit / Zoom Control")]
        public float orbitSensitivity = 0.3f;
        public float zoomSensitivity = 0.1f;
        public float minZoom = 0.4f;
        public float maxZoom = 3f;
        public float minPitch = -80f;
        public float maxPitch = 80f;

        private float _orbitYaw;
        private float _orbitPitch;
        private float _zoom = 1f;

        private void Start()
        {
            // Initialize orbit angles from the configured base offset so the camera
            // starts exactly where it used to before any manual orbiting occurs.
            Quaternion baseRotation = Quaternion.LookRotation(-baseOffsetDirection.normalized, Vector3.up);
            Vector3 euler = baseRotation.eulerAngles;
            _orbitPitch = NormalizePitch(euler.x);
            _orbitYaw = euler.y;
        }

        private void LateUpdate()
        {
            if (missile == null || target == null)
                return;

            HandleOrbitInput();

            Vector3 midpoint = (missile.position + target.position) * 0.5f;
            float separation = Vector3.Distance(missile.position, target.position);
            float distance = Mathf.Max(minDistance, separation + distancePadding) * _zoom;

            Quaternion orbitRotation = Quaternion.Euler(_orbitPitch, _orbitYaw, 0f);
            Vector3 offsetDirection = orbitRotation * Vector3.back;

            Vector3 desiredPosition = midpoint + offsetDirection * distance;

            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSmoothing);
            transform.LookAt(midpoint);
        }

        private void HandleOrbitInput()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                _orbitYaw += delta.x * orbitSensitivity;
                _orbitPitch = Mathf.Clamp(_orbitPitch - delta.y * orbitSensitivity, minPitch, maxPitch);
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                _zoom = Mathf.Clamp(_zoom - scroll * zoomSensitivity * 0.01f, minZoom, maxZoom);
        }

        private static float NormalizePitch(float angle)
        {
            if (angle > 180f)
                angle -= 360f;
            return angle;
        }
    }
}
