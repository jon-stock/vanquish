using UnityEngine;

namespace Vanquish.Combat.Play
{
    /// <summary>
    /// Keeps both a primary followed transform (the player's drone) and a secondary
    /// target framed by following their midpoint and backing off distance based on
    /// how far apart they are. Right-click-drag orbits around the followed point;
    /// scroll wheel zooms. Adapted from the pre-pivot project's Phase0ChaseCamera,
    /// ported from the new Input System to legacy UnityEngine.Input.
    /// </summary>
    public class ChaseCamera : MonoBehaviour
    {
        public Transform primary;
        public Transform secondary;

        public Vector3 baseOffsetDirection = new Vector3(-1f, 0.6f, -0.6f);

        [Tooltip("Tuned for ~1-2m-scale drones/objectives — close enough that the unit actually fills a meaningful part of the screen instead of reading as a tiny distant blob.")]
        public float minDistance = 6f;
        public float distancePadding = 5f;
        public float followSmoothing = 3f;

        [Header("Orbit / Zoom Control")]
        public float orbitSensitivity = 3f;
        public float zoomSensitivity = 2f;
        public float minZoom = 0.4f;
        public float maxZoom = 3f;
        public float minPitch = -80f;
        public float maxPitch = 80f;

        private float _orbitYaw;
        private float _orbitPitch;
        private float _zoom = 1f;

        private void Start()
        {
            Quaternion baseRotation = Quaternion.LookRotation(-baseOffsetDirection.normalized, Vector3.up);
            Vector3 euler = baseRotation.eulerAngles;
            _orbitPitch = NormalizePitch(euler.x);
            _orbitYaw = euler.y;
        }

        private void LateUpdate()
        {
            if (primary == null)
                return;

            HandleOrbitInput();

            Transform focus = secondary != null ? secondary : primary;
            Vector3 midpoint = secondary != null ? (primary.position + secondary.position) * 0.5f : primary.position;
            float separation = secondary != null ? Vector3.Distance(primary.position, secondary.position) : 0f;
            float distance = Mathf.Max(minDistance, separation + distancePadding) * _zoom;

            Quaternion orbitRotation = Quaternion.Euler(_orbitPitch, _orbitYaw, 0f);
            Vector3 offsetDirection = orbitRotation * Vector3.back;

            Vector3 desiredPosition = midpoint + offsetDirection * distance;

            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSmoothing);
            transform.LookAt(midpoint);
        }

        private void HandleOrbitInput()
        {
            if (Input.GetMouseButton(1))
            {
                float deltaX = Input.GetAxis("Mouse X");
                float deltaY = Input.GetAxis("Mouse Y");
                _orbitYaw += deltaX * orbitSensitivity;
                _orbitPitch = Mathf.Clamp(_orbitPitch - deltaY * orbitSensitivity, minPitch, maxPitch);
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
                _zoom = Mathf.Clamp(_zoom - scroll * zoomSensitivity, minZoom, maxZoom);
        }

        private static float NormalizePitch(float angle)
        {
            if (angle > 180f)
                angle -= 360f;
            return angle;
        }
    }
}
