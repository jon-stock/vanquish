using UnityEngine;

namespace Vanquish.Data.Drones
{
    public enum DroneAirframeClass
    {
        SmallQuad,
        FixedWing,
        FlyingWingStealth,
        CcaScale,

        // Added in Phase 2B for the quadcopter->hexacopter upgrade path. Appended
        // rather than inserted to keep existing values' serialized int ordinals
        // stable on already-saved assets (same convention as SeekerType in 2A).
        Hexacopter,
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

        [Header("Phase 2B: Multirotor Geometry")]
        [Tooltip("Number of rotor arms for multirotor airframes (SmallQuad/Hexacopter) — drives both " +
            "VehicleFactory/DroneVisualBuilder's procedural mesh and (once carry-capacity balancing lands) " +
            "hardpoint headroom. Meaningless for FixedWing/FlyingWingStealth/CcaScale airframes; leave at 0.")]
        public int rotorCount;

        [Header("Phase 2B: MTOW")]
        [Tooltip("Maximum take-off mass in kg for this airframe (mirrors MissileAirframeDefinition.maxTakeOffMassKg). " +
            "0 = no limit configured.")]
        public float maxTakeOffMassKg;

        [Header("Visual Dimensions (fixed-wing/jet only, meters) — real-world-inspired")]
        [Tooltip("Wingtip-to-wingtip span in meters, used by DroneVisualBuilder.BuildFixedWingVisual to size the " +
            "procedural mesh. Deliberately real-world-referenced per planform (e.g. a Fury/YFQ-44A-class airframe " +
            "uses that aircraft's actual ~5.2m span) rather than one flat constant for every fixed-wing design — " +
            "see each seeded airframe's own comment for its real-world reference. Meaningless for multirotor " +
            "airframes (rotorCount > 0); leave at the default.")]
        public float wingSpanMeters = 6f;

        [Tooltip("Nose-to-tail body length in meters, same real-world-referenced convention as wingSpanMeters. " +
            "Meaningless for multirotor airframes.")]
        public float fuselageLengthMeters = 4f;
    }
}
