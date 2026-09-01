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

        [Header("Phase 2B: Flight Model")]
        [Tooltip("True for fixed-wing/jet propulsion (constant forward thrust, orients nose to face velocity, " +
            "matches FlightBody.orientToVelocity/isThrusting). False for omnidirectional multirotor propulsion " +
            "(Electric) that hovers/strafes via vectored steering instead. VehicleFactory reads this to configure " +
            "FlightBody per-design instead of hardcoding quadcopter behavior for every drone.")]
        public bool requiresForwardFlight;
    }
}
