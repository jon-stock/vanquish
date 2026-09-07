using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Vanquish.Combat.DebugTools;

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

        public static void BuildPhase1DebugScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Phase1ScenePath)!);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var harnessGo = new GameObject("Phase1 Debug Harness");
            harnessGo.AddComponent<EngagementDebugHarness>();

            bool saved = EditorSceneManager.SaveScene(scene, Phase1ScenePath);

            if (saved)
                Debug.Log($"[SceneBuilder] Saved {Phase1ScenePath}");
            else
                Debug.LogError($"[SceneBuilder] Failed to save {Phase1ScenePath}");

            EditorApplication.Exit(saved ? 0 : 1);
        }
    }
}
