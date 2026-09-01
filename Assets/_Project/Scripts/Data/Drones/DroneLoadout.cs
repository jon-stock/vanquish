using System;
using UnityEngine;
using Vanquish.Data.Missiles;
using Vanquish.Data.Shared;

namespace Vanquish.Data.Drones
{
    /// <summary>
    /// A player-assembled drone design: one part chosen per slot, plus the missile
    /// loadout it carries. Not a ScriptableObject asset — see MissileLoadout for the
    /// same rationale.
    /// </summary>
    [Serializable]
    public class DroneLoadout
    {
        public string designName = "Drone";

        public PropulsionDefinition propulsion;
        public DroneAirframeDefinition airframe;
        public WingOrPropellerDefinition wingOrPropeller;
        public HullMaterialDefinition hullMaterial;
        public DroneEngineDefinition engine;
        public FuelDefinition fuel;
        public WeaponBayDefinition weaponBay;
        public SensorSuiteDefinition sensorSuite;

        public MissileLoadout missileLoadout;
        public int ammoCount = 4;

        [Tooltip("Optional decoy/flare-chaff countermeasure (Phase 2C) — reuses the same " +
            "CountermeasureDefinition type MissileLoadout uses, since decoy equipment logically belongs " +
            "to whatever's defending against an inbound missile, not the missile itself. Backs a runtime " +
            "CountermeasureController (see Simulation.Sensors) that can auto-deploy decoys to break an " +
            "inbound missile's lock.")]
        public CountermeasureDefinition countermeasure; // optional, may be null

        [Tooltip("Continuous fuel/battery fill level (0 = empty, 1 = full capacity) — the drone-side " +
            "equivalent of MissileLoadout.fuelFillFraction, needed for Phase 2B's MTOW validation to treat " +
            "the fuel/battery slider as a real mass trade-off rather than always assuming a full tank.")]
        [Range(0f, 1f)]
        public float fuelFillFraction = 1f;

        public bool IsComplete => propulsion != null && airframe != null && wingOrPropeller != null &&
                                   hullMaterial != null && engine != null && fuel != null && weaponBay != null &&
                                   sensorSuite != null;
    }
}
