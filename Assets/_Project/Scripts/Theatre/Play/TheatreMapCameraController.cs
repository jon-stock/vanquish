using UnityEngine;

namespace Vanquish.Theatre.Play
{
    /// <summary>
    /// Minimal top-down strategy-map camera: WASD pans across the world X/Z plane
    /// (deliberately world-space, not transform-relative, since this camera stays at
    /// a fixed downward pitch — panning "forward" should move across the map, not
    /// change altitude), scroll wheel zooms by moving the camera's height.
    /// </summary>
    public class TheatreMapCameraController : MonoBehaviour
    {
        public float panSpeed = 14f;
        public float zoomSpeed = 10f;
        public float minHeight = 6f;
        public float maxHeight = 34f;

        private void Update()
        {
            float x = 0f;
            float z = 0f;
            if (Input.GetKey(KeyCode.W)) z += 1f;
            if (Input.GetKey(KeyCode.S)) z -= 1f;
            if (Input.GetKey(KeyCode.A)) x -= 1f;
            if (Input.GetKey(KeyCode.D)) x += 1f;

            if (x != 0f || z != 0f)
                transform.position += new Vector3(x, 0f, z) * panSpeed * Time.deltaTime;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                Vector3 pos = transform.position;
                pos.y = Mathf.Clamp(pos.y - scroll * zoomSpeed * 10f, minHeight, maxHeight);
                transform.position = pos;
            }
        }
    }
}
