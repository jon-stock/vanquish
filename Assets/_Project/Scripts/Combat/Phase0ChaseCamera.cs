using UnityEngine;

namespace Vanquish.Combat
{
    /// <summary>
    /// Phase 0 test-harness camera: keeps both the missile and its target framed by
    /// following their midpoint and backing off distance based on how far apart they
    /// are. Not part of the final game's camera system — purely for observing the
    /// flight/guidance prototype during manual testing.
    /// </summary>
    public class Phase0ChaseCamera : MonoBehaviour
    {
        public Transform missile;
        public Transform target;

        public Vector3 offsetDirection = new Vector3(-1f, 0.6f, -0.6f);
        public float minDistance = 40f;
        public float distancePadding = 30f;
        public float followSmoothing = 3f;

        private void LateUpdate()
        {
            if (missile == null || target == null)
                return;

            Vector3 midpoint = (missile.position + target.position) * 0.5f;
            float separation = Vector3.Distance(missile.position, target.position);
            float distance = Mathf.Max(minDistance, separation + distancePadding);

            Vector3 desiredPosition = midpoint + offsetDirection.normalized * distance;

            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSmoothing);
            transform.LookAt(midpoint);
        }
    }
}
