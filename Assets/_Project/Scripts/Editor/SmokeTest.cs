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

            // Phase 1
            TestFactoryAndWarehouseObjectivesShareStructureLogic();
            TestSupplyLineObjectiveDefenderWinViaEscort();
            TestSupplyLineObjectiveAttackerWinViaDestruction();
            TestPointDefenseBatteryConsumesAmmoRegardlessOfOutcome();
            TestEngagementControllerCommitAttackerStrikePipeline();
            TestStandingOrderExecutorSequencesDecoysFirst();
            TestSeekerControlModel();

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
                controller.Objective = objective;
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
                controller.Objective = objective;
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

        private static void TestFactoryAndWarehouseObjectivesShareStructureLogic()
        {
            var factoryGo = new GameObject("SmokeTest_Factory");
            var warehouseGo = new GameObject("SmokeTest_Warehouse");
            try
            {
                var factory = factoryGo.AddComponent<FactoryObjective>();
                factory.attackerWinDestroyedFraction = 0.75f;
                factory.Damageable.Configure(200f, 40f); // harder than a base

                var warehouse = warehouseGo.AddComponent<WarehouseObjective>();
                warehouse.attackerWinDestroyedFraction = 0.75f;
                warehouse.Damageable.Configure(80f, 5f); // softer than a base

                Expect(!factory.HasMetAttackerWinCondition, "Fresh factory should not be won yet");
                Expect(!warehouse.HasMetAttackerWinCondition, "Fresh warehouse should not be won yet");
                Expect(!factory.HasMetDefenderWinCondition, "Structure objectives never have their own defender-win condition");

                // Same win-condition math as BaseObjective, proving the shared base class works identically.
                factory.Damageable.TakeDamage(160f, 100f); // payload exceeds hardness -> uncapped
                Expect(factory.HasMetAttackerWinCondition, $"Factory should be won at 80% destroyed, got {factory.DestroyedFraction01:P0}");

                warehouse.Damageable.TakeDamage(65f, 100f);
                Expect(warehouse.HasMetAttackerWinCondition, $"Warehouse should be won at >=75% destroyed, got {warehouse.DestroyedFraction01:P0}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(factoryGo);
                UnityEngine.Object.DestroyImmediate(warehouseGo);
            }
        }

        private static void TestSupplyLineObjectiveDefenderWinViaEscort()
        {
            var go = new GameObject("SmokeTest_SupplyLine_Escort");
            try
            {
                var supplyLine = go.AddComponent<SupplyLineObjective>();
                supplyLine.attackerWinDestroyedFraction = 0.6f;
                supplyLine.escortProgressPerSecond = 0.5f;
                supplyLine.Damageable.Configure(100f, 0f);

                Expect(!supplyLine.HasMetDefenderWinCondition, "Fresh convoy should not have reached safety yet");

                supplyLine.Tick(1f); // 0.5 progress
                Expect(!supplyLine.HasMetDefenderWinCondition, "Convoy should not be at safety after 1 of 2 seconds");

                supplyLine.Tick(1f); // 1.0 progress
                Expect(supplyLine.HasMetDefenderWinCondition, "Convoy should reach safety (defender win) after enough escort time");
                Expect(!supplyLine.HasMetAttackerWinCondition, "Undamaged convoy should never satisfy the attacker win condition");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void TestSupplyLineObjectiveAttackerWinViaDestruction()
        {
            var go = new GameObject("SmokeTest_SupplyLine_Destroyed");
            try
            {
                var supplyLine = go.AddComponent<SupplyLineObjective>();
                supplyLine.attackerWinDestroyedFraction = 0.6f;
                supplyLine.escortProgressPerSecond = 0.01f; // slow, so destruction wins the race
                supplyLine.Damageable.Configure(100f, 0f);

                supplyLine.Damageable.TakeDamage(70f, 0f);
                supplyLine.Tick(1f);

                Expect(supplyLine.HasMetAttackerWinCondition, "Convoy destroyed past threshold should satisfy attacker win");
                Expect(!supplyLine.HasMetDefenderWinCondition, "Barely-escorted, mostly-destroyed convoy should not also claim defender win");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void TestPointDefenseBatteryConsumesAmmoRegardlessOfOutcome()
        {
            var alwaysHitGo = new GameObject("SmokeTest_PointDefenseBattery_AlwaysHit");
            var alwaysMissGo = new GameObject("SmokeTest_PointDefenseBattery_AlwaysMiss");
            PointDefenseDefinition alwaysHitDef = ScriptableObject.CreateInstance<PointDefenseDefinition>();
            PointDefenseDefinition alwaysMissDef = ScriptableObject.CreateInstance<PointDefenseDefinition>();
            try
            {
                alwaysHitDef.id = "smoketest.pd.always-hit";
                alwaysHitDef.ammoCount = 2;
                alwaysHitDef.interceptProbability = 1f; // always intercepts, if it has ammo

                var alwaysHitBattery = alwaysHitGo.AddComponent<PointDefenseBattery>();
                alwaysHitBattery.Configure(alwaysHitDef);

                Expect(alwaysHitBattery.RemainingAmmo == 2, "Should start with 2 interceptors");
                Expect(alwaysHitBattery.TryIntercept(), "First intercept should succeed (100% chance)");
                Expect(alwaysHitBattery.RemainingAmmo == 1, "One interceptor should have been spent");
                Expect(alwaysHitBattery.TryIntercept(), "Second intercept should succeed");
                Expect(alwaysHitBattery.RemainingAmmo == 0, "Battery should now be out of ammo");
                Expect(!alwaysHitBattery.TryIntercept(), "Depleted battery cannot intercept a third time, even at 100% probability");

                // A battery that always MISSES should still spend ammo — this is the
                // core stockpile-drain mechanic: a decoy still costs the defender a shot.
                alwaysMissDef.id = "smoketest.pd.always-miss";
                alwaysMissDef.ammoCount = 1;
                alwaysMissDef.interceptProbability = 0f;

                var alwaysMissBattery = alwaysMissGo.AddComponent<PointDefenseBattery>();
                alwaysMissBattery.Configure(alwaysMissDef);

                bool intercepted = alwaysMissBattery.TryIntercept();
                Expect(!intercepted, "0% probability battery should never actually intercept");
                Expect(alwaysMissBattery.RemainingAmmo == 0, "Ammo should still be spent on a missed shot at a decoy");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(alwaysHitGo);
                UnityEngine.Object.DestroyImmediate(alwaysMissGo);
                UnityEngine.Object.DestroyImmediate(alwaysHitDef);
                UnityEngine.Object.DestroyImmediate(alwaysMissDef);
            }
        }

        private static void TestEngagementControllerCommitAttackerStrikePipeline()
        {
            var objectiveGo = new GameObject("SmokeTest_Objective_StrikePipeline");
            var controllerGo = new GameObject("SmokeTest_Controller_StrikePipeline");
            var batteryGo = new GameObject("SmokeTest_Battery_StrikePipeline");
            PointDefenseDefinition drone = MakePart<PointDefenseDefinition>("smoketest.strike.drone");
            PointDefenseDefinition alwaysMissBattery = ScriptableObject.CreateInstance<PointDefenseDefinition>();
            try
            {
                var objective = objectiveGo.AddComponent<BaseObjective>();
                objective.Damageable.Configure(100f, 0f);

                alwaysMissBattery.id = "smoketest.pd.pipeline";
                alwaysMissBattery.ammoCount = 1;
                alwaysMissBattery.interceptProbability = 0f; // never intercepts — first strike should land

                var battery = batteryGo.AddComponent<PointDefenseBattery>();
                battery.Configure(alwaysMissBattery);

                var controller = controllerGo.AddComponent<EngagementController>();
                controller.Objective = objective;
                controller.attackerLoadout = new[]
                {
                    new StockpileEntry { part = drone, startingCount = 2, rawDamage = 30f, payloadSize = 0f },
                };
                controller.defenderLoadout = new StockpileEntry[0];
                controller.defenderBatteries = new[] { battery };
                controller.Initialize();

                AttackerStrikeResult first = controller.CommitAttackerStrike("smoketest.strike.drone");
                Expect(first == AttackerStrikeResult.Hit, $"First strike should get through (battery never intercepts) and hit, got {first}");
                Expect(Math.Abs(objective.Damageable.CurrentHealth - 70f) < 0.001f, $"Objective should take 30 damage, got {objective.Damageable.CurrentHealth}");

                AttackerStrikeResult second = controller.CommitAttackerStrike("smoketest.strike.drone");
                Expect(second == AttackerStrikeResult.Hit, $"Second strike should also hit (only depleted, not intercepted, was tested here)");

                AttackerStrikeResult third = controller.CommitAttackerStrike("smoketest.strike.drone");
                Expect(third == AttackerStrikeResult.Depleted, $"Third strike should fail — only 2 in stock, got {third}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(objectiveGo);
                UnityEngine.Object.DestroyImmediate(controllerGo);
                UnityEngine.Object.DestroyImmediate(batteryGo);
                UnityEngine.Object.DestroyImmediate(drone);
                UnityEngine.Object.DestroyImmediate(alwaysMissBattery);
            }
        }

        private static void TestStandingOrderExecutorSequencesDecoysFirst()
        {
            var executor = new StandingOrderExecutor
            {
                Order = StandingOrder.AttackNow,
                CommitIntervalSeconds = 1f,
                CommitPriorityPartIds = new[] { "decoy", "strike" },
            };

            var stockpile = new System.Collections.Generic.Dictionary<string, int> { { "decoy", 2 }, { "strike", 1 } };
            bool TryCommit(string id)
            {
                if (stockpile.TryGetValue(id, out int count) && count > 0)
                {
                    stockpile[id] = count - 1;
                    return true;
                }
                return false;
            }

            // Sub-interval tick should not commit anything yet.
            executor.Tick(0.5f, TryCommit);
            Expect(stockpile["decoy"] == 2, "Should not commit before the interval elapses");

            executor.Tick(0.5f, TryCommit); // total 1.0s -> first commit
            Expect(stockpile["decoy"] == 1, "First interval should commit a decoy (priority order)");

            executor.Tick(1f, TryCommit); // second decoy
            Expect(stockpile["decoy"] == 0, "Second interval should commit the last decoy");

            executor.Tick(1f, TryCommit); // decoys depleted -> should fall through to strike
            Expect(stockpile["strike"] == 0, "Once decoys are depleted, the executor should advance to the strike priority");
        }

        private static void TestSeekerControlModel()
        {
            Expect(SeekerControlModel.IsManuallyFlyable(SeekerType.WireOrDatalinkGuided), "Command-guided/datalink munitions should be manually flyable");
            Expect(!SeekerControlModel.IsManuallyFlyable(SeekerType.ActiveRadar), "Autonomous active-radar seekers should not be manually flyable");
            Expect(!SeekerControlModel.IsManuallyFlyable(SeekerType.Infrared), "Autonomous IR seekers should not be manually flyable");
            Expect(!SeekerControlModel.IsManuallyFlyable(SeekerType.LaserDesignated), "Laser-designated munitions are guided by a designator, not flown by the player");
            Expect(!SeekerControlModel.IsManuallyFlyable(SeekerType.AntiRadiation), "Anti-radiation seekers are autonomous once locked");

            Expect(SeekerControlModel.RequiresActiveDesignation(SeekerType.LaserDesignated), "Laser-designated munitions require an active designator");
            Expect(SeekerControlModel.RequiresActiveDesignation(SeekerType.SemiActiveRadar), "Semi-active radar munitions require illumination");
            Expect(!SeekerControlModel.RequiresActiveDesignation(SeekerType.ActiveRadar), "Active radar seekers illuminate their own target");
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
