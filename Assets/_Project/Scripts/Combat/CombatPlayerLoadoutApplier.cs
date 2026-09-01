using UnityEngine;
using Vanquish.Core;
using Vanquish.Data.Drones;

namespace Vanquish.Combat
{
    /// <summary>
    /// Replaces Combat_Arena01's editor-time-baked player/scout drones (see
    /// Phase1CombatSceneBuilder, which spawns them from a hardcoded Tier-0
    /// DroneLoadout) with ones built from whatever the player actually configured in
    /// the Workshop's part picker, if any. Without this, the Workshop's multi-option
    /// drone picker (Phase 2B) would be purely cosmetic — it computes preview stats
    /// but the "Enter Combat" button always loaded the same static scene regardless of
    /// what was selected.
    ///
    /// Runs in Awake() rather than Start() deliberately: Unity guarantees every
    /// Awake() in a newly-loaded scene runs before any Start(), so the baked units are
    /// already swapped out before CombatManager.Start() scans the scene for
    /// DetectableSignature components to register for win/lose tracking — the
    /// replacement units get registered, not the (by then destroyed) originals.
    ///
    /// No-ops entirely — leaving Combat_Arena01's baked units untouched — when
    /// PlayerProgress doesn't exist or has no pending loadout, so this still works for
    /// headless regression testing (Phase1BatchRunner) and opening Combat_Arena01
    /// directly without ever visiting the Workshop scene.
    /// </summary>
    public class CombatPlayerLoadoutApplier : MonoBehaviour
    {
        [Tooltip("Name of the baked player drone GameObject to replace (see Phase1CombatSceneBuilder).")]
        public string playerDroneName = "Player_Drone";

        [Tooltip("Name of the baked scout drone GameObject to replace (see Phase1CombatSceneBuilder).")]
        public string scoutDroneName = "Scout_Drone";

        private void Awake()
        {
            PlayerProgress progress = PlayerProgress.Instance;
            if (progress == null)
                return;

            if (progress.PendingStrikeDroneLoadout != null && progress.PendingStrikeDroneLoadout.IsComplete)
                ReplacePlayerDrone(progress.PendingStrikeDroneLoadout);

            if (progress.PendingScoutDroneLoadout != null && progress.PendingScoutDroneLoadout.IsComplete)
                ReplaceScoutDrone(progress.PendingScoutDroneLoadout);
        }

        private void ReplacePlayerDrone(DroneLoadout loadout)
        {
            GameObject oldDrone = GameObject.Find(playerDroneName);
            if (oldDrone == null)
            {
                Debug.LogWarning($"[CombatPlayerLoadoutApplier] Could not find '{playerDroneName}' to replace — " +
                    "leaving the baked default in place.");
                return;
            }

            Vector3 position = oldDrone.transform.position;
            Quaternion rotation = oldDrone.transform.rotation;

            // SetActive(false) takes effect immediately (unlike Destroy(), which is
            // deferred to end-of-frame) so CombatManager.Start()'s FindObjectsByType
            // scan — which only finds active objects by default — won't pick up the
            // old drone even though it technically still exists until Destroy() flushes.
            oldDrone.SetActive(false);
            Destroy(oldDrone);

            GameObject newDrone = VehicleFactory.SpawnDrone(loadout, position, rotation, Team.Player);
            newDrone.name = playerDroneName;
            newDrone.AddComponent<PlayerDroneController>();

            // Re-point every other system that referenced the old player transform by
            // direct reference (camera follow target, HUD readouts).
            var chaseCamera = FindAnyObjectByType<Phase0ChaseCamera>();
            if (chaseCamera != null)
            {
                chaseCamera.missile = newDrone.transform;
                chaseCamera.target = newDrone.transform;
            }

            var hud = FindAnyObjectByType<HUDController>();
            if (hud != null)
            {
                hud.player = newDrone.transform;
                hud.playerHealth = newDrone.GetComponent<Health>();
                hud.playerWeapon = newDrone.GetComponent<WeaponController>();
            }
        }

        private void ReplaceScoutDrone(DroneLoadout loadout)
        {
            GameObject oldDrone = GameObject.Find(scoutDroneName);
            if (oldDrone == null)
            {
                Debug.LogWarning($"[CombatPlayerLoadoutApplier] Could not find '{scoutDroneName}' to replace — " +
                    "leaving the baked default in place.");
                return;
            }

            Vector3 position = oldDrone.transform.position;
            Quaternion rotation = oldDrone.transform.rotation;

            // Capture the old patrol's tuning so the replacement patrols the same area
            // instead of re-deriving arena geometry from scratch here.
            var oldPatrol = oldDrone.GetComponent<ScoutPatrol>();
            Vector3 arenaCenter = oldPatrol != null ? oldPatrol.arenaCenter : Vector3.zero;
            float patrolRadius = oldPatrol != null ? oldPatrol.patrolRadius : 250f;

            oldDrone.SetActive(false);
            Destroy(oldDrone);

            GameObject newDrone = VehicleFactory.SpawnDrone(loadout, position, rotation, Team.Player);
            newDrone.name = scoutDroneName;
            var newPatrol = newDrone.AddComponent<ScoutPatrol>();
            newPatrol.arenaCenter = arenaCenter;
            newPatrol.patrolRadius = patrolRadius;
        }
    }
}
