using UnityEngine;
using Vanquish.Data.Drones;
using Vanquish.Data.Support;
using Vanquish.Simulation.Damage;

namespace Vanquish.Combat.DebugTools
{
    // Namespace deliberately not "Vanquish.Combat.Debug" — that would shadow
    // UnityEngine.Debug for any code written inside it.
    /// <summary>
    /// Press-Play-and-click test harness for the Phase 0/1 combat-instance logic
    /// (PLAN.md). No art/prefabs/UI Toolkit needed — builds its own objective,
    /// stockpiles, and point-defense battery entirely in code at Start(), and draws
    /// an interactive OnGUI panel so you can actually click through an engagement
    /// and watch the stockpile-drain tactic (cheap decoys costing the defender real
    /// interceptors) play out for yourself.
    ///
    /// To use: open Assets/_Project/Scenes/Phase1_DebugHarness.unity and press Play.
    /// </summary>
    public class EngagementDebugHarness : MonoBehaviour
    {
        private const string DecoyId = "debug.decoy";
        private const string StrikeId = "debug.strike";

        private EngagementController _controller;
        private BaseObjective _objective;
        private PointDefenseBattery _battery;
        private string _lastStrikeLog = "(no strikes committed yet)";

        private void Start()
        {
            BuildEngagement();
        }

        private void BuildEngagement()
        {
            // --- Objective: a "Base" with hardness between the decoy's and the real
            // strike unit's payload size, so decoys alone can soften it but can't
            // finish it off (PLAN.md Damage & Payload Model soft-cap rule). ---
            var objectiveGo = new GameObject("Objective (Base)");
            objectiveGo.transform.SetParent(transform);
            _objective = objectiveGo.AddComponent<BaseObjective>();
            _objective.attackerWinDestroyedFraction = 0.75f;
            _objective.Damageable.Configure(newMaxHealth: 100f, newHardness: 30f, newSoftCapResidualFraction: 0.2f);

            // --- Defender: one point-defense battery with limited ammo and a 50%
            // per-shot intercept chance. Every attacker commit (decoy or real) costs
            // it one interceptor whether it hits or not. ---
            var batteryGo = new GameObject("Defender Point-Defense Battery");
            batteryGo.transform.SetParent(transform);
            var batteryDefinition = ScriptableObject.CreateInstance<PointDefenseDefinition>();
            batteryDefinition.id = "debug.pd.battery";
            batteryDefinition.displayName = "Debug SAM Battery";
            batteryDefinition.implementation = PointDefenseImplementation.Missile;
            batteryDefinition.ammoCount = 3;
            batteryDefinition.interceptProbability = 0.5f;
            _battery = batteryGo.AddComponent<PointDefenseBattery>();
            _battery.Configure(batteryDefinition);

            // --- Attacker loadout: cheap decoys (low damage, low payload — soft-capped
            // against this objective's hardness) and a small number of expensive real
            // strikes (high damage, payload exceeds hardness — uncapped, can finish it). ---
            var decoyPart = ScriptableObject.CreateInstance<DroneAirframeDefinition>();
            decoyPart.id = DecoyId;
            decoyPart.displayName = "Cheap Decoy Quadcopter";
            decoyPart.buildCost = 5;

            var strikePart = ScriptableObject.CreateInstance<DroneAirframeDefinition>();
            strikePart.id = StrikeId;
            strikePart.displayName = "Real Strike Drone";
            strikePart.buildCost = 80;

            var controllerGo = new GameObject("EngagementController");
            controllerGo.transform.SetParent(transform);
            _controller = controllerGo.AddComponent<EngagementController>();
            _controller.Objective = _objective;
            _controller.defenderBatteries = new[] { _battery };
            _controller.timeLimitSeconds = 90f;
            _controller.attackerLoadout = new[]
            {
                new StockpileEntry { part = decoyPart, startingCount = 6, rawDamage = 8f, payloadSize = 5f },
                new StockpileEntry { part = strikePart, startingCount = 2, rawDamage = 60f, payloadSize = 50f },
            };
            _controller.defenderLoadout = new StockpileEntry[0];
            _controller.Initialize();

            _lastStrikeLog = "(no strikes committed yet)";
        }

        private void CommitStrike(string partId, string label)
        {
            AttackerStrikeResult result = _controller.CommitAttackerStrike(partId);
            _lastStrikeLog = $"{label} -> {result}";
        }

        private void OnGUI()
        {
            const int width = 420;
            GUILayout.BeginArea(new Rect(20, 20, width, 500), GUI.skin.box);

            GUILayout.Label("Vanquish — Phase 1 Engagement Debug Harness", EditorBoldLabel());
            GUILayout.Space(8);

            GUILayout.Label($"Result: {_controller.Result}");
            GUILayout.Label($"Elapsed: {_controller.ElapsedSeconds:0.0}s / {_controller.timeLimitSeconds:0}s");

            GUILayout.Space(8);
            GUILayout.Label("Objective (Base) — hardness 30, soft-cap floor 20%");
            GUILayout.Label($"  Health: {_objective.Damageable.CurrentHealth:0.0} / {_objective.Damageable.MaxHealth:0.0}");
            GUILayout.Label($"  Destroyed: {_objective.DestroyedFraction01:P0} (win at 75%)");

            GUILayout.Space(8);
            GUILayout.Label("Defender point-defense battery — 50% intercept chance");
            GUILayout.Label($"  Interceptors remaining: {_battery.RemainingAmmo}");

            GUILayout.Space(8);
            GUILayout.Label("Attacker stockpile");
            GUILayout.Label($"  Decoys remaining: {_controller.Attacker.RemainingCount(DecoyId)} / 6  (cheap, payload 5 — soft-capped by hardness 30)");
            GUILayout.Label($"  Real strikes remaining: {_controller.Attacker.RemainingCount(StrikeId)} / 2  (expensive, payload 50 — exceeds hardness, uncapped)");

            GUILayout.Space(8);
            GUILayout.Label($"Last strike: {_lastStrikeLog}");

            GUILayout.Space(12);

            GUI.enabled = _controller.Result == EngagementResult.InProgress;
            if (GUILayout.Button("Commit Decoy Strike", GUILayout.Height(30)))
                CommitStrike(DecoyId, "Decoy");

            if (GUILayout.Button("Commit Real Strike", GUILayout.Height(30)))
                CommitStrike(StrikeId, "Real strike");
            GUI.enabled = true;

            GUILayout.Space(8);
            if (GUILayout.Button("Reset Engagement", GUILayout.Height(24)))
            {
                foreach (Transform child in transform)
                    Destroy(child.gameObject);

                BuildEngagement();
            }

            GUILayout.Space(8);
            GUILayout.Label(
                "Try: spend all 6 decoys first and watch the battery burn through its " +
                "3 interceptors on cheap targets, THEN commit real strikes — versus " +
                "committing real strikes first into a full-ammo battery.",
                EditorWrapLabel());

            GUILayout.EndArea();
        }

        private static GUIStyle EditorBoldLabel()
        {
            var style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            return style;
        }

        private static GUIStyle EditorWrapLabel()
        {
            var style = new GUIStyle(GUI.skin.label) { wordWrap = true, fontStyle = FontStyle.Italic };
            return style;
        }
    }
}
