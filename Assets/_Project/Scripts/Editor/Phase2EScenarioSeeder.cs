using System.IO;
using UnityEditor;
using UnityEngine;
using Vanquish.Data.Scenarios;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Seeds the ScenarioDefinition assets WorkshopController's in-UI scenario picker
    /// (Phase 3A; was ScenarioPickerOverlay pre-3A) lists in the Workshop scene. One
    /// per combat scene currently reachable: the original flat Phase 1 MVP arena, plus
    /// the two new Phase 2E terrain arenas.
    /// </summary>
    public static class Phase2EScenarioSeeder
    {
        private const string ScenariosDir = "Assets/_Project/Data/Scenarios";

        [MenuItem("Vanquish/Phase 2E/Seed Scenario Definitions")]
        public static void SeedScenarios()
        {
            EnsureDir(ScenariosDir);

            CreateOrReplace<ScenarioDefinition>($"{ScenariosDir}/Scenario_TierZeroSkirmish.asset", s =>
            {
                s.id = "scenario_tier0_skirmish";
                s.displayName = "Tier-0 Skirmish";
                s.description = "The original flat proving-ground arena: one enemy drone, no terrain, no surprises.";
                s.sceneName = "Combat_Arena01";
                s.objectiveSummary = "Destroy all enemy units.";
            });

            CreateOrReplace<ScenarioDefinition>($"{ScenariosDir}/Scenario_ValleyInterdiction.asset", s =>
            {
                s.id = "scenario_valley_interdiction";
                s.displayName = "Valley Interdiction";
                s.description = "A long valley with a SAM site dug in at the far end, guarded by a patrolling interceptor.";
                s.sceneName = "Combat_Arena_Valley";
                s.objectiveSummary = "Destroy the enemy SAM site guarding the valley (not every enemy unit).";
            });

            CreateOrReplace<ScenarioDefinition>($"{ScenariosDir}/Scenario_PlateauSkirmish.asset", s =>
            {
                s.id = "scenario_plateau_skirmish";
                s.displayName = "Plateau Skirmish";
                s.description = "A raised plateau with steep cliff edges — closer engagement ranges, blocked sightlines.";
                s.sceneName = "Combat_Arena_Plateau";
                s.objectiveSummary = "Destroy all enemy units.";
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase2EScenarioSeeder] Seeded 3 ScenarioDefinition assets under " +
                "Assets/_Project/Data/Scenarios/. Re-run Vanquish/Phase 1/Build Workshop Scene to pick " +
                "them up in the scenario picker.");
        }

        private static T CreateOrReplace<T>(string path, System.Action<T> configure) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                configure(existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            configure(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureDir(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
                string leaf = Path.GetFileName(path);
                if (!AssetDatabase.IsValidFolder(parent))
                    EnsureDir(parent);
                AssetDatabase.CreateFolder(parent, leaf);
            }
        }
    }
}
