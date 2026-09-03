using UnityEngine;

namespace Vanquish.Data.Missiles
{
    [CreateAssetMenu(menuName = "Vanquish/Missile/Engine", fileName = "NewMissileEngine")]
    public class MissileEngineDefinition : PartDefinition
    {
        [Header("Engine")]
        public PropulsionType propulsionType;

        [Tooltip("Newtons of thrust produced.")]
        public float thrustNewtons;

        [Tooltip("Seconds the engine burns before fuel is exhausted (paired with a Fuel part's capacity).")]
        public float burnTimeSeconds;

        [Tooltip("Maximum achievable speed in m/s under this engine's power, airframe drag pending.")]
        public float maxSpeedMetersPerSecond;

        [Tooltip("Heat signature contribution — increases IR detectability.")]
        public float infraredSignature;

        [Tooltip("Multiplies the airframe's maxGForce — engine type genuinely affects maneuverability, not " +
            "just speed/range: a solid rocket's short, violent boost tolerates hard corrections a sustained " +
            "airframe (ramjet/scramjet, optimized for straight-line hypersonic cruise) can't match. >1 = more " +
            "agile than the airframe alone allows, <1 = less. Added because before this field existed, engine " +
            "choice affected thrust/burn time/range only — maxGForce came from the airframe alone, so switching " +
            "from a Solid Rocket to a Scramjet changed how far/fast a missile flew but not how hard it could turn.")]
        public float maneuverabilityMultiplier = 1f;
    }
}
