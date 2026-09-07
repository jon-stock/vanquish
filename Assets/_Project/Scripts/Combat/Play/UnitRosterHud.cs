using System;
using System.Collections.Generic;
using UnityEngine;
using Vanquish.Combat;

namespace Vanquish.Combat.Play
{
    /// <summary>
    /// The "available units" bar across the bottom of the screen: one entry per
    /// controllable unit, showing its label, remaining missiles (read from the
    /// shared EngagementController's Stockpile — each unit has its own part id, so
    /// these counts are genuinely independent, not a shared pool), and highlighting
    /// whichever one is currently active/piloted (see PlayerUnitSwitcher).
    /// </summary>
    public class UnitRosterHud : MonoBehaviour
    {
        [Serializable]
        public class RosterEntry
        {
            public string label;
            public WeaponController weapon;
        }

        public List<RosterEntry> entries = new List<RosterEntry>();
        public PlayerUnitSwitcher switcher;

        private void OnGUI()
        {
            const int barHeight = 70;
            var barRect = new Rect(0, Screen.height - barHeight, Screen.width, barHeight);
            GUI.Box(barRect, GUIContent.none);

            GUILayout.BeginArea(barRect);
            GUILayout.Space(4);
            GUILayout.Label("Available Units — number keys to switch, V toggles objective camera view", Bold());

            GUILayout.BeginHorizontal();
            for (int i = 0; i < entries.Count; i++)
            {
                RosterEntry entry = entries[i];
                bool isActive = switcher != null && switcher.ActiveIndex == i;

                int remaining = 0;
                if (entry.weapon != null && entry.weapon.engagementController != null && entry.weapon.engagementController.Attacker != null)
                    remaining = entry.weapon.engagementController.Attacker.RemainingCount(entry.weapon.missilePartId);

                Color previous = GUI.color;
                GUI.color = isActive ? Color.green : Color.white;
                GUILayout.Box($"[{i + 1}] {entry.label}\nMissiles: {remaining}", GUILayout.Width(160), GUILayout.Height(40));
                GUI.color = previous;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private static GUIStyle Bold() => new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
    }
}
