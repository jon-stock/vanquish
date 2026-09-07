using UnityEngine;

namespace Vanquish.Data.Drones
{
    [CreateAssetMenu(menuName = "Vanquish/Drone/Propulsion", fileName = "NewPropulsion")]
    public class PropulsionDefinition : PartDefinition
    {
        [Header("Propulsion")]
        public PropulsionType propulsionType;

        public float maxSpeedMetersPerSecond;
        public float accelerationMetersPerSecondSquared;

        [Tooltip("Noise/acoustic signature — affects detection by audio-based sensors, if modeled.")]
        public float acousticSignature;

        [Tooltip("Heat signature contribution to IR detectability.")]
        public float infraredSignature;
    }
}
