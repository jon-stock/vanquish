using UnityEngine;

namespace Vanquish.Combat
{
    /// <summary>
    /// Applies damage for direct physical collisions — ramming another unit or flying
    /// into terrain — scaled by impact speed. Separate from MissilePayloadDefinition's
    /// warhead damage (handled by MissileImpact/ProximityFuseRelay), which only
    /// applies to actual missile detonations, not hull-to-hull or hull-to-terrain
    /// crashes. Ignores collisions with missiles entirely, since those already have
    /// their own dedicated damage path.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CrashDamage : MonoBehaviour
    {
        [Tooltip("Damage dealt per m/s of impact speed above minImpactSpeed.")]
        public float damagePerMps = 3f;

        [Tooltip("Impacts below this relative speed are treated as a harmless bump, not a crash.")]
        public float minImpactSpeed = 6f;

        private Health _ownHealth;

        private void Awake()
        {
            _ownHealth = GetComponent<Health>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Missiles have their own dedicated warhead damage path (MissileImpact /
            // ProximityFuseRelay) — don't double up on a plain hull-impact here.
            if (collision.gameObject.GetComponentInParent<MissileImpact>() != null)
                return;

            float impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed <= minImpactSpeed)
                return;

            float damage = (impactSpeed - minImpactSpeed) * damagePerMps;

            _ownHealth?.TakeDamage(damage);

            // Ramming another unit hurts both sides.
            var otherHealth = collision.gameObject.GetComponentInParent<Health>();
            if (otherHealth != null && otherHealth != _ownHealth)
                otherHealth.TakeDamage(damage);
        }
    }
}
