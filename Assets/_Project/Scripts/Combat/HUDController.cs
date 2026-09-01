using UnityEngine;
using Vanquish.Core;
using Vanquish.Simulation.Flight;
using Vanquish.Simulation.Sensors;

namespace Vanquish.Combat
{
    /// <summary>
    /// Phase 1 MVP HUD: health, ammo, speed/altitude, a simple artificial horizon,
    /// current-target lock status, and a heading-relative radar showing known enemy
    /// contacts (fed by TeamAwareness, i.e. including anything the scout has found).
    /// Deliberately implemented with OnGUI immediate-mode rendering rather than a
    /// Canvas/UI Toolkit layout — no art or prefabs required, matching the MVP's
    /// "ugly art is fine, the loop must be complete" scope. These are pragmatic,
    /// incremental additions for playtesting (added once flying without a
    /// speed/altitude/horizon reference and a fixed-world-axis radar proved
    /// genuinely hard to fly/read by) — the *full* UI/UX pass (tech tree
    /// visualization, workshop comparison tools, real HUD art) is still Phase 3's
    /// job, tracked separately in PLAN.md; this file isn't meant to preempt that.
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
        private Rigidbody _playerRigidbody;

        private void Start()
        {
            if (player != null)
                _playerRigidbody = player.GetComponent<Rigidbody>();
        }

