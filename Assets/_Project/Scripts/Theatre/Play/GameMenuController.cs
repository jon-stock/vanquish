using UnityEngine;

namespace Vanquish.Theatre.Play
{
    /// <summary>
    /// Escape-key pause menu: Save, Load, New Game, and Quit. Save/Load/New Game
    /// delegate straight to <see cref="TheatreMapHarness"/> — this component is just
    /// the UI shell around it. Also registers itself against the harness's
    /// IsPointerOverUI check (see TheatreMapCameraController) so opening the menu
    /// and clicking its buttons never reaches through to world-space hex clicks or
    /// camera panning underneath it.
    /// </summary>
    public class GameMenuController : MonoBehaviour
    {
        public TheatreMapHarness harness;

        private bool _isOpen;
        private string _statusMessage;

        private Rect MenuRect => new Rect((Screen.width - 320f) / 2f, (Screen.height - 260f) / 2f, 320f, 260f);

        /// <summary>Included in TheatreMapHarness.IsPointerOverUI's coverage so the menu itself blocks click-through while open.</summary>
        public bool IsPointerOverMenu() => _isOpen && MenuRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                _isOpen = !_isOpen;
        }

        private void OnGUI()
        {
            if (!_isOpen)
                return;

            GUILayout.BeginArea(MenuRect, GUI.skin.box);
            GUILayout.Label("Menu", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });
            GUILayout.Space(10);

            if (GUILayout.Button("Save Game", GUILayout.Height(32)))
            {
                harness.SaveGame();
                _statusMessage = "Game saved.";
            }

            if (GUILayout.Button("Load Game", GUILayout.Height(32)))
            {
                _statusMessage = harness.LoadGame() ? "Game loaded." : "No save file found.";
                if (_statusMessage == "Game loaded.")
                    _isOpen = false;
            }

            GUILayout.Space(6);
            GUILayout.Label("New Game erases any unsaved progress.", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Italic, wordWrap = true });
            if (GUILayout.Button("New Game", GUILayout.Height(32)))
            {
                harness.NewGame();
                _statusMessage = "Started a new game.";
                _isOpen = false;
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Quit", GUILayout.Height(32)))
                Quit();

            GUILayout.Space(6);
            if (GUILayout.Button("Resume"))
                _isOpen = false;

            if (!string.IsNullOrEmpty(_statusMessage))
                GUILayout.Label(_statusMessage);

            GUILayout.EndArea();
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
