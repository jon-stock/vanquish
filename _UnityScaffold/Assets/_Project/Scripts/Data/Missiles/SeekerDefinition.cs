using UnityEngine;

namespace Vanquish.Data.Missiles
{
    [CreateAssetMenu(menuName = "Vanquish/Missile/Seeker", fileName = "NewSeeker")]
    public class SeekerDefinition : PartDefinition
    {
        [Header("Seeker")]
        public SeekerType seekerType;

        [Tooltip("Detection range in meters under ideal conditions.")]
        public float detectionRangeMeters;

        [Tooltip("Half-angle of the seeker's detection cone, in degrees.")]
        public float fieldOfViewDegrees;

        [Tooltip("How quickly the seeker can re-acquire lock after losing it, in seconds.")]
        public float reacquisitionTimeSeconds;

        [Tooltip("Resistance to enemy jamming, 0-1 (used against ECM effectiveness).")]
        [Range(0f, 1f)]
        public float jamResistance;

        [Tooltip("Susceptibility to countermeasures like flares/chaff, 0-1 (higher = more easily spoofed).")]
        [Range(0f, 1f)]
        public float countermeasureSusceptibility;
    }
}
