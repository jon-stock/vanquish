using UnityEngine;

namespace Vanquish.Data.Drones
{
    public enum DroneAirframeClass
    {
        SmallQuad,
        FixedWing,
        FlyingWingStealth,
        CcaScale,
    }

    [CreateAssetMenu(menuName = "Vanquish/Drone/Airframe", fileName = "NewDroneAirframe")]
    public class DroneAirframeDefinition : PartDefinition
    {
        [Header("Airframe")]
        public DroneAirframeClass airframeClass;

        public float dragCoefficient;
        public float structuralMassKg;

        [Tooltip("Number of weapon bay/hardpoint slots this airframe provides.")]
        public int hardpointCount;

        [Tooltip("Number of internal (low-signature) weapon bay slots, subset of hardpointCount.")]
        public int internalBayCount;

        [Tooltip("Base radar cross-section before hull material / countermeasure modifiers.")]
        public float baseRadarCrossSection;
    }
}
