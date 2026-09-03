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

        [Tooltip("Dev-visibility pass (Phase 2D): known enemy contacts farther than this " +
            "get an on-screen diamond marker + distance readout drawn over their actual " +
            "world position, since the prototype primitive models are too small to " +
            "reliably spot in the 3D view at real combat distances on their own — see " +
            "PLAN.md's Phase 2D technical notes. Contacts closer than this are left " +
            "unmarked since the real model should already be visible by then.")]
        public float distantMarkerMinDistanceMeters = 150f;

        private GUIStyle _labelStyle;
        private GUIStyle _resultStyle;
        private GUIStyle _markerLabelStyle;
        private GUIStyle _objectiveLabelStyle;
        private GUIStyle _returnButtonStyle;
        private Rigidbody _playerRigidbody;
        private FlightBody _playerFlightBody;

        private void Start()
        {
            if (player != null)
            {
                _playerRigidbody = player.GetComponent<Rigidbody>();
                _playerFlightBody = player.GetComponent<FlightBody>();
            }
        }

        private void OnGUI()
        {
            _labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 18, normal = { textColor = Color.white } };
            _resultStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 36, normal = { textColor = Color.yellow }, alignment = TextAnchor.MiddleCenter };
            _markerLabelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = Color.red }, alignment = TextAnchor.UpperCenter };
            _objectiveLabelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 20, normal = { textColor = Color.white }, alignment = TextAnchor.MiddleCenter };
            _returnButtonStyle ??= new GUIStyle(GUI.skin.button) { fontSize = 16 };

            DrawStatusPanel();
            DrawFlightPanel();
            DrawArtificialHorizon();
            DrawRadar();
            DrawDistantContactMarkers();
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

            // Depth pass (direct user feedback: "there's no throttle indicator in the
            // sandbox, so you can't tell how much power you're putting down"): reads
            // FlightBody.throttleFraction directly — meaningful for a fixed-wing
            // design (PlayerDroneController's throttle lever, 0-1); shows N/A for a
            // multirotor, which has no single throttle lever concept (it's vectored
            // thrust in whatever direction WASD/space asks for, not a power setting).
            bool hasThrottle = _playerFlightBody != null && _playerFlightBody.useAerodynamicLift;
            GUI.Box(new Rect(10, 90, 260, hasThrottle ? 135 : 110), GUIContent.none);

            float speed = _playerRigidbody != null ? _playerRigidbody.linearVelocity.magnitude : 0f;
            float altitudeMsl = player.position.y;
            float groundHeight = GroundSampler.SampleGroundHeight(player.position);
            float altitudeAgl = altitudeMsl - groundHeight;

            GUI.Label(new Rect(20, 95, 240, 25), $"Speed: {speed:F0} m/s", _labelStyle);
            GUI.Label(new Rect(20, 120, 240, 25), $"Altitude: {altitudeAgl:F0} m AGL ({altitudeMsl:F0} m MSL)", _labelStyle);

            float nextLineY = 145f;
            if (hasThrottle)
            {
                GUI.Label(new Rect(20, nextLineY, 240, 25), $"Throttle: {_playerFlightBody.throttleFraction * 100f:F0}%", _labelStyle);
                nextLineY += 25f;
            }

            DetectableSignature target = TeamAwareness.Instance != null
                ? TeamAwareness.Instance.GetNearestKnownEnemy(Team.Player, player.position)
                : null;

            if (target != null)
            {
                float distance = Vector3.Distance(player.position, target.Position);
                bool canFire = playerWeapon != null && playerWeapon.CanFire;
                string lockText = canFire ? "LOCKED" : "TRACKING";
                GUI.Label(new Rect(20, nextLineY, 240, 25), $"Target: {target.name} — {distance:F0} m", _labelStyle);
                GUI.Label(new Rect(20, nextLineY + 25f, 240, 25), lockText, _labelStyle);
            }
            else
            {
                GUI.Label(new Rect(20, nextLineY, 240, 25), "Target: none", _labelStyle);
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

            // Fix (direct user feedback: "we go into the brown when going up"): this used
            // to be negated, which made climbing (forward pointing up) move the horizon
            // UP the gauge (less sky, more ground) — the exact opposite of a real
            // attitude indicator, where pitching up reveals more sky below a fixed nose
            // reference. Positive pitchDeg (no negation) now correctly means "climbing"
            // and pushes the horizon down (more sky) via horizonY below.
            float pitchDeg = Mathf.Asin(Mathf.Clamp(Vector3.Dot(player.forward, Vector3.up), -1f, 1f)) * Mathf.Rad2Deg;
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

        /// <summary>
        /// Dev-visibility pass (Phase 2D): unlike DrawRadar (a fixed heading-relative
        /// mini-map in the corner, range-limited to radarRangeMeters), this overlays a
        /// marker directly on top of each known enemy's actual on-screen position in
        /// the 3D view — the thing actually missing before, since the corner radar
        /// tells you *that* something exists but not *where to look* in front of you.
        /// Only off-screen-vs-on-screen is handled (contacts behind/outside the camera
        /// frustum are skipped); an edge-of-screen directional indicator for contacts
        /// currently out of view is a natural follow-up but out of scope for this pass.
        /// </summary>
        private void DrawDistantContactMarkers()
        {
            if (player == null || TeamAwareness.Instance == null || Camera.main == null)
                return;

            Camera cam = Camera.main;
            foreach (var contact in TeamAwareness.Instance.GetKnownEnemies(Team.Player))
            {
                if (contact == null)
                    continue;

                float distance = Vector3.Distance(player.position, contact.Position);
                if (distance < distantMarkerMinDistanceMeters)
                    continue; // close enough that the real 3D model should already read fine

                Vector3 viewportPoint = cam.WorldToViewportPoint(contact.Position);
                if (viewportPoint.z <= 0f)
                    continue; // behind the camera
                if (viewportPoint.x < 0f || viewportPoint.x > 1f || viewportPoint.y < 0f || viewportPoint.y > 1f)
                    continue; // off-screen — see method doc comment

                Vector2 screenPos = new Vector2(viewportPoint.x * Screen.width, (1f - viewportPoint.y) * Screen.height);
                DrawDiamondMarker(screenPos, Color.red, 10f);
                GUI.Label(new Rect(screenPos.x - 40f, screenPos.y + 8f, 80f, 20f), $"{distance:F0}m", _markerLabelStyle);
            }
        }

        private void DrawDiamondMarker(Vector2 center, Color color, float size)
        {
            Matrix4x4 previousMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(45f, center);
            DrawDot(center, color, size);
            GUI.matrix = previousMatrix;
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

            // Phase 2E: show *which* objective was won/lost — increasingly meaningful
            // now that victory isn't always "destroy all enemy units" (see
            // CombatManager.ObjectiveDescription / IObjective).
            string objective = CombatManager.Instance.ObjectiveDescription;
            if (!string.IsNullOrEmpty(objective))
                GUI.Label(new Rect(0, Screen.height / 2f + 20f, Screen.width, 30f), objective, _objectiveLabelStyle);

            // Phase 3A follow-up: CombatManager already auto-returns to the Workshop
            // after resultToReturnDelaySeconds, but forcing the player to sit through
            // that delay every time isn't the "consistent, in-UI return path" 3A's own
            // goal calls for — this lets them leave immediately instead. Same
            // OnGUI-immediate-mode style as the rest of this HUD.
            var buttonRect = new Rect(Screen.width / 2f - 110f, Screen.height / 2f + 60f, 220f, 40f);
            if (GUI.Button(buttonRect, "Return to Workshop", _returnButtonStyle))
                CombatManager.Instance.ReturnToWorkshopNow();
        }
    }
}
