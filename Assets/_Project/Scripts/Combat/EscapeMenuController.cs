using UnityEngine;
using UnityEngine.InputSystem;
using Vanquish.Core;

namespace Vanquish.Combat
{
    /// <summary>
    /// Escape/pause menu: ESC toggles a paused (Time.timeScale = 0) overlay with
    /// Resume / Return to Workshop / Quit Game options. Same OnGUI immediate-mode
    /// style as HUDController/TestRangeTelemetry — no art/prefabs required. Added by
    /// Phase1CombatSceneBuilder.BuildHud, so every combat scene (Combat_Arena01, the
    /// Phase 2E terrain arenas) and the Test Range (which reuses BuildHud) all get
    /// this for free — previously the only way to leave mid-session was the Test
    /// Range's own "Return to Workshop" button or waiting out a combat result.
    /// </summary>
    public class EscapeMenuController : MonoBehaviour
    {
        public string workshopSceneName = SceneNames.Workshop;

        private bool _isOpen;
        private GUIStyle _titleStyle;
        private GUIStyle _buttonStyle;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                SetOpen(!_isOpen);
        }

        private void SetOpen(bool open)
        {
            _isOpen = open;
            Time.timeScale = open ? 0f : 1f;
        }

        private void OnDisable()
        {
            // Guard against leaving the game permanently paused if this component is
            // ever disabled/destroyed (e.g. scene teardown) while the menu is open —
            // Time.timeScale is global engine state, not scene-scoped, so it would
            // otherwise silently leak into whatever scene loads next.
            if (_isOpen)
                Time.timeScale = 1f;
        }

        private void OnGUI()
        {
            if (!_isOpen)
                return;

            _titleStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
            };
            _buttonStyle ??= new GUIStyle(GUI.skin.button) { fontSize = 18 };

            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;

            const float panelWidth = 320f;
            const float panelHeight = 240f;
            var panelRect = new Rect((Screen.width - panelWidth) / 2f, (Screen.height - panelHeight) / 2f, panelWidth, panelHeight);
            GUI.Box(panelRect, GUIContent.none);

            GUI.Label(new Rect(panelRect.x, panelRect.y + 15f, panelWidth, 40f), "PAUSED", _titleStyle);

            if (GUI.Button(new Rect(panelRect.x + 30f, panelRect.y + 70f, panelWidth - 60f, 40f), "Resume", _buttonStyle))
                SetOpen(false);

            if (GUI.Button(new Rect(panelRect.x + 30f, panelRect.y + 120f, panelWidth - 60f, 40f), "Return to Workshop", _buttonStyle))
            {
                // Restore timeScale before leaving so the Workshop (or any scene
                // after this one) doesn't inherit a paused game.
                SetOpen(false);
                GameFlowController.ReturnToWorkshop(workshopSceneName);
            }

            if (GUI.Button(new Rect(panelRect.x + 30f, panelRect.y + 170f, panelWidth - 60f, 40f), "Quit Game", _buttonStyle))
            {
                Debug.Log("[EscapeMenuController] Quit requested.");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
    }
}
