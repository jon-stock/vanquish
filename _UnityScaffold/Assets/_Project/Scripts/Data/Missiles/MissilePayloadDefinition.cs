using UnityEngine;

namespace Vanquish.Data.Missiles
{
    [CreateAssetMenu(menuName = "Vanquish/Missile/Payload", fileName = "NewMissilePayload")]
    public class MissilePayloadDefinition : PartDefinition
    {
        [Header("Payload")]
        public PayloadType payloadType;

        [Tooltip("Warhead size class — larger sizes add mass/drag but increase blast radius and damage.")]
        public float warheadMassKg;

        public float blastRadiusMeters;
        public float directDamage;
        public float splashDamage;

        [Tooltip("If true, requires a proximity or impact fuse to be effective (affects vs. fast/maneuvering targets).")]
        public bool requiresProximityFuse;
    }
}
