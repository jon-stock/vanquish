using System;
using UnityEditor;
using UnityEngine;
using Vanquish.Combat;
using Vanquish.Data;
using Vanquish.Data.Support;
using Vanquish.Simulation.Damage;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Headless smoke test for the Phase 0 combat-instance foundations, run via:
    ///   Unity.exe -batchmode -nographics -projectPath &lt;repo&gt; -executeMethod
    ///   Vanquish.EditorTools.SmokeTest.Run -quit
    /// Exercises DamageResolver/Damageable, BaseObjective, Stockpile, and
    /// StrikePackageRange directly (no Play mode required — EngagementController.Tick
    /// is called explicitly rather than relying on Update()). Exits with a non-zero
    /// code on any failed assertion so this is CI-friendly.
    /// </summary>
    public static class SmokeTest
    {
        private static int _failures;

        public static void Run()
        {
            _failures = 0;

            TestDamageResolverSoftCap();
            TestDamageableDestruction();
            TestBaseObjectiveWinCondition();
            TestStockpileDepletion();
            TestStrikePackageRange();
            TestEngagementControllerResolvesAttackerWin();
            TestEngagementControllerResolvesDefenderWinOnDepletion();

            if (_failures > 0)
            {
                Debug.LogError($"[SmokeTest] FAILED — {_failures} assertion(s) failed.");
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log("[SmokeTest] PASSED — all assertions succeeded.");
                EditorApplication.Exit(0);
            }
        }

        private static void TestDamageResolverSoftCap()
        {
            // Sub-threshold payload (5) against hardness 10 should chip damage in but
            // never push health below the 15%-of-max soft-cap floor.
            float health = 100f;
            const float max = 100f;
            const float hardness = 10f;
            const float softCap = 0.15f;

            for (int i = 0; i < 50; i++)
            {
                health = DamageResolver.ApplyHit(health, max, hardness, softCap, rawDamage: 20f, payloadSize: 5f);
            }

            Expect(Math.Abs(health - max * softCap) < 0.001f,
                $"Sub-threshold damage should floor at {max * softCap}, got {health}");
            Expect(!DamageResolver.IsDestroyed(health), "Should not be destroyed by sub-threshold hits alone");

            // A payload that meets/exceeds hardness should finish it off, ignoring the floor.
            health = DamageResolver.ApplyHit(health, max, hardness, softCap, rawDamage: 50f, payloadSize: 10f);
            Expect(DamageResolver.IsDestroyed(health), "Adequate payload should be able to finish off a soft-capped target");
        }

        private static void TestDamageableDestruction()
        {
            var go = new GameObject("SmokeTest_Damageable");
            try
            {
                var damageable = go.AddComponent<Damageable>();
                damageable.Configure(newMaxHealth: 100f, newHardness: 0f);

                bool destroyedEventFired = false;
                damageable.OnDestroyed += () => destroyedEventFired = true;

                damageable.TakeDamage(rawDamage: 40f, payloadSize: 0f);
                Expect(!damageable.IsDestroyed, "Should survive a 40dmg hit on 100 health");
                Expect(Math.Abs(damageable.CurrentHealth - 60f) < 0.001f, $"Expected 60 health, got {damageable.CurrentHealth}");

                damageable.TakeDamage(rawDamage: 100f, payloadSize: 0f);
                Expect(damageable.IsDestroyed, "Should be destroyed after lethal hit (hardness 0 = always full damage)");
                Expect(destroyedEventFired, "OnDestroyed event should have fired exactly once");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void TestBaseObjectiveWinCondition()
        {
            var go = new GameObject("SmokeTest_BaseObjective");
            try
            {
                var objective = go.AddComponent<BaseObjective>();
                objective.attackerWinDestroyedFraction = 0.75f;
                objective.Damageable.Configure(newMaxHealth: 100f, newHardness: 0f);

                Expect(!objective.HasMetAttackerWinCondition, "Fresh objective should not be won yet");

                objective.Damageable.TakeDamage(rawDamage: 80f, payloadSize: 0f);
                Expect(objective.DestroyedFraction01 >= 0.75f, $"Expected >=75% destroyed, got {objective.DestroyedFraction01:P0}");
                Expect(objective.HasMetAttackerWinCondition, "Attacker win condition should trigger at >=75% destroyed");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void TestStockpileDepletion()
        {
            PointDefenseDefinition samSite = MakePart<PointDefenseDefinition>("smoketest.sam");
            var entry = new StockpileEntry { part = samSite, startingCount = 2 };
            var stockpile = new Stockpile(new[] { entry });

            Expect(stockpile.RemainingCount("smoketest.sam") == 2, "Should start with 2 in stock");
            Expect(stockpile.TryCommit("smoketest.sam"), "First commit should succeed");
            Expect(stockpile.TryCommit("smoketest.sam"), "Second commit should succeed");
            Expect(!stockpile.TryCommit("smoketest.sam"), "Third commit should fail — stockpile depleted");
            Expect(stockpile.IsFullyDepleted(), "Stockpile should report fully depleted");
            Expect(!stockpile.CanCommit("unknown.part"), "Unknown part id should never be committable");

            UnityEngine.Object.DestroyImmediate(samSite);
        }

        private static void TestStrikePackageRange()
        {
            float usable = StrikePackageRange.ComputeUsableRadiusMeters(new[] { 50000f, 12000f, 30000f });
            Expect(Math.Abs(usable - 12000f) < 0.001f, $"Package range should be bottlenecked to the shortest unit (12000), got {usable}");

            float empty = StrikePackageRange.ComputeUsableRadiusMeters(Array.Empty<float>());
            Expect(empty == 0f, "Empty package should have zero usable range");
        }

        private static void TestEngagementControllerResolvesAttackerWin()
        {
            var objectiveGo = new GameObject("SmokeTest_Objective_AttackerWin");
            var controllerGo = new GameObject("SmokeTest_Controller_AttackerWin");
            PointDefenseDefinition drone = MakePart<PointDefenseDefinition>("smoketest.drone.a");
            try
            {
                var objective = objectiveGo.AddComponent<BaseObjective>();
                objective.attackerWinDestroyedFraction = 0.75f;
                objective.Damageable.Configure(100f, 0f);

                var controller = controllerGo.AddComponent<EngagementController>();
                controller.objective = objective;
                controller.attackerLoadout = new[] { new StockpileEntry { part = drone, startingCount = 5 } };
                controller.defenderLoadout = new StockpileEntry[0];
                controller.timeLimitSeconds = 180f;
                controller.Initialize();

                Expect(controller.Result == EngagementResult.InProgress, "Should start InProgress");

                objective.Damageable.TakeDamage(90f, 0f);
                controller.Tick(1f);

                Expect(controller.Result == EngagementResult.AttackerWin, $"Expected AttackerWin, got {controller.Result}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(objectiveGo);
                UnityEngine.Object.DestroyImmediate(controllerGo);
                UnityEngine.Object.DestroyImmediate(drone);
            }
        }

        private static void TestEngagementControllerResolvesDefenderWinOnDepletion()
        {
            var objectiveGo = new GameObject("SmokeTest_Objective_DefenderWin");
            var controllerGo = new GameObject("SmokeTest_Controller_DefenderWin");
            PointDefenseDefinition drone = MakePart<PointDefenseDefinition>("smoketest.drone.b");
            try
            {
                var objective = objectiveGo.AddComponent<BaseObjective>();
                objective.attackerWinDestroyedFraction = 0.75f;
                objective.Damageable.Configure(100f, 0f);

                var controller = controllerGo.AddComponent<EngagementController>();
                controller.objective = objective;
                controller.attackerLoadout = new[] { new StockpileEntry { part = drone, startingCount = 1 } };
                controller.defenderLoadout = new StockpileEntry[0];
                controller.timeLimitSeconds = 180f;
                controller.Initialize();

                Expect(controller.TryCommitAttackerUnit("smoketest.drone.b"), "First commit should succeed");
                Expect(!controller.TryCommitAttackerUnit("smoketest.drone.b"), "Second commit should fail — only 1 in stock");

                // Objective barely scratched — attacker is out of stock before winning.
                objective.Damageable.TakeDamage(10f, 0f);
                controller.Tick(1f);

                Expect(controller.Result == EngagementResult.DefenderWin, $"Expected DefenderWin (attacker stockpile exhausted), got {controller.Result}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(objectiveGo);
                UnityEngine.Object.DestroyImmediate(controllerGo);
                UnityEngine.Object.DestroyImmediate(drone);
            }
        }

        private static T MakePart<T>(string id) where T : PartDefinition
        {
            T part = ScriptableObject.CreateInstance<T>();
            part.id = id;
            part.displayName = id;
            return part;
        }

        private static void Expect(bool condition, string message)
        {
            if (condition)
                return;

            _failures++;
            Debug.LogError($"[SmokeTest] ASSERTION FAILED: {message}");
        }
    }
}
