using UnityEngine;

namespace Vanquish.Data.Drones
{
    public enum HullMaterialType
    {
        CompositePlastic,
        AluminumAlloy,
        CarbonFiber,
        RadarAbsorbentMaterial,
        TitaniumAlloy,
    }

    [CreateAssetMenu(menuName = "Vanquish/Drone/Hull Material", fileName = "NewHullMaterial")]
    public class HullMaterialDefinition : PartDefinition
    {
        [Header("Hull Material")]
        public HullMaterialType materialType;

        [Tooltip("Structural health/armor contribution.")]
        public float armorRating;

        [Tooltip("Density factor affecting mass per unit volume of airframe.")]
        public float densityFactor;

        [Tooltip("Multiplier applied to airframe base RCS (radar-absorbent materials should be < 1).")]
        [Range(0f, 1f)]
        public float radarCrossSectionMultiplier = 1f;

        [Tooltip("Max operating temperature before material degrades (relevant at high speed).")]
        public float maxTemperatureCelsius;
    }
}
