using System;
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

        public bool IsComplete => propulsion != null && airframe != null && wingOrPropeller != null &&
                                   hullMaterial != null && engine != null && fuel != null && weaponBay != null &&
                                   sensorSuite != null;
    }
}
