using UnityEngine;
using Vanquish.Simulation.Damage;
using Vanquish.Simulation.Flight;
using Vanquish.Simulation.Guidance;

namespace Vanquish.Combat.Play
{
    /// <summary>
    /// Detects a missile's collision (hit) and applies its warhead's damage to
    /// whatever it hit via <see cref="IDamageable"/> — the same payload/hardness
    /// soft-cap model the rest of the combat-instance logic uses (PLAN.md Damage &amp;
    /// Payload Model), so a real 3D hit goes through identical math to the
    /// abstract/headless engagement logic. Adapted from the pre-pivot project's
    /// MissileImpact, which used a flat Health.TakeDamage(amount) — this version
    /// routes through IDamageable.TakeDamage(rawDamage, payloadSize) instead so
    /// hardness/soft-cap actually applies.
    /// </summary>
    public class MissileImpact : MonoBehaviour
    {
        public bool HasImpacted { get; private set; }

        [Tooltip("Populated from the firing StockpileEntry's rawDamage at spawn time.")]
        public float rawDamage = 25f;

        [Tooltip("Populated from the firing StockpileEntry's payloadSize at spawn time — fed into the target's DamageResolver hardness soft-cap.")]
        public float payloadSize = 1f;

        private void OnCollisionEnter(Collision collision)
        {
            ReportHit(collision.gameObject);
        }

        private void ReportHit(GameObject hitObject)
        {
            if (HasImpacted)
                return;

            HasImpacted = true;

            IDamageable damageable = hitObject.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(rawDamage, payloadSize);
                Debug.Log($"[Combat] Missile hit '{hitObject.name}' for {rawDamage} raw damage (payload {payloadSize}).");
            }
            else
            {
                Debug.Log($"[Combat] Missile hit '{hitObject.name}' (no IDamageable — no damage applied).");
            }

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
    }
}
