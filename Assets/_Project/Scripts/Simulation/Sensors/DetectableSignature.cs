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

        [Tooltip("Phase 2D: true for units that can actually shoot back (a drone with a completed " +
            "WeaponBay+MissileLoadout, baked in at spawn time by VehicleFactory), false for unarmed " +
            "support units like scout drones. Lets role-aware AI archetypes (e.g. Interceptor) tell " +
            "'the player's strike drone' apart from any other same-team contact instead of only ever " +
            "chasing whichever contact happens to be nearest.")]
        public bool isArmed;

        [Tooltip("Phase 2D: true for units whose sensor suite shares contacts with its whole team " +
            "(SensorSuiteDefinition.sharesContactsWithTeam, baked in at spawn time by VehicleFactory) — " +
            "i.e. a scout. Lets role-aware AI archetypes (e.g. Scout-hunter) prioritize killing the unit " +
            "that's blinding-by-proxy the rest of its team's TeamAwareness, rather than just chasing " +
            "whichever contact happens to be nearest.")]
        public bool isScout;

        public Vector3 Position => transform.position;
        float IDetectable.RadarCrossSection => radarCrossSection;
        float IDetectable.InfraredSignature => infraredSignature;
    }
}
