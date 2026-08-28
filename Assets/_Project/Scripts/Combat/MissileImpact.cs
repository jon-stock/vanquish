using UnityEngine;
using Vanquish.Core;
using Vanquish.Simulation.Flight;
using Vanquish.Simulation.Guidance;
using Vanquish.Simulation.Sensors;

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

        [Header("Warhead (populated from MissilePayloadDefinition at spawn time)")]
        public float directDamage = 25f;
        public float splashDamage = 0f;
        public float blastRadiusMeters = 0f;

        [Tooltip("Layer mask used when finding nearby Health components for splash damage.")]
        public LayerMask splashDamageMask = ~0;

        private void OnCollisionEnter(Collision collision)
        {
            ReportHit(collision.gameObject, isProximityDetonation: false);
        }

        /// <summary>
        /// Records a hit, whether from a direct hull collision or a proximity fuse
        /// detonation (see ProximityFuseRelay), applies warhead damage, and halts the missile.
        /// </summary>
        public void ReportHit(GameObject hitObject, bool isProximityDetonation)
        {
            if (hasImpacted)
                return;

            // No friendly fire: ignore same-team hits, most importantly the missile's
            // own launching drone, which the proximity fuse would otherwise detonate
            // on instantly since missiles spawn overlapping/very close to it.
            var ownSignature = GetComponent<DetectableSignature>();
            var hitSignature = hitObject.GetComponentInParent<DetectableSignature>();
            if (ownSignature != null && hitSignature != null && hitSignature.team == ownSignature.team)
                return;

            hasImpacted = true;
            string kind = isProximityDetonation ? "PROXIMITY DETONATION" : "DIRECT HIT";
            Debug.Log($"[Combat] {kind}: missile impacted '{hitObject.name}' at t={Time.time:F2}s, " +
                      $"position={transform.position}");

            ApplyDamage(hitObject);

            var flightBody = GetComponent<FlightBody>();
            if (flightBody != null)
                flightBody.isThrusting = false;

            var guidance = GetComponent<GuidanceController>();
            if (guidance != null)
                guidance.enabled = false;

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = Vector3.zero;

            Destroy(gameObject, 0.1f);
        }

        private void ApplyDamage(GameObject hitObject)
        {
            var directHealth = hitObject.GetComponentInParent<Health>();
            if (directHealth != null)
                directHealth.TakeDamage(directDamage);

            if (blastRadiusMeters <= 0f || splashDamage <= 0f)
                return;

            var colliders = Physics.OverlapSphere(transform.position, blastRadiusMeters, splashDamageMask);
            var alreadyDamaged = new System.Collections.Generic.HashSet<Health>();
            if (directHealth != null)
                alreadyDamaged.Add(directHealth);

            foreach (var col in colliders)
            {
                var health = col.GetComponentInParent<Health>();
                if (health == null || !alreadyDamaged.Add(health))
                    continue;

                float distance = Vector3.Distance(transform.position, health.transform.position);
                float falloff = Mathf.Clamp01(1f - (distance / blastRadiusMeters));
                health.TakeDamage(splashDamage * falloff);
            }
        }
    }
}
