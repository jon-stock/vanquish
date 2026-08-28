using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Headlessly opens the Phase 0 test scene, enters Play mode, lets it run for a
    /// fixed duration, then exits — so results/logs can be captured from the command
    /// line (-batchmode -nographics) without needing a human to click Play manually.
    /// </summary>
    public static class Phase0BatchRunner
    {
        private const string ScenePath = "Assets/_Project/Scenes/Phase0_MissileTest.unity";
        private const double TestDurationSeconds = 15.0;

        private static double _startTime;
        private static bool _started;

        [MenuItem("Vanquish/Phase 0/Run Headless Test")]
        public static void RunHeadlessTest()
        {
            EditorSceneManager.OpenScene(ScenePath);
            _started = false;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying)
                return;

            if (!_started)
            {
                _startTime = EditorApplication.timeSinceStartup;
                _started = true;
                Debug.Log("[Phase0BatchRunner] Entered Play mode, test running...");
            }

            if (EditorApplication.timeSinceStartup - _startTime > TestDurationSeconds)
            {
                EditorApplication.update -= Tick;
                Debug.Log("[Phase0BatchRunner] Test duration elapsed, exiting.");
                EditorApplication.isPlaying = false;
                EditorApplication.Exit(0);
            }
        }
    }
}
