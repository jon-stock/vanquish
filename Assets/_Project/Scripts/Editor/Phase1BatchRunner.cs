using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Headlessly opens the Phase 1 combat arena, enters Play mode, and lets it run
    /// long enough for the AI logic to resolve a battle outcome, then exits. See
    /// docs/CODING_STANDARDS.md's headless testing workflow.
    ///
    /// Entering Play mode triggers a domain reload, which clears any plain static
    /// event subscription (e.g. `EditorApplication.update += Tick`) made before the
    /// reload — so this uses SessionState (which survives domain reloads within the
    /// same Editor process) to track that a test is running and re-subscribes via
    /// [InitializeOnLoadMethod], which re-runs after every reload.
    /// </summary>
    [InitializeOnLoad]
    public static class Phase1BatchRunner
    {
        private const string ScenePath = "Assets/_Project/Scenes/Combat_Arena01.unity";
        private const double TestDurationSeconds = 60.0;
        private const string RunningKey = "Vanquish.Phase1BatchRunner.Running";
        private const string StartTimeKey = "Vanquish.Phase1BatchRunner.StartTime";

        static Phase1BatchRunner()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Vanquish/Phase 1/Run Headless Combat Test")]
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
                Debug.Log("[Phase1BatchRunner] Test duration elapsed, exiting.");
                EditorApplication.isPlaying = false;
                EditorApplication.Exit(0);
            }
        }
    }
}
