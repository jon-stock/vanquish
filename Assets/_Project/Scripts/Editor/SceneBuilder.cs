using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Vanquish.Combat.DebugTools;
using Vanquish.Theatre.DebugTools;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// One-off headless scene-authoring utilities, run via:
    ///   Unity.exe -batchmode -nographics -projectPath &lt;repo&gt; -executeMethod
    ///   Vanquish.EditorTools.SceneBuilder.BuildPhase1DebugScene -quit
    /// Regenerate a scene with this if its harness setup logic ever changes shape
    /// enough that hand-editing isn't worth it — these scenes only ever contain one
    /// GameObject with a self-configuring debug harness component, so there's
    /// nothing hand-authored in them to lose.
    /// </summary>
    public static class SceneBuilder
    {
        private const string Phase1ScenePath = "Assets/_Project/Scenes/Phase1_DebugHarness.unity";
        private const string Phase2ScenePath = "Assets/_Project/Scenes/Phase2_TheatreDebugHarness.unity";

        public static void BuildPhase1DebugScene()
        {
            BuildSingleComponentScene<EngagementDebugHarness>(Phase1ScenePath, "Phase1 Debug Harness");
        }

        public static void BuildPhase2DebugScene()
        {
            BuildSingleComponentScene<TheatreDebugHarness>(Phase2ScenePath, "Phase2 Debug Harness");
        }

        private static void BuildSingleComponentScene<T>(string scenePath, string gameObjectName) where T : Component
        {
            Directory.CreateDirectory(Path.GetDirectoryName(scenePath)!);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var harnessGo = new GameObject(gameObjectName);
            harnessGo.AddComponent<T>();

            bool saved = EditorSceneManager.SaveScene(scene, scenePath);

            if (saved)
                Debug.Log($"[SceneBuilder] Saved {scenePath}");
            else
                Debug.LogError($"[SceneBuilder] Failed to save {scenePath}");

            EditorApplication.Exit(saved ? 0 : 1);
        }
    }
}
