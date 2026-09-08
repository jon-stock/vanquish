using UnityEngine;

namespace Vanquish.Theatre.Play
{
    /// <summary>
    /// Top-down strategy-map camera, orbiting a focus point: WASD *or* left-click-
    /// drag pans the focus point (relative to the camera's current facing, so
    /// "forward" always means "into the screen" regardless of rotation),
    /// right-click-drag orbits (yaw/pitch) around the focus point, and the scroll
    /// wheel zooms — smoothly: scrolling sets a target distance, and the actual
    /// distance eases toward it each frame rather than jumping.
    /// </summary>
    public class TheatreMapCameraController : MonoBehaviour
    {
        public Vector3 focusPoint;

        /// <summary>
        /// Set by whoever builds this camera to a check like harness.IsPointerOverUI
        /// — clicking/dragging/scrolling while the cursor is over an OnGUI panel
        /// (e.g. clicking a "Build" button) must NOT also pan/orbit/zoom the map or
        /// reach through to world-space hex selection underneath it. Optional (null
        /// means "never over UI").
        /// </summary>
        public System.Func<bool> isPointerOverUI;

        public float panSpeed = 14f;

        [Tooltip("Left-click-drag pan speed, scaled by current zoom distance so a drag covers the same apparent screen distance at any zoom level.")]
        public float dragPanSensitivity = 0.03f;

        public float orbitSensitivity = 3f;
        public float minPitch = 15f;
        public float maxPitch = 85f;

        public float zoomSensitivity = 2f;
        public float zoomSmoothing = 8f;
        public float minDistance = 6f;
        public float maxDistance = 60f;

        private float _yaw;
        private float _pitch;
        private float _distance;
        private float _targetDistance;

        private void Start()
        {
            // Derive the initial yaw/pitch/distance from wherever the harness placed
            // this camera, so callers can just set position+LookAt once at spawn and
            // this component takes over smoothly from there.
            Vector3 offset = transform.position - focusPoint;
            _distance = _targetDistance = Mathf.Clamp(offset.magnitude, minDistance, maxDistance);

            Quaternion lookRotation = Quaternion.LookRotation(-offset.normalized, Vector3.up);
            Vector3 euler = lookRotation.eulerAngles;
            _pitch = NormalizePitch(euler.x);
            _yaw = euler.y;
        }

        private void LateUpdate()
        {
            HandlePan();
            HandleLeftDragPan();
            HandleOrbit();
            HandleZoom();
            Apply();
        }

        private void HandlePan()
        {
            float x = 0f;
            float z = 0f;
            if (Input.GetKey(KeyCode.W)) z += 1f;
            if (Input.GetKey(KeyCode.S)) z -= 1f;
            if (Input.GetKey(KeyCode.A)) x -= 1f;
            if (Input.GetKey(KeyCode.D)) x += 1f;

            if (x == 0f && z == 0f)
                return;

            // Pan relative to the camera's current horizontal facing (yaw only) so
            // "forward" always reads as "into the screen," even after rotating.
            Quaternion yawRotation = Quaternion.Euler(0f, _yaw, 0f);
            Vector3 panDirection = yawRotation * new Vector3(x, 0f, z);
            focusPoint += panDirection.normalized * panSpeed * Time.deltaTime;
        }

        /// <summary>
        /// Left-click-drag "grab and drag the map" panning — same relative-to-facing
        /// direction as WASD, scaled by current zoom distance so a given screen-space
        /// drag covers the same apparent distance whether zoomed in or out.
        /// </summary>
        private bool IsPointerOverUI() => isPointerOverUI != null && isPointerOverUI();

        private void HandleLeftDragPan()
        {
            if (!Input.GetMouseButton(0) || IsPointerOverUI())
                return;

            float deltaX = Input.GetAxis("Mouse X");
            float deltaY = Input.GetAxis("Mouse Y");
            if (Mathf.Approximately(deltaX, 0f) && Mathf.Approximately(deltaY, 0f))
                return;

            Quaternion yawRotation = Quaternion.Euler(0f, _yaw, 0f);
            Vector3 right = yawRotation * Vector3.right;
            Vector3 forward = yawRotation * Vector3.forward;

            float scale = dragPanSensitivity * _distance;
            // Dragging right/up should move the "grabbed" ground with the mouse —
            // i.e. the camera's focus moves the OPPOSITE way the mouse moved.
            focusPoint -= (right * deltaX + forward * deltaY) * scale;
        }

        private void HandleOrbit()
        {
            if (!Input.GetMouseButton(1) || IsPointerOverUI())
                return;

            float deltaX = Input.GetAxis("Mouse X");
            float deltaY = Input.GetAxis("Mouse Y");
            _yaw += deltaX * orbitSensitivity;
            _pitch = Mathf.Clamp(_pitch - deltaY * orbitSensitivity, minPitch, maxPitch);
        }

        private void HandleZoom()
        {
            float scroll = IsPointerOverUI() ? 0f : Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
                _targetDistance = Mathf.Clamp(_targetDistance - scroll * zoomSensitivity * 10f, minDistance, maxDistance);

            // Ease toward the target distance rather than jumping straight to it —
            // this is the actual "smoother zoom" fix.
            _distance = Mathf.Lerp(_distance, _targetDistance, Time.deltaTime * zoomSmoothing);
        }

        private void Apply()
        {
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            transform.position = focusPoint + rotation * Vector3.back * _distance;
            transform.LookAt(focusPoint);
        }

        private static float NormalizePitch(float angle)
        {
            if (angle > 180f)
                angle -= 360f;
            return angle;
        }
    }
}
