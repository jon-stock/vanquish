using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    /// Tracks win/lose conditions for a combat scene: victory once every enemy Health
    /// is destroyed, defeat once every player Health is destroyed. Awards currency to
    /// PlayerProgress on victory. One instance per combat scene.
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        public int victoryCurrencyReward = 100;
        public string workshopSceneName = "Workshop";
        public float resultToReturnDelaySeconds = 3f;

        public CombatResult Result { get; private set; } = CombatResult.InProgress;

        private readonly List<Health> _playerUnits = new List<Health>();
        private readonly List<Health> _enemyUnits = new List<Health>();

        private void Awake()
        {
            Instance = this;
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

            bool allEnemiesDown = AllDestroyed(_enemyUnits);
            bool allPlayersDown = AllDestroyed(_playerUnits);

            if (allEnemiesDown)
                DeclareResult(CombatResult.Victory);
            else if (allPlayersDown)
                DeclareResult(CombatResult.Defeat);
        }

        private static bool AllDestroyed(List<Health> units)
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
                ? $"[Combat] VICTORY — all enemy units destroyed. Awarding {victoryCurrencyReward} currency."
                : "[Combat] DEFEAT — all player units destroyed.");

            if (result == CombatResult.Victory && PlayerProgress.Instance != null)
                PlayerProgress.Instance.AddCurrency(victoryCurrencyReward);

            if (!string.IsNullOrEmpty(workshopSceneName))
                Invoke(nameof(ReturnToWorkshop), resultToReturnDelaySeconds);
        }

        private void ReturnToWorkshop()
        {
            SceneManager.LoadScene(workshopSceneName);
        }
    }
}
