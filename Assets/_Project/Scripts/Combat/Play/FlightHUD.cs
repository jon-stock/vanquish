using UnityEngine;
using Vanquish.Combat;

namespace Vanquish.Combat.Play
{
    /// <summary>
    /// Minimal OnGUI HUD for the flyable Phase 1 combat scene — engagement result,
    /// elapsed time, and target health/destroyed %, so the existing stockpile-
    /// economy/win-condition logic (EngagementController) is visible while actually
    /// flying and shooting rather than only readable via debug buttons. Per-unit
    /// ammo is shown in the bottom <see cref="UnitRosterHud"/> instead of here, since
    /// there can be more than one controllable unit with independent ammo.
    /// </summary>
    public class FlightHUD : MonoBehaviour
    {
        public EngagementController engagementController;
        public BaseObjective objective;

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20, 20, 380, 160), GUI.skin.box);

            GUILayout.Label("Vanquish — Phase 1 Flight Test", Bold());
            GUILayout.Space(6);

            if (engagementController != null)
            {
                GUILayout.Label($"Result: {engagementController.Result}");
                GUILayout.Label($"Time: {engagementController.ElapsedSeconds:0.0}s / {engagementController.timeLimitSeconds:0}s");
            }

            if (objective != null)
            {
                GUILayout.Label($"Target health: {objective.Damageable.CurrentHealth:0.0} / {objective.Damageable.MaxHealth:0.0}");
                GUILayout.Label($"Target destroyed: {objective.DestroyedFraction01:P0}");
            }

            GUILayout.Space(8);
            GUILayout.Label(
                "WASD move, Space/Shift up/down, mouse fires. 1/2 switch unit, V toggles " +
                "objective camera view, right-drag orbits, scroll zooms.", Wrap());

            GUILayout.EndArea();
        }

        private static GUIStyle Bold() => new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        private static GUIStyle Wrap() => new GUIStyle(GUI.skin.label) { wordWrap = true, fontStyle = FontStyle.Italic };
    }
}
