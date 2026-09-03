using UnityEngine;
using UnityEngine.SceneManagement;
using Vanquish.Data.Scenarios;

namespace Vanquish.Core
{
    /// <summary>
    /// Phase 3A: the one reusable navigation primitive for the "Main Menu -> Workshop
    /// -> Test Range -> Combat -> Workshop" flow. Deliberately a static utility, not a
    /// DontDestroyOnLoad singleton MonoBehaviour like PlayerProgress — there is no
    /// cross-scene *state* to carry here (that's already PlayerProgress's job via
    /// PendingScenario/PendingStrikeDroneLoadout/PendingScoutDroneLoadout), just a
    /// single place that knows *how* to move between scenes. Being static also means
    /// every scene remains independently openable/testable (headless regression runs
    /// that jump straight into Combat_Arena01 without ever passing through Main Menu
    /// still work) without needing a bootstrap object present first.
    ///
    /// WorkshopController's future intra-scene "missile designer vs. drone/plane
    /// designer" mode split (Phase 3C) should reuse this same
    /// resolve-then-navigate shape (a pure resolution function plus a thin
    /// apply-the-result step) rather than inventing a second, differently-shaped
    /// mechanism — see PLAN.md Phase 3A/3C notes.
    /// </summary>
    public static class GameFlowController
    {
        /// <summary>The actual (mockable-by-inspection, side-effecting) scene load —
        /// every other method in this class funnels through here so there is exactly
        /// one place that calls SceneManager.LoadScene for the whole app-flow.</summary>
        public static void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[GameFlowController] LoadScene called with a null/empty scene name — ignoring.");
                return;
            }

            Debug.Log($"[GameFlowController] Loading scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }

        public static void LoadMainMenu() => LoadScene(SceneNames.MainMenu);

        public static void LoadWorkshop() => LoadScene(SceneNames.Workshop);

        /// <summary>Semantic alias for LoadWorkshop — used by every "return to
        /// designing" call site (CombatManager, TestRangeTelemetry) so the intent
        /// reads clearly at the call site even though it's the same scene.</summary>
        public static void ReturnToWorkshop(string workshopSceneNameOverride = null) =>
            LoadScene(string.IsNullOrEmpty(workshopSceneNameOverride) ? SceneNames.Workshop : workshopSceneNameOverride);

        public static void LoadTestRange(string testRangeSceneNameOverride = null) =>
            LoadScene(string.IsNullOrEmpty(testRangeSceneNameOverride) ? SceneNames.TestRange : testRangeSceneNameOverride);

        /// <summary>
        /// Resolves which combat scene to load: whatever scenario the player picked in
        /// the Workshop's in-UI scenario selector, if any, else the single hardcoded
        /// default so Combat_Arena01 stays directly reachable (headless regression
        /// tests, or a fresh save that never touched the scenario picker). Pure/static
        /// so it's headlessly unit-testable without a live scene — see
        /// Phase3AValidation.
        /// </summary>
        public static string ResolveCombatScene(ScenarioDefinition pendingScenario, string fallbackSceneName)
        {
            string fallback = string.IsNullOrEmpty(fallbackSceneName) ? SceneNames.DefaultCombatArena : fallbackSceneName;
            return pendingScenario != null && !string.IsNullOrEmpty(pendingScenario.sceneName)
                ? pendingScenario.sceneName
                : fallback;
        }

        public static void LoadCombat(ScenarioDefinition pendingScenario, string fallbackSceneName) =>
            LoadScene(ResolveCombatScene(pendingScenario, fallbackSceneName));
    }
}
