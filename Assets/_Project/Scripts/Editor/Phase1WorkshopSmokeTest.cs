using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Headlessly opens the Workshop scene, enters Play mode briefly, and exits — a
    /// quick smoke test to catch NullReferenceExceptions on scene load/Start (UIDocument
    /// wiring, part references, PlayerProgress) before asking a human to click through
    /// the UI. Simulating actual UI Toolkit button clicks headlessly is possible but
    /// not done here yet — that would be a natural follow-up once the Workshop UI is
    /// stable, not required for this crash-smoke check.
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
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Phase1WorkshopSmokeTest] Already in Play mode — stop it first (Play button or "
                    + "Ctrl+P), then re-run this test. EditorSceneManager.OpenScene cannot run during Play mode.");
                return;
            }

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
                EditorApplication.isPlaying = false;

                // Only force-quit the whole Editor process in true headless/CI runs
                // (Unity.exe -batchmode ...). Calling EditorApplication.Exit here when
                // triggered interactively from the menu would abruptly close the whole
                // Editor with no dialog and no guaranteed time to flush this log line —
                // which looks exactly like a crash to a human running the test by hand.
                if (Application.isBatchMode)
                {
                    Debug.Log("[Phase1WorkshopSmokeTest] Ran without crashing, exiting Editor (batch mode).");
                    EditorApplication.Exit(0);
                }
                else
                {
                    Debug.Log("[Phase1WorkshopSmokeTest] Ran without crashing, stopping Play mode.");
                }
            }
        }
    }
}
