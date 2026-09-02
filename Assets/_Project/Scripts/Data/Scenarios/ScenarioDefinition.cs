using UnityEngine;

namespace Vanquish.Data.Scenarios
{
    /// <summary>
    /// Phase 2E: lightweight scenario metadata — which pre-built combat scene to load
    /// and what to tell the player about it before they commit. Deliberately NOT a
    /// full "scene description" spec (no unit-placement data, no terrain parameters):
    /// every arena in this project is authored via a scripted Editor scene-builder
    /// (Phase1CombatSceneBuilder, Phase2EArenaBuilder) that bakes everything —
    /// including the CombatManager's objectiveType/objectiveTarget — directly into the
    /// saved .unity scene, matching the project's existing "everything reproducible
    /// via code, not hand-placed" convention. This asset is just the picker's index
    /// card for one of those pre-built scenes, not the source of truth for what's in
    /// it — the actual objective logic lives on that scene's own baked CombatManager.
    /// </summary>
    [CreateAssetMenu(menuName = "Vanquish/Scenario", fileName = "NewScenario")]
    public class ScenarioDefinition : ScriptableObject
    {
        public string id;
        public string displayName;

        [TextArea(2, 4)]
        public string description;

        [Tooltip("Scene name (not path) to load via SceneManager.LoadScene — must be registered in " +
            "Build Settings or the load will fail at runtime.")]
        public string sceneName;

        [Tooltip("Purely descriptive, shown on the picker before entering combat — the actual " +
            "objective logic lives on the target scene's own baked CombatManager " +
            "(objectiveType/objectiveTarget), not here.")]
        public string objectiveSummary;
    }
}
