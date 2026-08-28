using System.Collections.Generic;
using UnityEngine;
using Vanquish.Core;
using Vanquish.Simulation.Sensors;

namespace Vanquish.Combat
{
    /// <summary>
    /// Aggregates every DetectionSensor's contacts into a shared per-team "contact
    /// picture" each frame. This is what makes a scout drone useful: any enemy it
    /// detects becomes visible to the whole team (HUD radar, AI targeting) even if
    /// the player's own strike drone never got close enough to detect it directly.
    /// One instance should exist per combat scene (see CombatBootstrap).
    /// </summary>
    public class TeamAwareness : MonoBehaviour
    {
        public static TeamAwareness Instance { get; private set; }

        private readonly HashSet<DetectableSignature> _playerKnownEnemies = new HashSet<DetectableSignature>();
        private readonly HashSet<DetectableSignature> _enemyKnownPlayers = new HashSet<DetectableSignature>();

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void LateUpdate()
        {
            _playerKnownEnemies.Clear();
            _enemyKnownPlayers.Clear();

            var sensors = FindObjectsByType<DetectionSensor>(FindObjectsSortMode.None);
            foreach (var sensor in sensors)
            {
                var targetSet = sensor.ownerTeam == Team.Player ? _playerKnownEnemies : _enemyKnownPlayers;
                foreach (var contact in sensor.EnemyContacts)
                    targetSet.Add(contact);
            }
        }

        /// <summary>All enemy contacts currently known to the given team, from any of its sensors combined.</summary>
        public IEnumerable<DetectableSignature> GetKnownEnemies(Team team)
        {
            return team == Team.Player ? _playerKnownEnemies : _enemyKnownPlayers;
        }

        /// <summary>Convenience: nearest known enemy to a given position, or null if none known.</summary>
        public DetectableSignature GetNearestKnownEnemy(Team team, Vector3 fromPosition)
        {
            DetectableSignature nearest = null;
            float nearestSqrDist = float.MaxValue;
            foreach (var contact in GetKnownEnemies(team))
            {
                if (contact == null)
                    continue;
                float sqrDist = (contact.Position - fromPosition).sqrMagnitude;
                if (sqrDist < nearestSqrDist)
                {
                    nearestSqrDist = sqrDist;
                    nearest = contact;
                }
            }
            return nearest;
        }
    }
}
