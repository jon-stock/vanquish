using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vanquish.Core;
using Vanquish.Simulation.Sensors;

namespace Vanquish.Combat
{
    /// <summary>
    /// Phase 2G: purely observational reporting for the Test Range — no win/lose, no
    /// currency, just distance/hit-point/time-to-kill feedback so the player can judge
    /// a design's real behavior (this sub-milestone's own goal) using the exact same
    /// simulation Combat uses (VehicleFactory/WeaponController/GuidanceController), not
    /// a separate mock. Mirrors Phase0TestHarness's "log the numbers, plain overlay,
    /// nothing fancier" style rather than a polished report screen — that's Phase 3's
    /// UI/UX pass. Deliberately has no dependency on CombatManager at all (which would
    /// pull in currency rewards and a VICTORY/DEFEAT banner neither wanted here); it
    /// discovers dummy targets itself via the same DetectableSignature scene-scan
    /// technique CombatManager.Start() already uses for real combat scenes.
    /// </summary>
    public class TestRangeTelemetry : MonoBehaviour
    {
        public Transform player;
        public string workshopSceneName = "Workshop";

        private class TargetRecord
        {
            public Health health;
            public float spawnTime;
            public bool destroyed;
            public float destroyedTime;
        }

        private readonly List<TargetRecord> _targets = new List<TargetRecord>();
        private GUIStyle _labelStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _buttonStyle;

        private void Start()
        {
            foreach (var signature in FindObjectsByType<DetectableSignature>(FindObjectsSortMode.None))
            {
                if (signature.team != Team.Enemy)
                    continue;

                var health = signature.GetComponent<Health>();
                if (health == null)
                    continue;

                var record = new TargetRecord { health = health, spawnTime = Time.time };
                health.OnDestroyed += _ => OnTargetDestroyed(record);
                _targets.Add(record);
            }
        }

        private void OnTargetDestroyed(TargetRecord record)
        {
            record.destroyed = true;
            record.destroyedTime = Time.time;
            float timeToKill = record.destroyedTime - record.spawnTime;
            Debug.Log($"[TestRange] {record.health.name} destroyed — time-to-kill {timeToKill:F1}s");
        }

        private void OnGUI()
        {
            _labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = Color.white } };
            _headerStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            _buttonStyle ??= new GUIStyle(GUI.skin.button) { fontSize = 13 };

            const float panelWidth = 280f;
            float panelHeight = 35f + _targets.Count * 22f + 36f;
            var rect = new Rect(Screen.width - panelWidth - 10f, 10f, panelWidth, panelHeight);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 5f, panelWidth - 20f, 20f), "Test Range", _headerStyle);

            for (int i = 0; i < _targets.Count; i++)
            {
                TargetRecord record = _targets[i];
                string status;
                if (record.destroyed)
                {
                    status = $"{record.health.name}: DESTROYED — TTK {record.destroyedTime - record.spawnTime:F1}s";
                }
                else if (record.health != null && player != null)
                {
                    float distance = Vector3.Distance(player.position, record.health.transform.position);
                    status = $"{record.health.name}: {distance:F0}m, {record.health.CurrentHealth:F0}/{record.health.maxHealth:F0} HP";
                }
                else
                {
                    status = $"{(record.health != null ? record.health.name : "target")}: alive";
                }
                GUI.Label(new Rect(rect.x + 10f, rect.y + 28f + i * 22f, panelWidth - 20f, 20f), status, _labelStyle);
            }

            var buttonRect = new Rect(rect.x + 10f, rect.y + panelHeight - 32f, panelWidth - 20f, 26f);
            if (GUI.Button(buttonRect, "Return to Workshop", _buttonStyle))
                SceneManager.LoadScene(workshopSceneName);
        }
    }
}
