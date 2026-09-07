using UnityEngine;
using Vanquish.Combat;

namespace Vanquish.Combat.Play
{
    /// <summary>
    /// Minimal OnGUI HUD for the flyable Phase 1 combat scene — ammo remaining,
    /// target health/destroyed %, and the engagement result, so the existing
    /// stockpile-economy/win-condition logic (EngagementController) is visible while
    /// actually flying and shooting rather than only readable via debug buttons.
    /// </summary>
    public class FlightHUD : MonoBehaviour
    {
        public EngagementController engagementController;
        public WeaponController weapon;
        public BaseObjective objective;

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20, 20, 360, 220), GUI.skin.box);

            GUILayout.Label("Vanquish — Phase 1 Flight Test", Bold());
            GUILayout.Space(6);

            if (engagementController != null)
            {
                GUILayout.Label($"Result: {engagementController.Result}");
                GUILayout.Label($"Time: {engagementController.ElapsedSeconds:0.0}s / {engagementController.timeLimitSeconds:0}s");
            }

            if (weapon != null && engagementController != null)
            {
                int remaining = engagementController.Attacker.RemainingCount(weapon.missilePartId);
                GUILayout.Label($"Missiles remaining: {remaining}");
                GUILayout.Label(weapon.CanFire ? "Ready to fire" : "Reloading / out of ammo");
            }

            if (objective != null)
            {
                GUILayout.Label($"Target health: {objective.Damageable.CurrentHealth:0.0} / {objective.Damageable.MaxHealth:0.0}");
                GUILayout.Label($"Target destroyed: {objective.DestroyedFraction01:P0}");
            }

            GUILayout.Space(8);
            GUILayout.Label("WASD move, Space/Shift up/down, mouse fires, right-drag orbits camera, scroll zooms.", Wrap());

            GUILayout.EndArea();
        }

        private static GUIStyle Bold() => new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        private static GUIStyle Wrap() => new GUIStyle(GUI.skin.label) { wordWrap = true, fontStyle = FontStyle.Italic };
    }
}
