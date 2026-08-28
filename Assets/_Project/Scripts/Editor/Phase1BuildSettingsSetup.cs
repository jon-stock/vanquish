using UnityEditor;
using UnityEngine;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Registers the Workshop and Combat scenes in Build Settings so
    /// SceneManager.LoadScene works reliably in actual builds, not just the Editor
    /// (where any scene can be loaded by path regardless of Build Settings).
    /// </summary>
    public static class Phase1BuildSettingsSetup
    {
        [MenuItem("Vanquish/Phase 1/Register Scenes In Build Settings")]
        public static void RegisterScenes()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/_Project/Scenes/Workshop.unity", true),
                new EditorBuildSettingsScene("Assets/_Project/Scenes/Combat_Arena01.unity", true),
            };
            EditorBuildSettings.scenes = scenes;
            Debug.Log("[Phase1BuildSettingsSetup] Registered Workshop and Combat_Arena01 in Build Settings.");
        }
    }
}
