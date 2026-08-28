using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Headlessly opens the Workshop scene, enters Play mode briefly, and exits — a
    /// quick smoke test to catch NullReferenceExceptions on scene load/Start before
    /// asking a human to click through the UI (which OnGUI can't be scripted headlessly
    /// in a meaningful way, so that part is inherently a manual test).
    /// </summary>
    [InitializeOnLoad]
    public static class Phase1WorkshopSmokeTest
    {
        private const string ScenePath = "Assets/_Project/Scenes/Workshop.unity";
        private const double TestDurationSeconds = 5.0;
        private const string RunningKey = "Vanquish.Phase1WorkshopSmokeTest.Running";
        private const string StartTimeKey = "Vanquish.Phase1WorkshopSmokeTest.StartTime";

        static Phase1WorkshopSmokeTest()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Vanquish/Phase 1/Run Workshop Smoke Test")]
        public static void RunTest()
        {
            EditorSceneManager.OpenScene(ScenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetFloat(StartTimeKey, (float)EditorApplication.timeSinceStartup);
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying)
                return;

            double startTime = SessionState.GetFloat(StartTimeKey, (float)EditorApplication.timeSinceStartup);
            if (EditorApplication.timeSinceStartup - startTime > TestDurationSeconds)
            {
                EditorApplication.update -= Tick;
                SessionState.SetBool(RunningKey, false);
                Debug.Log("[Phase1WorkshopSmokeTest] Ran without crashing, exiting.");
                EditorApplication.isPlaying = false;
                EditorApplication.Exit(0);
            }
        }
    }
}
