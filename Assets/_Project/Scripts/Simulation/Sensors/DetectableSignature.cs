using UnityEngine;
using Vanquish.Core;

namespace Vanquish.Simulation.Sensors
{
    /// <summary>
    /// Concrete MonoBehaviour implementation of IDetectable. Attach to any drone,
    /// missile, or installation that should be discoverable by sensors.
    /// </summary>
    public class DetectableSignature : MonoBehaviour, IDetectable
    {
        [Tooltip("Radar cross-section in m^2, after all stealth/countermeasure modifiers are baked in at spawn time.")]
        public float radarCrossSection = 1f;

        [Tooltip("Infrared signature, arbitrary units, after all modifiers are baked in at spawn time.")]
        public float infraredSignature = 1f;

        [Tooltip("Which side this unit belongs to, used to filter friend/foe in team-aware detection and AI targeting.")]
        public Team team = Team.Enemy;

        public Vector3 Position => transform.position;
        float IDetectable.RadarCrossSection => radarCrossSection;
        float IDetectable.InfraredSignature => infraredSignature;
    }
}
