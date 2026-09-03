using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Vanquish.MainMenu;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Phase 3A: builds MainMenu.unity — the app's actual entry point, replacing
    /// Workshop.unity's previous role as the de facto first scene. Mirrors
    /// Phase1WorkshopSceneBuilder's pattern exactly (UIDocument + a PanelSettings
    /// asset + a background-only camera so the Game View doesn't show the "No cameras
    /// rendering" placeholder), but has no PlayerProgress/GameBootstrap of its own —
    /// PlayerProgress is created and Load()-ed by WorkshopController.Start() the first
    /// time the Workshop scene loads, same as before this scene existed; Main Menu
    /// itself needs no persistent state.
    /// </summary>
    public static class MainMenuSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/MainMenu.unity";
        private const string VisualTreeAssetPath = "Assets/_Project/UI/MainMenu/MainMenu.uxml";
        private const string PanelSettingsPath = "Assets/_Project/UI/MainMenu/MainMenuPanelSettings.asset";

        [MenuItem("Vanquish/Phase 3A/Build Main Menu Scene")]
        public static void BuildScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[MainMenuSceneBuilder] Cannot rebuild while in Play mode.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Background Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 0;
            cameraGo.AddComponent<AudioListener>();

            var menuGo = new GameObject("MainMenuController");
            var uiDocument = menuGo.AddComponent<UIDocument>();
            uiDocument.visualTreeAsset = Load<VisualTreeAsset>(VisualTreeAssetPath);
            uiDocument.panelSettings = GetOrCreatePanelSettings();
            menuGo.AddComponent<MainMenuController>();

            System.IO.Directory.CreateDirectory("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            // Build index 0 so this is the app's actual entry point (Editor Play
            // button/a real build both start from index 0) ahead of Workshop.unity.
            Phase1CombatSceneBuilder.EnsureSceneInBuildSettingsAtIndex(ScenePath, 0);

            Debug.Log($"[MainMenuSceneBuilder] Scene built and saved to {ScenePath}, registered at build index 0.");
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                Debug.LogError($"[MainMenuSceneBuilder] Could not load asset at {path}");
            return asset;
        }

        private static PanelSettings GetOrCreatePanelSettings()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (existing != null)
                return existing;

            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            AssetDatabase.CreateAsset(settings, PanelSettingsPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[MainMenuSceneBuilder] Created new PanelSettings asset at {PanelSettingsPath}");
            return settings;
        }
    }
}
