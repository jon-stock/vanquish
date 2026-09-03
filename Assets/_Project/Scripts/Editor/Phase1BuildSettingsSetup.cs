using UnityEditor;
using UnityEngine;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Registers the always-present scenes in Build Settings so
    /// SceneManager.LoadScene works reliably in actual builds, not just the Editor
    /// (where any scene can be loaded by path regardless of Build Settings). Phase 3A:
    /// changed from destructively overwriting EditorBuildSettings.scenes wholesale
    /// (which used to wipe out every other scene builder's own registration —
    /// Phase2EArenaBuilder's arenas, TestRangeSceneBuilder's Test Range, and now
    /// MainMenuSceneBuilder's required build-index-0 placement) to the same
    /// additive/idempotent EnsureSceneInBuildSettings(AtIndex) helpers every other
    /// scene builder already uses, so running this after those has no destructive
    /// effect. Scenario-specific arenas aren't listed here — each arena builder
    /// registers itself.
    /// </summary>
    public static class Phase1BuildSettingsSetup
    {
        [MenuItem("Vanquish/Phase 1/Register Scenes In Build Settings")]
        public static void RegisterScenes()
        {
            Phase1CombatSceneBuilder.EnsureSceneInBuildSettingsAtIndex("Assets/_Project/Scenes/MainMenu.unity", 0);
            Phase1CombatSceneBuilder.EnsureSceneInBuildSettings("Assets/_Project/Scenes/Workshop.unity");
            Phase1CombatSceneBuilder.EnsureSceneInBuildSettings("Assets/_Project/Scenes/Combat_Arena01.unity");
            Phase1CombatSceneBuilder.EnsureSceneInBuildSettings("Assets/_Project/Scenes/TestRange.unity");
            Debug.Log("[Phase1BuildSettingsSetup] Ensured MainMenu (index 0), Workshop, Combat_Arena01, and TestRange are registered in Build Settings.");
        }
    }
}