        private void OnGUI()
        {
            _labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 18, normal = { textColor = Color.white } };
            _resultStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 36, normal = { textColor = Color.yellow }, alignment = TextAnchor.MiddleCenter };

            DrawStatusPanel();
            DrawFlightPanel();
            DrawArtificialHorizon();
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

        /// <summary>
        /// Speed, altitude (both absolute MSL and ground-relative AGL via
        /// GroundSampler — see Phase 2B's altitude-mode work), and current target
        /// lock status, added specifically to answer "I'm terrible at flying without
        /// a speedo, an artificial horizon, and such" — the previous HUD had no
        /// numeric flight reference at all.
        /// </summary>
        private void DrawFlightPanel()
        {
            if (player == null)
                return;

            GUI.Box(new Rect(10, 90, 260, 110), GUIContent.none);

            float speed = _playerRigidbody != null ? _playerRigidbody.linearVelocity.magnitude : 0f;
            float altitudeMsl = player.position.y;
            float groundHeight = GroundSampler.SampleGroundHeight(player.position);
            float altitudeAgl = altitudeMsl - groundHeight;

            GUI.Label(new Rect(20, 95, 240, 25), $"Speed: {speed:F0} m/s", _labelStyle);
            GUI.Label(new Rect(20, 120, 240, 25), $"Altitude: {altitudeAgl:F0} m AGL ({altitudeMsl:F0} m MSL)", _labelStyle);

            DetectableSignature target = TeamAwareness.Instance != null
                ? TeamAwareness.Instance.GetNearestKnownEnemy(Team.Player, player.position)
                : null;

            if (target != null)
            {
                float distance = Vector3.Distance(player.position, target.Position);
                bool canFire = playerWeapon != null && playerWeapon.CanFire;
                string lockText = canFire ? "LOCKED" : "TRACKING";
                GUI.Label(new Rect(20, 145, 240, 25), $"Target: {target.name} — {distance:F0} m", _labelStyle);
                GUI.Label(new Rect(20, 170, 240, 25), lockText, _labelStyle);
            }
            else
            {
                GUI.Label(new Rect(20, 145, 240, 25), "Target: none", _labelStyle);
            }
        }

        /// <summary>
        /// Minimal analog artificial horizon: a sky/ground split rect that rotates
        /// with roll and shifts vertically with pitch, plus a fixed yellow aircraft
        /// reference marker at the gauge center (the "nose" reference — this stays
        /// level while the horizon moves around it, matching a real attitude
        /// indicator). Pitch/roll are derived from the player's transform directly
        /// (Vector3.Dot/SignedAngle against world up) rather than raw
        /// transform.eulerAngles, to avoid Euler-angle unwinding artifacts at steep
        /// attitudes.
        /// </summary>
        private void DrawArtificialHorizon()
        {
            if (player == null)
                return;

            const float gaugeSize = 140f;
            float x = Screen.width - gaugeSize - 20f;
            float y = 20f;
            var gaugeRect = new Rect(x, y, gaugeSize, gaugeSize);

            float pitchDeg = -Mathf.Asin(Mathf.Clamp(Vector3.Dot(player.forward, Vector3.up), -1f, 1f)) * Mathf.Rad2Deg;
            float rollDeg = Vector3.SignedAngle(Vector3.up, player.up, player.forward);

            GUI.Box(gaugeRect, GUIContent.none);
            GUI.BeginGroup(gaugeRect);

            Vector2 localCenter = new Vector2(gaugeSize / 2f, gaugeSize / 2f);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(rollDeg, localCenter);

            const float pixelsPerDegree = 1.4f;
            float horizonY = localCenter.y + Mathf.Clamp(pitchDeg, -60f, 60f) * pixelsPerDegree;

            // Oversized rects (well beyond the gauge bounds) so rotating the sky/ground
            // split never reveals a gap at the gauge's corners.
            const float overscan = 80f;
            var previousColor = GUI.color;
            GUI.color = new Color(0.25f, 0.55f, 0.85f); // sky
            GUI.DrawTexture(new Rect(-overscan, -overscan, gaugeSize + overscan * 2f, horizonY - -overscan), Texture2D.whiteTexture);
            GUI.color = new Color(0.45f, 0.32f, 0.15f); // ground
            GUI.DrawTexture(new Rect(-overscan, horizonY, gaugeSize + overscan * 2f, gaugeSize + overscan - horizonY), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(-overscan, horizonY - 1f, gaugeSize + overscan * 2f, 2f), Texture2D.whiteTexture); // horizon line
            GUI.color = previousColor;

            // Fixed aircraft reference marker — drawn after restoring the matrix so it
            // stays level/centered regardless of roll, like a real attitude indicator's
            // fixed nose symbol.
            GUI.matrix = previousMatrix;
            GUI.color = Color.yellow;
            GUI.DrawTexture(new Rect(localCenter.x - 18f, localCenter.y - 1f, 14f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(localCenter.x + 4f, localCenter.y - 1f, 14f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(localCenter.x - 1f, localCenter.y - 1f, 2f, 2f), Texture2D.whiteTexture);
            GUI.color = previousColor;

            GUI.EndGroup();
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

            // Heading tick: shows which way the player is facing on the radar (always
            // straight up, since this is a heading-relative display — see below), so
            // there's a fixed visual reference for "forward" even when no contacts
            // are visible.
            DrawDot(center + new Vector2(0f, -(radarBoxSize / 2f - 4f)), Color.white, 3f);

            if (TeamAwareness.Instance == null)
                return;

            float scale = (radarBoxSize / 2f) / radarRangeMeters;
            float maxRadius = radarBoxSize / 2f - 6f;
            float playerYawRad = player.eulerAngles.y * Mathf.Deg2Rad;

            foreach (var contact in TeamAwareness.Instance.GetKnownEnemies(Team.Player))
            {
                if (contact == null)
                    continue;

                Vector3 offset = contact.Position - player.position;

                // Heading-relative (a real cockpit/HUD-style radar): "forward" is
                // always straight up on the display, rotating with the player,
                // rather than a fixed north-up map view. The previous version used
                // raw world-space X/Z offset with no rotation for player heading at
                // all, so a contact dead ahead only appeared "up" on the radar if
                // the player happened to be facing world +Z — everywhere else, the
                // dot position didn't correspond to the actual relative bearing,
                // which is what made the radar look broken while flying and turning.
                float worldBearingRad = Mathf.Atan2(offset.x, offset.z); // 0 = world +Z, positive = clockwise
                float relativeBearingRad = worldBearingRad - playerYawRad;

                float distance = new Vector2(offset.x, offset.z).magnitude;
                float radius = Mathf.Min(distance * scale, maxRadius);
                Vector2 dotPos = center + new Vector2(Mathf.Sin(relativeBearingRad), -Mathf.Cos(relativeBearingRad)) * radius;

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
