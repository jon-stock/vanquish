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
    }
}
