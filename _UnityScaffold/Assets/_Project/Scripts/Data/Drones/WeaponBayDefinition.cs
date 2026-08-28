using UnityEngine;

namespace Vanquish.Data.Drones
{
    [CreateAssetMenu(menuName = "Vanquish/Drone/Weapon Bay", fileName = "NewWeaponBay")]
    public class WeaponBayDefinition : PartDefinition
    {
        [Header("Weapon Bay")]
        [Tooltip("Maximum combined mass of munitions this bay can carry, in kg.")]
        public float payloadCapacityKg;

        [Tooltip("Maximum number of discrete munitions (e.g. missiles/bombs) regardless of mass, if relevant.")]
        public int maxMunitionCount;

        [Tooltip("If true, munitions are enclosed (internal bay) and do not add to the airframe's exposed RCS.")]
        public bool isInternal;

        [Tooltip("Time in seconds to cycle/reload between shots if applicable.")]
        public float cycleTimeSeconds;
    }
}
