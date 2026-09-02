using UnityEngine;
using Vanquish.Data.Missiles;

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

        [Header("Armament (Phase 2D)")]
        [Tooltip("Missile design this site fires at anything within engagementRangeMeters. " +
            "Embedded directly on the definition (rather than assembled via a DroneLoadout-style " +
            "picker) since a SAM site has no airframe/propulsion/sensor-suite of its own to build " +
            "around it — InstallationFactory.SpawnBaseDefense is the runtime consumer.")]
        public MissileLoadout missileLoadout;

        [Tooltip("Ammo capacity before the site would need (currently unmodeled) resupply. Deliberately " +
            "high by default — a real SAM battery carries far more rounds than a single strike drone.")]
        public int ammoCount = 20;
    }
}
