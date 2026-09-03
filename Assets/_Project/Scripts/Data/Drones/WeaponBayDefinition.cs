using UnityEngine;

namespace Vanquish.Data.Drones
{
    [CreateAssetMenu(menuName = "Vanquish/Drone/Weapon Bay", fileName = "NewWeaponBay")]
    public class WeaponBayDefinition : PartDefinition
    {
        [Header("Weapon Bay")]
        [Tooltip("Maximum combined mass of munitions this bay can carry, in kg — now actually enforced (see " +
            "DesignStatsCalculator.maxAmmoByMass) instead of a seeded-but-unread number.")]
        public float payloadCapacityKg;

        [Tooltip("Maximum number of discrete munitions this bay can carry in total (internal + external " +
            "combined) — now actually enforced as the real ammo cap (see DesignStatsCalculator/" +
            "WorkshopController's ammo stepper), not just a visual hardpoint-mesh cap.")]
        public int maxMunitionCount;

        [Tooltip("How many of maxMunitionCount are carried enclosed/hidden (no exposed RCS contribution, no " +
            "visible mounted-missile mesh) — the rest (maxMunitionCount - internalCapacity) are carried " +
            "externally on pylons, each adding to the airframe's exposed RCS and rendering as a mounted " +
            "missile. Internal capacity is always filled first (see VehicleFactory/DroneVisualBuilder): a " +
            "design under internalCapacity carries everything hidden; only ammo beyond that spills onto " +
            "visible external pylons. A bay with internalCapacity == 0 is a purely external rack (the old " +
            "isInternal == false); internalCapacity == maxMunitionCount is a purely internal bay (the old " +
            "isInternal == true) with no external overflow at all.")]
        public int internalCapacity;

        [Tooltip("Legacy flag, kept for existing data/back-compat: true is equivalent to internalCapacity == " +
            "maxMunitionCount (fully internal, no external overflow), false is equivalent to internalCapacity " +
            "== 0 (purely external). New bays should just set internalCapacity directly instead of this.")]
        public bool isInternal;

        [Tooltip("Time in seconds to cycle/reload between shots if applicable.")]
        public float cycleTimeSeconds;
    }
}
