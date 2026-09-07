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
    }
}
