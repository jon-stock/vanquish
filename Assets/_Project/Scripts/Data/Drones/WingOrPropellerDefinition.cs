using UnityEngine;

namespace Vanquish.Data.Drones
{
    public enum LiftSurfaceType
    {
        Propeller,
        FixedWing,
        DeltaWing,
        VariableSweepWing,
    }

    /// <summary>Phase 2B rotor breadth — only meaningful when liftSurfaceType == Propeller.</summary>
    public enum RotorMaterial
    {
        Plastic,
        CarbonFiber,
        Metal,
    }

    /// <summary>Phase 2B rotor breadth — only meaningful when liftSurfaceType == Propeller.</summary>
    public enum RotorSize
    {
        Small,
        Medium,
        Large,
    }

    [CreateAssetMenu(menuName = "Vanquish/Drone/Wing Or Propeller", fileName = "NewWingOrPropeller")]
    public class WingOrPropellerDefinition : PartDefinition
    {
        [Header("Lift Surface")]
        public LiftSurfaceType liftSurfaceType;

        [Tooltip("Lift coefficient — higher improves low-speed handling/payload capacity.")]
        public float liftCoefficient;

        [Tooltip("Additional drag contributed by this surface.")]
        public float dragCoefficient;

        [Tooltip("Maneuverability bonus (turn rate), degrees/second.")]
        public float turnRateDegreesPerSecond;

        [Tooltip("Efficiency multiplier applied to fuel/battery consumption at cruise speed.")]
        public float cruiseEfficiencyMultiplier = 1f;

        [Header("Phase 2B: Rotor Breadth (Propeller-type only)")]
        [Tooltip("Rotor material — Plastic (cheap/low mass/low durability), CarbonFiber (lightest but weaker " +
            "than Plastic/Metal), Metal (heaviest, strongest/most durable). Meaningless for wing types.")]
        public RotorMaterial rotorMaterial;

        [Tooltip("Rotor size — scales liftCoefficient/lift capacity up at a mass and drag cost, independent " +
            "of material. Meaningless for wing types.")]
        public RotorSize rotorSize;

        [Tooltip("Structural integrity 0-1 (durability). Informational for now — a hook for a future rotor " +
            "damage mechanic (see PLAN.md Phase 3 stretch goals), not yet consumed by any runtime system.")]
        [Range(0f, 1f)]
        public float structuralIntegrity = 1f;
    }
}
