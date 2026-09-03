using System.Collections.Generic;
using UnityEngine;
using Vanquish.Core;
using Vanquish.Simulation.Sensors;

namespace Vanquish.Combat
{
    public enum CombatResult
    {
        InProgress,
        Victory,
        Defeat,
    }

    /// <summary>
    /// Phase 2E: which victory-condition strategy this scene's CombatManager should
    /// build in Awake — see IObjective's own doc comment for why this is stored as a
    /// serializable (enum, GameObject reference) pair rather than a live IObjective
    /// reference directly.
    /// </summary>
    public enum ObjectiveType
    {
        DestroyAllEnemies,
        DestroyTarget,
    }

    /// <summary>
    /// Tracks win/lose conditions for a combat scene: defeat once every player Health
    /// is destroyed (universal, every scenario); victory per a pluggable IObjective
    /// (Phase 2E — was hardcoded "every enemy Health destroyed", now the default of
    /// two strategies). Awards currency to PlayerProgress on victory. One instance per
    /// combat scene.
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        public int victoryCurrencyReward = 100;
        public string workshopSceneName = SceneNames.Workshop;
        public float resultToReturnDelaySeconds = 3f;

        [Header("Objective (Phase 2E)")]
        public ObjectiveType objectiveType = ObjectiveType.DestroyAllEnemies;

        [Tooltip("Required when objectiveType == DestroyTarget: the specific enemy unit (must have " +
            "a Health component) that must be destroyed for victory, independent of any other enemy " +
            "units in the scene. Ignored for DestroyAllEnemies.")]
        public GameObject objectiveTarget;

        [Tooltip("Player-facing description shown alongside the VICTORY/DEFEAT banner for " +
            "DestroyTarget objectives. Ignored for DestroyAllEnemies (which has a fixed description).")]
        public string objectiveTargetDescription = "Destroy the designated target.";

        public CombatResult Result { get; private set; } = CombatResult.InProgress;

        /// <summary>Player-facing objective description, for HUDController to display alongside the result banner.</summary>
        public string ObjectiveDescription => _objective?.Description ?? string.Empty;

        public IReadOnlyList<Health> PlayerUnits => _playerUnits;
        public IReadOnlyList<Health> EnemyUnits => _enemyUnits;

        private readonly List<Health> _playerUnits = new List<Health>();
        private readonly List<Health> _enemyUnits = new List<Health>();
        private IObjective _objective;

        private void Awake()
        {
            Instance = this;
            _objective = BuildObjective();
        }

        /// <summary>
        /// Constructs the runtime strategy object from this component's serialized
        /// configuration — the actual "pluggable objective" seam. Falls back to
        /// DestroyAllEnemies (preserving every pre-2E scene's exact original behavior)
        /// if DestroyTarget is selected but misconfigured, rather than throwing or
        /// leaving victory permanently unreachable. Public (not private) so
        /// Phase2EValidation (in the separate Editor assembly, where `internal`
        /// wouldn't be visible) can exercise this decision directly — CombatManager's
        /// Awake() (the only normal runtime caller) never runs outside Play mode, so a
        /// headless edit-mode test can't reach this via the component lifecycle at all.
        /// </summary>
        public IObjective BuildObjective()
        {
            if (objectiveType == ObjectiveType.DestroyTarget)
            {
                Health targetHealth = objectiveTarget != null ? objectiveTarget.GetComponent<Health>() : null;
                if (targetHealth != null)
                    return new DestroyTargetObjective(targetHealth, objectiveTargetDescription);

                Debug.LogWarning("[CombatManager] objectiveType=DestroyTarget but objectiveTarget is null or has " +
                    "no Health component — falling back to DestroyAllEnemies.");
            }

            return new DestroyAllEnemiesObjective(this);
        }

