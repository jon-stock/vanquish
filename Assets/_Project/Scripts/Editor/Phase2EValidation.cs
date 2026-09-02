using UnityEditor;
using UnityEngine;
using Vanquish.Combat;
using Vanquish.Core;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Headless sanity checks for Phase 2E's pluggable objective system
    /// (IObjective/DestroyAllEnemiesObjective/DestroyTargetObjective/CombatManager.
    /// BuildObjective's misconfiguration fallback). Terrain generation and the two new
    /// arena scenes themselves are verified separately via Phase2EArenaBuilder's own
    /// scene-build success + a full headless Play-mode regression (see PLAN.md's
    /// Phase 2E writeup) — nothing about a heightmap function is meaningfully
    /// unit-testable in isolation the way the objective decision logic is.
    /// </summary>
    public static class Phase2EValidation
    {
        [MenuItem("Vanquish/Phase 2E/Validate Objectives (Headless)")]
        public static void ValidateObjectives()
        {
            var enemyGo = new GameObject("Phase2EValidation_Enemy");
            var targetGo = new GameObject("Phase2EValidation_Target");
            var combatManagerGo = new GameObject("Phase2EValidation_CombatManager");
            var fallbackManagerGo = new GameObject("Phase2EValidation_FallbackCombatManager");
            try
            {
                var enemyHealth = enemyGo.AddComponent<Health>();
                enemyHealth.SetMaxHealth(10f);

                var targetHealth = targetGo.AddComponent<Health>();
                targetHealth.SetMaxHealth(10f);

                var combatManager = combatManagerGo.AddComponent<CombatManager>();
                combatManager.RegisterUnit(enemyGo, Team.Enemy);

                var allEnemiesObjective = new DestroyAllEnemiesObjective(combatManager);
                bool falseBeforeDamage = !allEnemiesObjective.IsVictoryAchieved();
                enemyHealth.TakeDamage(9999f);
                bool trueAfterDamage = allEnemiesObjective.IsVictoryAchieved();

                Debug.Log($"[Phase2EValidation] DestroyAllEnemiesObjective false while the registered enemy is alive: {(falseBeforeDamage ? "PASS" : "FAIL")}");
                Debug.Log($"[Phase2EValidation] DestroyAllEnemiesObjective true once the registered enemy is destroyed: {(trueAfterDamage ? "PASS" : "FAIL")}");

                var targetObjective = new DestroyTargetObjective(targetHealth, "Destroy the thing");
                bool targetFalseBeforeDamage = !targetObjective.IsVictoryAchieved();
                targetHealth.TakeDamage(9999f);
                bool targetTrueAfterDamage = targetObjective.IsVictoryAchieved();

                Debug.Log($"[Phase2EValidation] DestroyTargetObjective false while its target is alive: {(targetFalseBeforeDamage ? "PASS" : "FAIL")}");
                Debug.Log($"[Phase2EValidation] DestroyTargetObjective true once its specific target is destroyed: {(targetTrueAfterDamage ? "PASS" : "FAIL")}");
                Debug.Log($"[Phase2EValidation] DestroyTargetObjective description round-trips: {(targetObjective.Description == "Destroy the thing" ? "PASS" : "FAIL")}");

                // CombatManager.BuildObjective's misconfiguration fallback: set
                // objectiveType=DestroyTarget with no objectiveTarget assigned, then
                // call BuildObjective() directly (its only normal caller, Awake(),
                // never runs outside Play mode, so a headless edit-mode test can't
                // reach it via the component lifecycle at all) and confirm it silently
                // fell back to DestroyAllEnemies instead of leaving victory permanently
                // unreachable.
                var fallbackManager = fallbackManagerGo.AddComponent<CombatManager>();
                fallbackManager.objectiveType = ObjectiveType.DestroyTarget;
                fallbackManager.objectiveTarget = null;
                IObjective builtFallbackObjective = fallbackManager.BuildObjective();

                bool fellBackToDestroyAllEnemies = builtFallbackObjective.Description == "Destroy all enemy units.";
                Debug.Log($"[Phase2EValidation] Misconfigured DestroyTarget (no objectiveTarget) falls back to DestroyAllEnemies: {(fellBackToDestroyAllEnemies ? "PASS" : "FAIL")}");

                bool allPass = falseBeforeDamage && trueAfterDamage && targetFalseBeforeDamage && targetTrueAfterDamage
                    && targetObjective.Description == "Destroy the thing" && fellBackToDestroyAllEnemies;
                Debug.Log(allPass
                    ? "[Phase2EValidation] Pluggable objectives: ALL PASS"
                    : "[Phase2EValidation] Pluggable objectives: ONE OR MORE FAILURES ABOVE");
                if (!allPass)
                    Debug.LogError("[Phase2EValidation] Pluggable objectives validation FAILED.");
            }
            finally
            {
                Object.DestroyImmediate(enemyGo);
                Object.DestroyImmediate(targetGo);
                Object.DestroyImmediate(combatManagerGo);
                Object.DestroyImmediate(fallbackManagerGo);
            }
        }
    }
}
