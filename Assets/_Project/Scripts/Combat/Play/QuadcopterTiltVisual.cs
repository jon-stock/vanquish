using UnityEngine;

namespace Vanquish.Combat.Play
{
    /// <summary>
    /// Purely visual: banks a quadcopter drone's visual mesh in the direction it's
    /// currently moving (pitch forward/back, roll left/right), the way a real
    /// multirotor leans into its direction of travel. Lives entirely on the visual
    /// child, never touching the physics root's actual rotation — this is what makes
    /// movement direction readable at a glance even though FlightBody disables
    /// orientToVelocity for quadcopter-style drones. (Ported near-verbatim from the
    /// pre-pivot project.)
    /// </summary>
    public class QuadcopterTiltVisual : MonoBehaviour
    {
        public Rigidbody body;
        public Transform visualRoot;

        public float maxTiltDegrees = 25f;
        public float tiltResponsiveness = 6f;

        private Quaternion _baseLocalRotation;

        private void Awake()
        {
            if (visualRoot != null)
                _baseLocalRotation = visualRoot.localRotation;
        }

        private void LateUpdate()
        {
            if (body == null || visualRoot == null)
                return;

            Vector3 localVelocity = body.transform.InverseTransformDirection(body.linearVelocity);

            float pitch = Mathf.Clamp(localVelocity.z, -maxTiltDegrees, maxTiltDegrees);
            float roll = Mathf.Clamp(-localVelocity.x, -maxTiltDegrees, maxTiltDegrees);

            Quaternion targetTilt = _baseLocalRotation * Quaternion.Euler(pitch, 0f, roll);
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, targetTilt, Time.deltaTime * tiltResponsiveness);
        }
    }
}
