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
            if (owner != null)
                owner.ReportHit(other.gameObject, isProximityDetonation: true);
        }
    }
}
