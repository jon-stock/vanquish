using UnityEngine;

namespace Vanquish.Data.Drones
{
    public enum LiftSurfaceType
    {
        Propeller,
        FixedWing,
        DeltaWing,
        VariableSweepWing,

        // Added for the planform-preset pass (merging Airframe+Wing into curated,
        // real-world-referenced "Planform" presets — see DronePlanformDefinition) to
        // give the tailless flying-wing planform (X-47B-inspired) its own genuinely
        // distinct cranked/kite mesh via PrimitiveMeshFactory.CreateKiteWing, instead
        // of reusing DeltaWing's simple tapered-triangle mesh for a shape that's
        // actually a broad low-aspect double-sweep kite in reality. Appended, not
        // inserted, to keep existing values' serialized int ordinals stable.
        FlyingWing,
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

        [Header("Fixed-Wing Lift Curve (wing types only — meaningless for Propeller)")]
        [Tooltip("Angle of attack (degrees) at which this surface generates zero lift. Below this AoA, lift " +
            "reverses sign (pushes the nose further down instead of up) — real airfoils are usually slightly " +
            "cambered, so this is a small negative number rather than exactly 0.")]
        public float zeroLiftAoADegrees = -2f;

        [Tooltip("The angle of attack liftCoefficient was tuned at — i.e. flying at exactly this AoA and " +
            "FlightBody's aerodynamic-lift speed model produces exactly liftCoefficient*speed^2 of lift. AoA " +
            "above this (up to criticalAoADegrees) generates progressively more lift than that; AoA below it " +
            "generates progressively less. Kept as its own tunable (rather than assumed to be a fixed 0 or " +
            "always at criticalAoADegrees) so a design's cruise trim angle is a real, inspectable number.")]
        public float referenceAoADegrees = 5f;

        [Tooltip("Angle of attack (degrees) beyond which this surface stalls — lift collapses sharply instead " +
            "of continuing to rise, matching a real airfoil's stall behavior (FlightBody.ComputeLiftFactor " +
            "implements the actual curve). Deliberately still no separate tunable stall SPEED: stall is purely " +
            "a function of angle of attack here, and low speed only causes a stall indirectly, by forcing a " +
            "higher AoA to generate enough lift to counteract weight — exactly how real aerodynamic stall " +
            "works. Delta/variable-sweep planforms realistically tolerate a higher critical angle than a " +
            "straight wing (vortex lift), which is exactly the low-speed-handling vs. maneuverability trade " +
            "PLAN.md's wing-type breadth already calls for — see Phase2BDroneBreadthSeeder's per-wing tuning.")]
        public float criticalAoADegrees = 16f;

        [Tooltip("Lift-induced drag factor — extra drag proportional to the square of the current lift factor " +
            "(see FlightBody.ComputeLiftFactor), representing the real aerodynamic cost of generating lift " +
            "(worst during hard turns/high-AoA climbs, near-zero at the AoA lift was tuned for). A higher-lift, " +
            "lower-aspect-ratio planform (e.g. a delta wing pulling a hard break-turn) pays more of this than " +
            "a slender straight wing at the same lift factor — tune per wing type, not left at one shared value.")]
        public float inducedDragFactor = 0.02f;
    }
}
