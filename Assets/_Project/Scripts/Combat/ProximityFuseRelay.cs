using UnityEngine;

namespace Vanquish.Combat
{
    /// <summary>
    /// Lives on a child trigger-collider GameObject representing a missile's proximity
    /// fuse detection radius (see MissilePayloadDefinition.requiresProximityFuse).
    /// Forwards trigger entry back to the parent's MissileImpact, since Unity delivers
    /// trigger callbacks to the GameObject that owns the Collider, not the Rigidbody
    /// root. Modeling this separately from a direct hull impact reflects how real
    /// proximity-fused warheads detonate near a target rather than requiring contact.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ProximityFuseRelay : MonoBehaviour
    {
        public MissileImpact owner;

        private void OnTriggerEnter(Collider other)
        {
            if (owner == null)
                return;

            // Only actual units (anything with Health) arm the proximity fuse — this
            // deliberately excludes terrain/scenery, which the missile should only
            // ever destroy itself against via a direct hull collision, not detonate
            // near just because it's flying close to the ground.
            if (other.GetComponentInParent<Health>() == null)
                return;

            owner.ReportHit(other.gameObject, isProximityDetonation: true);
        }
    }
}
