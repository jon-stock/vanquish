using UnityEngine;
using Vanquish.Core;

namespace Vanquish.Combat
{
    /// <summary>
    /// Phase 1 MVP HUD: health, ammo, and a simple top-down radar showing known
    /// enemy contacts (fed by TeamAwareness, i.e. including anything the scout has
    /// found). Deliberately implemented with OnGUI immediate-mode rendering rather
    /// than a Canvas/UI Toolkit layout — no art or prefabs required, matching the
    /// MVP's "ugly art is fine, the loop must be complete" scope. Replace with a
    /// proper UI in the Phase 3 polish pass.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        public Transform player;
        public Health playerHealth;
        public WeaponController playerWeapon;

        public float radarRangeMeters = 1000f;
        public float radarBoxSize = 220f;

        private GUIStyle _labelStyle;
        private GUIStyle _resultStyle;

        private void OnGUI()
        {
            _labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 18, normal = { textColor = Color.white } };
            _resultStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 36, normal = { textColor = Color.yellow }, alignment = TextAnchor.MiddleCenter };

            DrawStatusPanel();
            DrawRadar();
            DrawCombatResult();
        }

        private void DrawStatusPanel()
        {
            GUI.Box(new Rect(10, 10, 220, 70), GUIContent.none);
            float healthPct = playerHealth != null && playerHealth.maxHealth > 0
                ? playerHealth.CurrentHealth / playerHealth.maxHealth * 100f
                : 0f;
            GUI.Label(new Rect(20, 15, 200, 25), $"Health: {healthPct:F0}%", _labelStyle);
            int ammo = playerWeapon != null ? playerWeapon.ammoRemaining : 0;
            GUI.Label(new Rect(20, 40, 200, 25), $"Ammo: {ammo}", _labelStyle);
        }

        private void DrawRadar()
        {
            if (player == null)
                return;

            float x = Screen.width - radarBoxSize - 20f;
            float y = Screen.height - radarBoxSize - 20f;
            GUI.Box(new Rect(x, y, radarBoxSize, radarBoxSize), GUIContent.none);

            Vector2 center = new Vector2(x + radarBoxSize / 2f, y + radarBoxSize / 2f);
            DrawDot(center, Color.green, 6f); // player at center

            if (TeamAwareness.Instance == null)
                return;

            foreach (var contact in TeamAwareness.Instance.GetKnownEnemies(Team.Player))
            {
                if (contact == null)
                    continue;

                Vector3 offset = contact.Position - player.position;
                Vector2 flat = new Vector2(offset.x, offset.z);
                float scale = (radarBoxSize / 2f) / radarRangeMeters;
                Vector2 dotPos = center + new Vector2(flat.x, -flat.y) * scale;

                // Clamp to radar box edge if beyond range, so distant scout-spotted
                // contacts still show a directional blip.
                Vector2 toDot = dotPos - center;
                float maxRadius = radarBoxSize / 2f - 6f;
                if (toDot.magnitude > maxRadius)
                    dotPos = center + toDot.normalized * maxRadius;

                DrawDot(dotPos, Color.red, 5f);
            }
        }

        private void DrawDot(Vector2 position, Color color, float size)
        {
            var prevColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(position.x - size / 2f, position.y - size / 2f, size, size), Texture2D.whiteTexture);
            GUI.color = prevColor;
        }

        private void DrawCombatResult()
        {
            if (CombatManager.Instance == null || CombatManager.Instance.Result == CombatResult.InProgress)
                return;

            string text = CombatManager.Instance.Result == CombatResult.Victory ? "VICTORY" : "DEFEAT";
            GUI.Label(new Rect(0, Screen.height / 2f - 40f, Screen.width, 80f), text, _resultStyle);
        }
    }
}
