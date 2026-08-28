using UnityEngine;

namespace Vanquish.Combat
{
    /// <summary>
    /// Purely visual: banks a quadcopter drone's visual mesh in the direction it's
    /// currently moving/accelerating (pitch forward/back, roll left/right), the way a
    /// real multirotor leans into its direction of travel. Lives entirely on the
    /// visual child, never touching the physics root's actual rotation — this is what
    /// makes movement direction readable at a glance even though FlightBody disables
    /// orientToVelocity for quadcopter-style drones (see VehicleFactory.SpawnDrone).
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

            // Velocity expressed in the physics root's own local space, since the root
            // doesn't rotate to face travel direction — this gives a stable "which way
            // relative to my fixed orientation am I sliding" reading to bank against.
            Vector3 localVelocity = body.transform.InverseTransformDirection(body.linearVelocity);

            float pitch = Mathf.Clamp(localVelocity.z, -maxTiltDegrees, maxTiltDegrees);
            float roll = Mathf.Clamp(-localVelocity.x, -maxTiltDegrees, maxTiltDegrees);

            Quaternion targetTilt = _baseLocalRotation * Quaternion.Euler(pitch, 0f, roll);
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, targetTilt, Time.deltaTime * tiltResponsiveness);
        }
    }
}
