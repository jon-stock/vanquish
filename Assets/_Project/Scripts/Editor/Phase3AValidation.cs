using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Vanquish.Core;
using Vanquish.Data.Scenarios;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Headless checks for Phase 3A's connected flow: GameFlowController.
    /// ResolveCombatScene's pure fallback logic, that every SceneNames constant
    /// actually corresponds to a scene file on disk, and that Build Settings
    /// registers MainMenu.unity at index 0 (required for it to be the app's real
    /// entry point) alongside Workshop/TestRange/the default combat arena. Does not
    /// attempt a live multi-scene Play-mode traversal (SceneManager.LoadScene across
    /// separate Play sessions isn't reliably scriptable from a single headless Editor
    /// invocation) — that's covered by manually clicking through the flow once per
    /// this sub-milestone's own exit criteria, same as Phase1WorkshopSmokeTest's own
    /// "crash smoke check, not full UI interaction" scope note.
    /// </summary>
    public static class Phase3AValidation
    {
        [MenuItem("Vanquish/Phase 3A/Validate Connected Flow (Headless)")]
        public static void ValidateConnectedFlow()
        {
            bool allPassed = true;

            allPassed &= ValidateResolveCombatScene();
            allPassed &= ValidateSceneFilesExist();
            allPassed &= ValidateBuildSettingsRegistration();

            Debug.Log(allPassed
                ? "[Phase3AValidation] All connected-flow checks PASSED."
                : "[Phase3AValidation] One or more connected-flow checks FAILED — see log above.");
        }

        private static bool ValidateResolveCombatScene()
        {
            bool pass = true;

            string withNoPending = GameFlowController.ResolveCombatScene(null, "SomeFallbackScene");
            bool fallbackUsedWhenNoPending = withNoPending == "SomeFallbackScene";
            Debug.Log($"[Phase3AValidation] No pending scenario falls back to the caller-provided scene: {(fallbackUsedWhenNoPending ? "PASS" : "FAIL")}");
            pass &= fallbackUsedWhenNoPending;

            string withNoPendingNoFallback = GameFlowController.ResolveCombatScene(null, null);
            bool defaultArenaUsedWhenNothingProvided = withNoPendingNoFallback == SceneNames.DefaultCombatArena;
            Debug.Log($"[Phase3AValidation] No pending scenario and no fallback resolves to SceneNames.DefaultCombatArena: {(defaultArenaUsedWhenNothingProvided ? "PASS" : "FAIL")}");
            pass &= defaultArenaUsedWhenNothingProvided;

            var scenario = ScriptableObject.CreateInstance<ScenarioDefinition>();
            try
            {
                scenario.sceneName = "Combat_Arena_Valley";
                string withPending = GameFlowController.ResolveCombatScene(scenario, "SomeFallbackScene");
                bool pendingScenarioWins = withPending == "Combat_Arena_Valley";
                Debug.Log($"[Phase3AValidation] A pending scenario overrides the fallback scene: {(pendingScenarioWins ? "PASS" : "FAIL")}");
                pass &= pendingScenarioWins;
            }
            finally
            {
                Object.DestroyImmediate(scenario);
            }

            return pass;
        }

        private static bool ValidateSceneFilesExist()
        {
            bool pass = true;
            pass &= CheckSceneFileExists(SceneNames.MainMenu);
            pass &= CheckSceneFileExists(SceneNames.Workshop);
            pass &= CheckSceneFileExists(SceneNames.TestRange);
            pass &= CheckSceneFileExists(SceneNames.DefaultCombatArena);
            return pass;
        }

        private static bool CheckSceneFileExists(string sceneName)
        {
            string path = $"Assets/_Project/Scenes/{sceneName}.unity";
            bool exists = File.Exists(path);
            Debug.Log($"[Phase3AValidation] Scene file exists for '{sceneName}' ({path}): {(exists ? "PASS" : "FAIL")}");
            return exists;
        }

        private static bool ValidateBuildSettingsRegistration()
        {
            var scenes = EditorBuildSettings.scenes;

            int mainMenuIndex = System.Array.FindIndex(scenes, s => s.path.EndsWith($"{SceneNames.MainMenu}.unity"));
            bool mainMenuAtIndexZero = mainMenuIndex == 0;
            Debug.Log($"[Phase3AValidation] MainMenu.unity registered at build index 0 (actual index: {mainMenuIndex}): {(mainMenuAtIndexZero ? "PASS" : "FAIL")}");

            bool workshopRegistered = scenes.Any(s => s.path.EndsWith($"{SceneNames.Workshop}.unity"));
            Debug.Log($"[Phase3AValidation] Workshop.unity registered in Build Settings: {(workshopRegistered ? "PASS" : "FAIL")}");

            bool testRangeRegistered = scenes.Any(s => s.path.EndsWith($"{SceneNames.TestRange}.unity"));
            Debug.Log($"[Phase3AValidation] TestRange.unity registered in Build Settings: {(testRangeRegistered ? "PASS" : "FAIL")}");

            bool defaultArenaRegistered = scenes.Any(s => s.path.EndsWith($"{SceneNames.DefaultCombatArena}.unity"));
            Debug.Log($"[Phase3AValidation] {SceneNames.DefaultCombatArena}.unity registered in Build Settings: {(defaultArenaRegistered ? "PASS" : "FAIL")}");

            return mainMenuAtIndexZero && workshopRegistered && testRangeRegistered && defaultArenaRegistered;
        }
    }
}
