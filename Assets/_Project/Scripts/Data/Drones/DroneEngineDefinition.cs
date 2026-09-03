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

        [Header("Flight Model Compatibility")]
        [Tooltip("Must match this engine's paired PropulsionDefinition.requiresForwardFlight — true for jet " +
            "engines (Subsonic/Supersonic Turbofan etc.), false for electric/ICE motors. Added specifically " +
            "because, before this field existed, nothing tied an engine to a flight model at all: a jet engine " +
            "and an electric motor were interchangeable in the Workshop's Engine slot with zero validation, " +
            "so a design could freely mismatch a jet engine onto multirotor propulsion or vice versa. See " +
            "DroneCompatibility, which reads this alongside the airframe/wing/propulsion choices to flag a " +
            "design whose parts disagree on flight model.")]
        public bool requiresForwardFlight;
    }
}
