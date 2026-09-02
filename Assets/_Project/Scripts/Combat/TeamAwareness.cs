using System;
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

        /// <summary>
        /// Convenience: nearest known enemy to a given position, or null if none known.
        /// Phase 2D: pass armedOnly=true to restrict the search to contacts with a
        /// completed weapon loadout (see DetectableSignature.isArmed) — this is what
        /// lets a role-aware archetype like Interceptor target "the player's strike
        /// drone specifically" instead of whichever contact merely happens to be
        /// closest (e.g. an unarmed scout escorting it).
        /// </summary>
        public DetectableSignature GetNearestKnownEnemy(Team team, Vector3 fromPosition, bool armedOnly = false)
        {
            return SelectNearest(GetKnownEnemies(team), fromPosition, armedOnly ? (c => c.isArmed) : (Func<DetectableSignature, bool>)null);
        }

        /// <summary>
        /// Phase 2D: nearest known enemy whose sensor suite shares contacts with its
        /// team (DetectableSignature.isScout) — i.e. a scout — or null if none known.
        /// Lets the Scout-hunter archetype specifically target the unit that's
        /// blinding-by-proxy the rest of its team's contact picture, rather than
        /// whichever contact merely happens to be closest.
        /// </summary>
        public DetectableSignature GetNearestKnownScoutEnemy(Team team, Vector3 fromPosition)
        {
            return SelectNearest(GetKnownEnemies(team), fromPosition, c => c.isScout);
        }

        /// <summary>
        /// Pure selection logic factored out of the role-aware Get* queries above so
        /// it can be headlessly unit-tested against a hand-built list of contacts (see
        /// Phase2DValidation) without needing a live scene of DetectionSensors for
        /// TeamAwareness to scan every LateUpdate. filter == null means "no role
        /// restriction, nearest of any role" (equivalent to the original Phase 1
        /// nearest-contact-only behavior).
        /// </summary>
        public static DetectableSignature SelectNearest(IEnumerable<DetectableSignature> contacts, Vector3 fromPosition, Func<DetectableSignature, bool> filter = null)
        {
            DetectableSignature nearest = null;
            float nearestSqrDist = float.MaxValue;
            foreach (var contact in contacts)
            {
                if (contact == null)
                    continue;
                if (filter != null && !filter(contact))
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
