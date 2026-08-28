using UnityEngine;
using Vanquish.Simulation.Flight;
using Vanquish.Simulation.Guidance;

namespace Vanquish.Combat
{
    /// <summary>
    /// Phase 0 test-harness script: detects a collision (hit) and logs/reports it,
    /// then stops the missile. Real damage/warhead application arrives with the
    /// MissilePayloadDefinition integration in Phase 1/2.
    /// </summary>
    public class MissileImpact : MonoBehaviour
    {
        public bool hasImpacted { get; private set; }

        private void OnCollisionEnter(Collision collision)
        {
            ReportHit(collision.gameObject, isProximityDetonation: false);
        }

        /// <summary>
        /// Records a hit, whether from a direct hull collision or a proximity fuse
        /// detonation (see ProximityFuseRelay), and halts the missile.
        /// </summary>
        public void ReportHit(GameObject hitObject, bool isProximityDetonation)
        {
            if (hasImpacted)
                return;

            hasImpacted = true;
            string kind = isProximityDetonation ? "PROXIMITY DETONATION" : "DIRECT HIT";
            Debug.Log($"[Phase0Test] {kind}: missile impacted '{hitObject.name}' at t={Time.time:F2}s, " +
                      $"position={transform.position}");

            var flightBody = GetComponent<FlightBody>();
            if (flightBody != null)
                flightBody.isThrusting = false;

            var guidance = GetComponent<GuidanceController>();
            if (guidance != null)
                guidance.enabled = false;

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = Vector3.zero;
        }
    }
}
