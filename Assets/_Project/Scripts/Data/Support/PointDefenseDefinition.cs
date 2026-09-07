using UnityEngine;

namespace Vanquish.Data.Support
{
    /// <summary>
    /// How this point-defense system physically engages targets. This is the
    /// consolidation called for in PLAN.md's "Radar & SEAD" pivot: SAM sites,
    /// gun-based interceptors/CIWS, and future directed-energy defenses are all one
    /// <see cref="PartCategory.SupportPointDefense"/> category sharing common stats,
    /// distinguished only by this implementation sub-type. Player-facing UI/flavor
    /// text may still informally call any of these "SAM".
    /// </summary>
    public enum PointDefenseImplementation
    {
        /// <summary>Guided interceptor missile (classic SAM site).</summary>
        Missile,

        /// <summary>Rapid-fire gun/cannon (CIWS-style close-in defense).</summary>
        Gun,

        /// <summary>Future tech tier — directed-energy point defense.</summary>
        DirectedEnergy,
    }

    [CreateAssetMenu(menuName = "Vanquish/Support/Point Defense", fileName = "NewPointDefense")]
    public class PointDefenseDefinition : PartDefinition
    {
        [Header("Point Defense")]
        public PointDefenseImplementation implementation;

        public float engagementRangeMeters;
        public float rateOfFirePerSecond;

        [Range(0f, 1f)]
        public float interceptProbability;

        public float health;

        [Tooltip(
            "Finite ammo/interceptor count this system starts an engagement with — " +
            "the resource being drained by the stockpile-economy tactic (baiting an " +
            "expensive interceptor into firing at a cheap decoy). 0/negative means " +
            "unlimited (e.g. a gun with no meaningful ammo constraint at this tier).")]
        public int ammoCount;
    }
}
