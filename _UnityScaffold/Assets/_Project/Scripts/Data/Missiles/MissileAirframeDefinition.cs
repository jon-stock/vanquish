using UnityEngine;

namespace Vanquish.Data.Missiles
{
    [CreateAssetMenu(menuName = "Vanquish/Missile/Airframe", fileName = "NewMissileAirframe")]
    public class MissileAirframeDefinition : PartDefinition
    {
        [Header("Airframe / Material")]
        [Tooltip("Aerodynamic drag coefficient — lower is more efficient at speed.")]
        public float dragCoefficient;

        [Tooltip("Structural mass added beyond payload/engine/fuel.")]
        public float structuralMassKg;

        [Tooltip("Maximum g-force the airframe can pull without structural failure — governs maneuverability ceiling.")]
        public float maxGForce;

        [Tooltip("Base radar cross-section contribution before countermeasure/shaping modifiers.")]
        public float baseRadarCrossSection;

        [Tooltip("Maximum sustained heat before airframe degrades (relevant for hypersonic tiers).")]
        public float maxTemperatureCelsius;
    }
}
