using UnityEngine;

namespace Vanquish.Data.Shared
{
    /// <summary>
    /// Shared fuel definition used by both missiles and drones. Set `category` to
    /// MissileFuel or DroneFuel depending on which slot this asset is intended for.
    /// </summary>
    [CreateAssetMenu(menuName = "Vanquish/Shared/Fuel", fileName = "NewFuel")]
    public class FuelDefinition : PartDefinition
    {
        [Header("Fuel")]
        public FuelType fuelType;

        [Tooltip("Energy density (affects range/burn-time per kg of fuel carried).")]
        public float energyDensityMjPerKg;

        [Tooltip("Mass of fuel carried at full capacity, in kg.")]
        public float capacityKg;

        [Tooltip("Flammability/volatility — higher increases damage if the fuel tank is hit.")]
        [Range(0f, 1f)]
        public float volatility;
    }
}
