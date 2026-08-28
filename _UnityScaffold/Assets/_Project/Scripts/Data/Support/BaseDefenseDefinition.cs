using UnityEngine;

namespace Vanquish.Data.Support
{
    public enum BaseDefenseType
    {
        PointDefenseInterceptor,
        SamSite,
        Cram, // close-in weapon system style rapid-fire defense
    }

    [CreateAssetMenu(menuName = "Vanquish/Support/Base Defense", fileName = "NewBaseDefense")]
    public class BaseDefenseDefinition : PartDefinition
    {
        [Header("Base Defense")]
        public BaseDefenseType defenseType;

        public float engagementRangeMeters;
        public float rateOfFirePerSecond;
        public float interceptProbability;
        public float health;
    }
}
