using UnityEngine;

namespace Vanquish.Data.Support
{
    [CreateAssetMenu(menuName = "Vanquish/Support/Radar Installation", fileName = "NewRadarInstallation")]
    public class RadarInstallationDefinition : PartDefinition
    {
        [Header("Radar Installation")]
        public float detectionRangeMeters;
        public float fieldOfViewDegrees;

        [Tooltip("If true, provides 360-degree coverage regardless of fieldOfViewDegrees.")]
        public bool omnidirectional;

        [Tooltip("Resistance to enemy jamming, 0-1.")]
        [Range(0f, 1f)]
        public float jamResistance;

        public float health;
    }
}