        private void Start()
        {
            // VehicleFactory.RegisterUnit only fires when a unit is spawned, which for
            // this scene happened once at edit-time when the scene was scripted into
            // existence (see Phase1CombatSceneBuilder) — that registration is pure
            // runtime C# state (event subscriptions, list membership) that is NOT
            // saved into the scene file, so a fresh Play session would otherwise start
            // with empty unit lists and victory/defeat could never fire. Scan the
            // already-loaded scene for units instead, so this works regardless of
            // whether units were registered at spawn time or already existed in the
            // scene when it loaded.
            var allSignatures = FindObjectsByType<DetectableSignature>(FindObjectsSortMode.None);
            foreach (var signature in allSignatures)
            {
                RegisterUnit(signature.gameObject, signature.team);
            }
        }

        /// <summary>Registers a unit for win/lose tracking. Safe to call multiple times for the same unit.</summary>
        public void RegisterUnit(GameObject unit, Team team)
        {
            var health = unit.GetComponent<Health>();
            if (health == null)
                return;

            var targetList = team == Team.Player ? _playerUnits : _enemyUnits;
            if (targetList.Contains(health))
                return;

            health.OnDestroyed += OnUnitDestroyed;
            targetList.Add(health);
        }

        private void OnUnitDestroyed(Health health)
        {
            if (Result != CombatResult.InProgress)
                return;

            // Defeat triggers the moment the actual player-controlled craft
            // (PlayerDroneController — always added to whichever unit the player
            // flies, see Phase1CombatSceneBuilder/CombatPlayerLoadoutApplier) is
            // destroyed, regardless of whether an unarmed escort like the scout
            // drone is still alive. Previously this required every single
            // Team.Player unit (including the scout) to be destroyed, which meant
            // losing the drone you're actually flying didn't end the match at all
            // as long as the scout survived — the match just hung in InProgress
            // forever with the HUD referencing a destroyed player transform.
            bool playerControlledUnitDown = health.GetComponent<PlayerDroneController>() != null;
            bool victoryAchieved = _objective != null && _objective.IsVictoryAchieved();

            if (victoryAchieved)
                DeclareResult(CombatResult.Victory);
            else if (playerControlledUnitDown)
                DeclareResult(CombatResult.Defeat);
        }

        /// <summary>Public (was private) so IObjective implementations — DestroyAllEnemiesObjective
        /// in particular — can reuse the exact same "list non-empty and every entry destroyed" rule.</summary>
        public static bool AllDestroyed(IReadOnlyList<Health> units)
        {
            if (units.Count == 0)
                return false;
            foreach (var unit in units)
            {
                if (unit != null && !unit.IsDestroyed)
                    return false;
            }
            return true;
        }

        private void DeclareResult(CombatResult result)
        {
            Result = result;
            Debug.Log(result == CombatResult.Victory
                ? $"[Combat] VICTORY — {ObjectiveDescription} Awarding {victoryCurrencyReward} currency."
                : "[Combat] DEFEAT — the player-controlled drone was destroyed.");

            if (result == CombatResult.Victory && PlayerProgress.Instance != null)
                PlayerProgress.Instance.AddCurrency(victoryCurrencyReward);

            if (!string.IsNullOrEmpty(workshopSceneName))
                Invoke(nameof(ReturnToWorkshop), resultToReturnDelaySeconds);
        }

        private void ReturnToWorkshop()
        {
            GameFlowController.ReturnToWorkshop(workshopSceneName);
        }

        /// <summary>
        /// Lets the player skip the resultToReturnDelaySeconds wait instead of
        /// forcing them to watch the VICTORY/DEFEAT banner for the full delay —
        /// wired to HUDController's "Return to Workshop" button. Cancels the
        /// scheduled auto-return Invoke first so clicking the button doesn't leave a
        /// second LoadScene queued up behind it. Safe to call while
        /// Result == InProgress (does nothing — HUDController only shows the button
        /// once a result exists) or after workshopSceneName was left empty (matches
        /// DeclareResult's own "no configured destination" behavior).
        /// </summary>
        public void ReturnToWorkshopNow()
        {
            if (Result == CombatResult.InProgress || string.IsNullOrEmpty(workshopSceneName))
                return;

            CancelInvoke(nameof(ReturnToWorkshop));
            ReturnToWorkshop();
        }
    }
}
