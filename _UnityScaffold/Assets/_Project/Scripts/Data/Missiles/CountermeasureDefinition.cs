using UnityEngine;

namespace Vanquish.Data.Missiles
{
    [CreateAssetMenu(menuName = "Vanquish/Missile/Countermeasure", fileName = "NewCountermeasure")]
    public class CountermeasureDefinition : PartDefinition
    {
        [Header("Stealth")]
        [Tooltip("Multiplier applied to airframe base RCS, e.g. 0.5 halves radar cross-section.")]
        [Range(0f, 1f)]
        public float radarCrossSectionMultiplier = 1f;

        [Tooltip("Multiplier applied to engine infrared signature.")]
        [Range(0f, 1f)]
        public float infraredSignatureMultiplier = 1f;

        [Header("Maneuverability")]
        [Tooltip("Bonus applied to max G-force, e.g. thrust-vectoring or extra control surfaces.")]
        public float maxGForceBonus;

        [Header("Active Countermeasures")]
        [Tooltip("Number of flare/chaff charges carried, if any.")]
        public int decoyCharges;

        [Tooltip("Probability [0-1] a single decoy charge successfully spoofs an incoming seeker lock.")]
        [Range(0f, 1f)]
        public float decoySuccessChance;
    }
}
