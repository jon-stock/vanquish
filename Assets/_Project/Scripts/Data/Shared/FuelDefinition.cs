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

        [Header("Range (Phase 0 simplification)")]
        [Tooltip(
            "Authored max operational radius in meters at full fuel capacity, for a " +
            "'reference' airframe/engine combo. PLAN.md's movement/reach rule needs a " +
            "range/endurance stat per unit; deriving this properly from thrust + drag + " +
            "fuel burn belongs to the full flight model (Phase 2+ aerodynamics work), so " +
            "Phase 0 authors it directly here rather than computing it. A committed " +
            "strike/defense package's usable engagement radius is the MINIMUM of this " +
            "value across all committed units — see Combat.StrikePackageRange.")]
        public float operationalRangeMeters;

        [Tooltip("Flammability/volatility — higher increases damage if the fuel tank is hit.")]
        [Range(0f, 1f)]
        public float volatility;
    }
}
