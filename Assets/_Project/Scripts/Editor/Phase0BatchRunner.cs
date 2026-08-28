using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Headlessly opens the Phase 0 test scene, enters Play mode, lets it run for a
    /// fixed duration, then exits — so results/logs can be captured from the command
    /// line (-batchmode -nographics) without needing a human to click Play manually.
    ///
    /// Uses SessionState (survives the domain reload triggered by entering Play mode)
    /// plus [InitializeOnLoadMethod] to reliably re-subscribe the update tick after
    /// that reload — see Phase1BatchRunner for the detailed rationale.
    /// </summary>
    [InitializeOnLoad]
    public static class Phase0BatchRunner
    {
        private const string ScenePath = "Assets/_Project/Scenes/Phase0_MissileTest.unity";
        private const double TestDurationSeconds = 15.0;
        private const string RunningKey = "Vanquish.Phase0BatchRunner.Running";
        private const string StartTimeKey = "Vanquish.Phase0BatchRunner.StartTime";

        static Phase0BatchRunner()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Vanquish/Phase 0/Run Headless Test")]
        public static void RunHeadlessTest()
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
                Debug.Log("[Phase0BatchRunner] Test duration elapsed, exiting.");
                EditorApplication.isPlaying = false;
                EditorApplication.Exit(0);
            }
        }
    }
}
