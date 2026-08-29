using UnityEngine;

namespace Vanquish.Combat
{
    /// <summary>
    /// Continuously spins its transform around the local Y axis. Purely cosmetic —
    /// used for the procedural multirotor drone visual (see DroneVisualBuilder) to
    /// make a thin flat box read as a spinning propeller blur without needing an
    /// actual modeled propeller mesh.
    /// </summary>
    public class RotorSpinner : MonoBehaviour
    {
        public float degreesPerSecond = 1600f;

        private void Update()
        {
            transform.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
