using UnityEngine;

namespace Vanquish.Data.Drones
{
    [CreateAssetMenu(menuName = "Vanquish/Drone/Engine", fileName = "NewDroneEngine")]
    public class DroneEngineDefinition : PartDefinition
    {
        [Header("Engine")]
        [Tooltip("Power output in kW (electric) or thrust in Newtons (jet) — interpreted based on matched propulsion type.")]
        public float powerOutput;

        [Tooltip("Fuel/energy consumption rate at full throttle.")]
        public float consumptionRatePerSecond;

        [Tooltip("Heat signature contribution to IR detectability.")]
        public float infraredSignature;

        [Tooltip("Reliability factor 0-1, used for random failure chance in long missions (future feature hook).")]
        [Range(0f, 1f)]
        public float reliability = 1f;
    }
}
