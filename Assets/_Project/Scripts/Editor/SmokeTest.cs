using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Vanquish.Combat;
using Vanquish.Combat.Play;
using Vanquish.Core;
using Vanquish.Data;
using Vanquish.Data.Support;
using Vanquish.Simulation.Damage;
using Vanquish.Simulation.Flight;
using Vanquish.Theatre;
using Vanquish.Theatre.Play;

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

            // Phase 2
            TestHexGridMovementCostAndFrontLine();
            TestTheatreMovementReachableHexes();
            TestSiteConstructionRepairRelocateLifecycle();
            TestEconomicCollapseCondition();
            TestTerritorialControlConditionSustain();
            TestTheatreTurnControllerIntegration();

            // Phase 1 — flight/visual layer
            TestFlightTestHarnessBuildsAWorkingScene();
            TestMissileFactorySpawnsAWorkingMissile();

            // Phase 2 — visible/clickable theatre map
            TestHexMeshFactoryAxialToWorldMatchesNeighborSpacing();
            TestHexMeshFactoryPrismCapsFaceOutward();
            TestTheatreMapHarnessBuildsAClickableMap();
            TestTheatreMapHarnessBuildMenu();
            TestTheatreMapHarnessLabAndFactoryProduction();
            TestTheatreMapHarnessSaveLoadRoundTrip();
            TestTheatreMapHarnessPointerOverUIDoesNotThrow();

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

        private static void TestHexGridMovementCostAndFrontLine()
        {
            var grid = new HexGrid();
            var playerHex = new HexCoordinate(0, 0);
            var roadHex = new HexCoordinate(-1, 0);
            var enemyHex = new HexCoordinate(1, 0);
            var mountainHex = new HexCoordinate(0, -1);

            grid.GetOrAddTile(playerHex, TerrainType.Open, TheatreFaction.Player);
            grid.GetOrAddTile(roadHex, TerrainType.Road, TheatreFaction.Player);
            grid.GetOrAddTile(enemyHex, TerrainType.Open, TheatreFaction.Enemy);
            grid.GetOrAddTile(mountainHex, TerrainType.Mountain, TheatreFaction.Neutral);

            Expect(Math.Abs(grid.GetTile(playerHex).MovementCostToEnter() - 1f) < 0.001f, "Open terrain should cost 1 to enter");
            Expect(Math.Abs(grid.GetTile(roadHex).MovementCostToEnter() - 0.25f) < 0.001f, "Road terrain should be much cheaper to enter");
            Expect(!grid.GetTile(mountainHex).IsPassable, "Mountain terrain should be impassable");

            Expect(grid.IsFrontLineTile(playerHex), "Player hex adjacent to an enemy hex should be on the front line");
            Expect(grid.IsFrontLineTile(enemyHex), "Enemy hex adjacent to a player hex should be on the front line");
            Expect(!grid.IsFrontLineTile(roadHex), "Player hex only adjacent to other player/neutral hexes should not be on the front line");

            Expect(HexCoordinate.Distance(playerHex, enemyHex) == 1, "Adjacent hexes should be distance 1 apart");
        }

        private static void TestTheatreMovementReachableHexes()
        {
            var grid = new HexGrid();
            var start = new HexCoordinate(0, 0);
            var roadStep1 = new HexCoordinate(1, 0);
            var roadStep2 = new HexCoordinate(2, 0);
            var openStep = new HexCoordinate(0, 1);
            var mountainBlock = new HexCoordinate(3, 0);
            var beyondMountain = new HexCoordinate(4, 0);

            grid.GetOrAddTile(start, TerrainType.Open, TheatreFaction.Player);
            grid.GetOrAddTile(roadStep1, TerrainType.Road, TheatreFaction.Player);
            grid.GetOrAddTile(roadStep2, TerrainType.Road, TheatreFaction.Player);
            grid.GetOrAddTile(openStep, TerrainType.Open, TheatreFaction.Player);
            grid.GetOrAddTile(mountainBlock, TerrainType.Mountain, TheatreFaction.Neutral);
            grid.GetOrAddTile(beyondMountain, TerrainType.Open, TheatreFaction.Neutral);

            // Budget bottlenecked by the slowest unit in the army (PLAN.md rule) — a
            // fast drone paired with a slow logistics element moves at 1.0/turn.
            float armyBudget = TheatreMovement.ComputeArmyMovementBudget(new[] { 5f, 1f, 3f });
            Expect(Math.Abs(armyBudget - 1f) < 0.001f, $"Army movement budget should be bottlenecked to the slowest unit (1.0), got {armyBudget}");

            // Use a smaller, explicit budget here to clearly demonstrate "roads let you
            // go further than open terrain for the same budget" — the whole point of
            // the multi-front-reach mechanic.
            const float testBudget = 0.6f;
            var reachable = TheatreMovement.ComputeReachableHexes(grid, start, testBudget);

            Expect(reachable.ContainsKey(start), "Start hex should always be reachable (cost 0)");
            Expect(reachable.ContainsKey(roadStep1), "One road hex should be reachable within budget 0.6 (cost 0.25)");
            Expect(reachable.ContainsKey(roadStep2), $"A SECOND road hex should be reachable within budget 0.6 (cumulative cost 0.5) — this is the 'roads let you reach further' mechanic, got hexes: {string.Join(",", reachable.Keys)}");
            Expect(!reachable.ContainsKey(openStep), "Open terrain (cost 1.0) should NOT be reachable within budget 0.6 — same budget reaches much further along roads than through open terrain");
            Expect(!reachable.ContainsKey(beyondMountain), "Hex beyond an impassable mountain should never be reachable regardless of budget");
        }

        private static void TestSiteConstructionRepairRelocateLifecycle()
        {
            Site site = Site.BeginConstruction(SiteType.Factory, TheatreFaction.Player, new HexCoordinate(0, 0), turnsToBuild: 2);
            Expect(site.State == SiteState.UnderConstruction, "New site should start under construction");
            Expect(!site.IsOperational, "Site under construction should not be operational");

            site.Tick();
            Expect(site.State == SiteState.UnderConstruction, "Site should still be under construction after 1 of 2 turns");

            site.Tick();
            Expect(site.IsOperational, "Site should become operational after enough construction turns");
            Expect(Math.Abs(site.HealthFraction01 - 1f) < 0.001f, "Newly completed site should be at full health");

            site.ApplyDamage(0.3f);
            Expect(Math.Abs(site.HealthFraction01 - 0.7f) < 0.001f, $"Expected 70% health after 30% damage, got {site.HealthFraction01:P0}");
            Expect(site.IsOperational, "A damaged-but-not-destroyed site should remain operational");

            Expect(site.BeginRepair(2), "Should be able to begin repairing a damaged operational site");
            Expect(site.State == SiteState.Repairing, "Site should now be repairing");
            site.Tick();
            site.Tick();
            Expect(site.IsOperational, "Site should return to operational after repair completes");
            Expect(Math.Abs(site.HealthFraction01 - 1f) < 0.001f, "Repaired site should be back to full health");

            var newLocation = new HexCoordinate(5, 5);
            Expect(site.BeginRelocation(newLocation, 1), "Should be able to begin relocating an operational site");
            Expect(site.State == SiteState.Relocating, "Site should now be relocating");
            Expect(site.Location.Equals(newLocation), "Relocation should update the site's location immediately");
            site.Tick();
            Expect(site.IsOperational, "Site should return to operational after relocation completes");

            site.ApplyDamage(2f); // way more than 100% health
            Expect(site.State == SiteState.Destroyed, "Site should be destroyed once health reaches zero");
            site.Tick();
            Expect(site.State == SiteState.Destroyed, "Ticking a destroyed site should be a no-op");
        }

        private static void TestEconomicCollapseCondition()
        {
            var grid = new HexGrid();
            var world = new TheatreWorldState(grid);
            world.Sites.Add(Site.BeginConstruction(SiteType.Factory, TheatreFaction.Player, new HexCoordinate(0, 0), 1));
            world.Sites[0].Tick(); // now Operational

            var condition = new EconomicCollapseCondition();
            Expect(condition.Evaluate(world) == TheatreFaction.Player, "Enemy has zero factories at all — Player should win by economic collapse");

            world.Sites.Add(Site.BeginConstruction(SiteType.Factory, TheatreFaction.Enemy, new HexCoordinate(1, 0), 3));
            Expect(condition.Evaluate(world) == null, "Enemy now has a factory under construction — not yet collapsed, no winner");
        }

        private static void TestTerritorialControlConditionSustain()
        {
            var grid = new HexGrid();
            grid.GetOrAddTile(new HexCoordinate(0, 0), TerrainType.Open, TheatreFaction.Player);
            grid.GetOrAddTile(new HexCoordinate(1, 0), TerrainType.Open, TheatreFaction.Player);
            grid.GetOrAddTile(new HexCoordinate(2, 0), TerrainType.Open, TheatreFaction.Player);
            grid.GetOrAddTile(new HexCoordinate(3, 0), TerrainType.Open, TheatreFaction.Enemy);

            var world = new TheatreWorldState(grid);
            var condition = new TerritorialControlCondition(requiredFraction: 0.75f, requiredConsecutiveTurns: 2);

            Expect(Math.Abs(grid.OwnershipFraction(TheatreFaction.Player) - 0.75f) < 0.001f, "Player should own exactly 75% of the 4 contestable hexes");

            Expect(condition.Evaluate(world) == null, "First turn meeting the threshold should not win yet — needs to be sustained");
            Expect(condition.ConsecutiveTurnsMet(TheatreFaction.Player) == 1, $"Player should have 1 consecutive turn met so far, got {condition.ConsecutiveTurnsMet(TheatreFaction.Player)}");
            Expect(condition.ConsecutiveTurnsMet(TheatreFaction.Enemy) == 0, "Enemy never met the threshold, so should have 0 consecutive turns");

            Expect(condition.Evaluate(world) == TheatreFaction.Player, "Second consecutive turn meeting the threshold should win for Player");

            // Save/load restoration: a fresh condition instance should be able to
            // pick up exactly where a saved sustain-counter left off.
            var restored = new TerritorialControlCondition(requiredFraction: 0.75f, requiredConsecutiveTurns: 2);
            restored.RestoreConsecutiveTurns(TheatreFaction.Player, 1);
            Expect(restored.ConsecutiveTurnsMet(TheatreFaction.Player) == 1, "RestoreConsecutiveTurns should be reflected by ConsecutiveTurnsMet");
            Expect(restored.Evaluate(world) == TheatreFaction.Player, "A restored count of 1 (of 2 required) should win on the very next Evaluate, same as if it had never been saved/loaded");
        }

        private static void TestTheatreTurnControllerIntegration()
        {
            var grid = new HexGrid();
            grid.GetOrAddTile(new HexCoordinate(0, 0), TerrainType.Open, TheatreFaction.Player);
            grid.GetOrAddTile(new HexCoordinate(1, 0), TerrainType.Open, TheatreFaction.Enemy);

            var world = new TheatreWorldState(grid);
            var factory = Site.BeginConstruction(SiteType.Factory, TheatreFaction.Player, new HexCoordinate(0, 0), turnsToBuild: 1);
            world.Sites.Add(factory);

            var controller = new TheatreTurnController(world, new ITheatreVictoryCondition[]
            {
                new EconomicCollapseCondition(),
            });

            Expect(controller.Result == TheatreResult.InProgress, "Fresh controller should be InProgress");

            controller.AdvanceTurn(); // factory completes construction this tick, becomes Operational
            Expect(factory.IsOperational, "Factory should be operational after its construction turn completes");
            Expect(controller.Result == TheatreResult.PlayerVictory,
                $"Enemy owns no factories at all — Player should win by economic collapse on the very first turn the condition is checked, got {controller.Result}");

            int turnAfterWin = controller.CurrentTurn;
            controller.AdvanceTurn();
            Expect(controller.CurrentTurn == turnAfterWin, "AdvanceTurn should be a no-op once the game has already resolved");
        }

        private static void TestFlightTestHarnessBuildsAWorkingScene()
        {
            var harnessGo = new GameObject("SmokeTest_FlightTestHarness");
            try
            {
                var harness = harnessGo.AddComponent<FlightTestHarness>();
                harness.Build();

                var objective = UnityEngine.Object.FindFirstObjectByType<BaseObjective>();
                Expect(objective != null, "FlightTestHarness.Build should create a BaseObjective");
                Expect(objective != null && Math.Abs(objective.Damageable.MaxHealth - 200f) < 0.001f, "Objective should be configured with 200 max health");

                var controller = UnityEngine.Object.FindFirstObjectByType<EngagementController>();
                Expect(controller != null, "FlightTestHarness.Build should create an EngagementController");
                Expect(controller != null && controller.Attacker.RemainingCount("flighttest.quad.missile") == 4, "Quad stockpile should start with 4 missiles");
                Expect(controller != null && controller.Attacker.RemainingCount("flighttest.hex.missile") == 4, "Hex stockpile should start with 4 missiles");
                Expect(controller != null && controller.Objective != null && ReferenceEquals(controller.Objective, objective), "EngagementController's objective should be the same BaseObjective instance");

                GameObject quadGo = GameObject.Find("Quadcopter");
                GameObject hexGo = GameObject.Find("Hexacopter");
                Expect(quadGo != null, "FlightTestHarness.Build should create a 'Quadcopter' unit");
                Expect(hexGo != null, "FlightTestHarness.Build should create a 'Hexacopter' unit");

                WeaponController quadWeapon = quadGo?.GetComponent<WeaponController>();
                WeaponController hexWeapon = hexGo?.GetComponent<WeaponController>();
                Expect(quadWeapon != null && quadWeapon.missilePartId == "flighttest.quad.missile", "Quad's weapon should use the quad-specific part id");
                Expect(hexWeapon != null && hexWeapon.missilePartId == "flighttest.hex.missile", "Hex's weapon should use the hex-specific part id");
                Expect(quadWeapon != null && quadWeapon.target == objective.transform, "Quad's weapon target should be the objective");

                FlightBody quadFlightBody = quadGo?.GetComponent<FlightBody>();
                Expect(quadFlightBody != null && !quadFlightBody.orientToVelocity, "Multirotor drone's FlightBody should have orientToVelocity disabled");

                Expect(quadGo?.GetComponent<PlayerDroneController>() != null, "Quad should have a PlayerDroneController");
                Expect(hexGo?.GetComponent<PlayerDroneController>() != null, "Hex should have a PlayerDroneController");

                var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
                Expect(camera != null, "FlightTestHarness.Build should create a camera");

                var hud = UnityEngine.Object.FindFirstObjectByType<FlightHUD>();
                Expect(hud != null, "FlightTestHarness.Build should create a FlightHUD");

                var switcher = UnityEngine.Object.FindFirstObjectByType<PlayerUnitSwitcher>();
                Expect(switcher != null, "FlightTestHarness.Build should create a PlayerUnitSwitcher");
                Expect(switcher != null && switcher.ActiveIndex == 0, "Quad (index 0) should be active by default");
                Expect(quadGo?.GetComponent<PlayerDroneController>().IsActive == true, "Quad should be active by default");
                Expect(hexGo?.GetComponent<PlayerDroneController>().IsActive == false, "Hex should NOT be active by default");

                var cameraMode = UnityEngine.Object.FindFirstObjectByType<CameraModeController>();
                Expect(cameraMode != null, "FlightTestHarness.Build should create a CameraModeController");

                // Switching units should hand camera-follow over to the newly active unit.
                switcher.SetActive(1);
                Expect(switcher.ActiveIndex == 1, "Switching to index 1 should update ActiveIndex");
                Expect(hexGo.GetComponent<PlayerDroneController>().IsActive, "Hex should become active after switching");
                Expect(!quadGo.GetComponent<PlayerDroneController>().IsActive, "Quad should become inactive after switching away");
                var chaseCamera = UnityEngine.Object.FindFirstObjectByType<ChaseCamera>();
                Expect(chaseCamera != null && chaseCamera.primary == hexGo.transform, "Camera should follow the newly active unit (hex) after switching");
                Expect(!cameraMode.IsObjectiveView, "Switching units should return the camera to follow mode, not objective view");

                cameraMode.ToggleObjectiveView();
                Expect(cameraMode.IsObjectiveView, "Toggling should enter objective view");
                Expect(chaseCamera.primary == objective.transform && chaseCamera.secondary == null, "Objective view should point the camera at the objective alone");
                cameraMode.ToggleObjectiveView();
                Expect(!cameraMode.IsObjectiveView && chaseCamera.primary == hexGo.transform, "Toggling again should return to following the active unit");

                switcher.SetActive(0); // back to quad for the ammo-independence check below

                var roster = UnityEngine.Object.FindFirstObjectByType<UnitRosterHud>();
                Expect(roster != null && roster.entries.Count == 2, "UnitRosterHud should list both units");

                var quadMountedVisuals = quadGo.GetComponent<MountedMissileVisuals>();
                var hexMountedVisuals = hexGo.GetComponent<MountedMissileVisuals>();
                Expect(quadMountedVisuals != null && quadMountedVisuals.MountedCount == 4, "Quad should start with 4 visibly mounted missile props");
                Expect(hexMountedVisuals != null && hexMountedVisuals.MountedCount == 4, "Hex should start with 4 visibly mounted missile props");

                // Firing the quad should consume ONLY the quad's stockpile/visuals —
                // the whole point of giving each unit its own part id.
                bool fired = quadWeapon.Fire();
                Expect(fired, "Quad's Weapon.Fire() should succeed with full ammo and a valid target");
                Expect(controller.Attacker.RemainingCount("flighttest.quad.missile") == 3, "Firing the quad should consume one quad missile");
                Expect(controller.Attacker.RemainingCount("flighttest.hex.missile") == 4, "Firing the quad should NOT touch the hex's stockpile");
                Expect(quadMountedVisuals.MountedCount == 3, "Firing the quad should visually remove one of ITS mounted missile props");
                Expect(hexMountedVisuals.MountedCount == 4, "Firing the quad should NOT remove any of the hex's mounted missile props");

                var spawnedMissile = UnityEngine.Object.FindFirstObjectByType<MissileImpact>();
                Expect(spawnedMissile != null, "Firing should spawn a missile with a MissileImpact component");
            }
            finally
            {
                // Clean up everything Build() created, not just the harness object —
                // named lookup rather than nuking every GameObject in the scene, so
                // this can't accidentally destroy something unrelated.
                foreach (string goName in new[]
                         {
                             "SmokeTest_FlightTestHarness", "Ground", "Directional Light",
                             "Objective (Base)", "EngagementController", "Quadcopter", "Hexacopter",
                             "Main Camera", "CameraModeController", "PlayerUnitSwitcher",
                             "HUD", "UnitRosterHud", "Missile",
                         })
                {
                    GameObject found = GameObject.Find(goName);
                    if (found != null)
                        UnityEngine.Object.DestroyImmediate(found);
                }
            }
        }

        private static void TestMissileFactorySpawnsAWorkingMissile()
        {
            var targetGo = new GameObject("SmokeTest_MissileTarget");
            GameObject missile = null;
            try
            {
                targetGo.transform.position = new Vector3(0f, 0f, 50f);

                missile = MissileFactory.SpawnMissile(Vector3.zero, Quaternion.identity, targetGo.transform, rawDamage: 42f, payloadSize: 17f);

                Expect(missile != null, "SpawnMissile should return a GameObject");

                var flightBody = missile.GetComponent<FlightBody>();
                Expect(flightBody != null, "Missile should have a FlightBody");
                Expect(flightBody != null && flightBody.isThrusting, "Missile should start thrusting");
                Expect(flightBody != null && flightBody.orientToVelocity, "Missile should orient to velocity (not a multirotor)");

                var guidance = missile.GetComponent<Vanquish.Simulation.Guidance.GuidanceController>();
                Expect(guidance != null && guidance.target == targetGo.transform, "Missile's guidance should target the given transform");

                var burn = missile.GetComponent<MissileBurnController>();
                Expect(burn != null && burn.flightBody == flightBody, "Missile should have a burn controller wired to its own FlightBody");

                var impact = missile.GetComponent<MissileImpact>();
                Expect(impact != null, "Missile should have a MissileImpact");
                Expect(impact != null && Math.Abs(impact.rawDamage - 42f) < 0.001f, "Missile's impact damage should match the requested rawDamage");
                Expect(impact != null && Math.Abs(impact.payloadSize - 17f) < 0.001f, "Missile's impact payload size should match the requested payloadSize");

                var collider = missile.GetComponent<CapsuleCollider>();
                Expect(collider != null && collider.direction == 2, "Missile's collider should be Z-oriented (direction=2), matching FlightBody's forward-thrust convention");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetGo);
                if (missile != null)
                    UnityEngine.Object.DestroyImmediate(missile);
            }
        }

        private static void TestHexMeshFactoryAxialToWorldMatchesNeighborSpacing()
        {
            var center = new HexCoordinate(2, 3);
            Vector3 centerWorld = HexMeshFactory.AxialToWorld(center, radius: 1f);

            float expectedNeighborDistance = Mathf.Sqrt(3f);
            foreach (HexCoordinate neighbor in center.Neighbors())
            {
                Vector3 neighborWorld = HexMeshFactory.AxialToWorld(neighbor, radius: 1f);
                float distance = Vector3.Distance(centerWorld, neighborWorld);
                Expect(Mathf.Abs(distance - expectedNeighborDistance) < 0.001f,
                    $"Neighbor {neighbor} should be sqrt(3)*radius from center, got {distance}");
            }
        }

        private static void TestHexMeshFactoryPrismCapsFaceOutward()
        {
            // Regression test: the top/bottom cap winding order was once swapped,
            // which culled the top face entirely from above (the tile looked like an
            // open-topped dish with no lid) — verify the actual computed vertex
            // normals face outward (+Y top, -Y bottom), not just that "a mesh was
            // returned." Vertex layout is CreateHexPrism's own: 0 = top center,
            // 1-6 = top rim, 7 = bottom center, 8-13 = bottom rim.
            Mesh mesh = HexMeshFactory.CreateHexPrism(radius: 1f, height: 0.6f);
            Vector3[] normals = mesh.normals;

            Expect(normals.Length >= 14, $"Prism mesh should have at least 14 vertices (2 caps + centers), got {normals.Length}");
            Expect(normals[0].y > 0.9f, $"Top cap center's normal should face upward (+Y), got {normals[0]}");
            Expect(normals[7].y < -0.9f, $"Bottom cap center's normal should face downward (-Y), got {normals[7]}");
        }

        private static void TestTheatreMapHarnessBuildsAClickableMap()
        {
            var harnessGo = new GameObject("SmokeTest_TheatreMapHarness");
            try
            {
                var harness = harnessGo.AddComponent<TheatreMapHarness>();
                harness.Build();

                Expect(harness.World.Grid.Tiles.Count == 100, $"Map should be a 10x10 = 100 hex grid, got {harness.World.Grid.Tiles.Count}");

                int playerHexes = harness.World.Grid.CountOwnedBy(TheatreFaction.Player);
                int enemyHexes = harness.World.Grid.CountOwnedBy(TheatreFaction.Enemy);
                Expect(playerHexes == 50, $"Player should own 5 of 10 columns * 10 rows = 50 hexes, got {playerHexes}");
                Expect(enemyHexes == 50, $"Enemy should own 5 of 10 columns * 10 rows = 50 hexes, got {enemyHexes}");

                Expect(harness.World.Sites.Count == 4, $"Should have 4 sites (factory+base per side), got {harness.World.Sites.Count}");
                Expect(harness.World.Sites.Any(s => s.Owner == TheatreFaction.Player && s.Type == SiteType.Factory && s.IsOperational), "Player factory should exist and be operational");
                Expect(harness.World.Sites.Any(s => s.Owner == TheatreFaction.Enemy && s.Type == SiteType.Factory && s.IsOperational), "Enemy factory should exist and be operational");

                var camera = harnessGo.GetComponentInChildren<Camera>();
                Expect(camera != null, "Build should create a camera");
                var cameraController = harnessGo.GetComponentInChildren<TheatreMapCameraController>();
                Expect(cameraController != null, "Build should attach a TheatreMapCameraController");
                Expect(cameraController != null && cameraController.focusPoint != Vector3.zero,
                    "Camera controller's focus point should be set to the grid center, not left at the default zero");

                var tileViewCount = harnessGo.GetComponentsInChildren<HexTileView>().Length;
                Expect(tileViewCount == 100, $"Should create one HexTileView per hex, got {tileViewCount}");
                var siteViewCount = harnessGo.GetComponentsInChildren<SiteMarkerView>().Length;
                Expect(siteViewCount == 4, $"Should create one SiteMarkerView per site, got {siteViewCount}");

                // A hex on the front line (Enemy-owned, adjacent to a Player hex) should be capturable...
                HexTile frontHex = harness.World.Grid.Tiles.FirstOrDefault(t =>
                    t.Owner == TheatreFaction.Enemy && harness.World.Grid.NeighborsOf(t.Coordinate).Any(n => n.Owner == TheatreFaction.Player));
                Expect(frontHex != null, "There should be at least one Enemy hex adjacent to Player territory (a front line)");

                harness.SelectTile(frontHex);
                Expect(harness.SelectedTile == frontHex, "SelectTile should set SelectedTile");
                Expect(harness.CanCaptureSelectedTile(), "A front-line Enemy hex adjacent to Player territory should be capturable");

                bool captured = harness.TryCaptureSelectedTile();
                Expect(captured, "TryCaptureSelectedTile should succeed on a capturable hex");
                Expect(frontHex.Owner == TheatreFaction.Player, "Captured hex should now be Player-owned");

                // ...but a hex deep in enemy territory (no Player neighbor) should NOT be.
                HexTile deepHex = harness.World.Grid.Tiles.FirstOrDefault(t =>
                    t.Owner == TheatreFaction.Enemy && !harness.World.Grid.NeighborsOf(t.Coordinate).Any(n => n.Owner == TheatreFaction.Player));
                Expect(deepHex != null, "There should be at least one Enemy hex NOT adjacent to Player territory");
                harness.SelectTile(deepHex);
                Expect(!harness.CanCaptureSelectedTile(), "A hex with no adjacent Player territory should not be capturable");

                // Advancing a turn should tick production for both operational factories.
                int playerResourceBefore = harness.Controller.ResourcePool[TheatreFaction.Player];
                int enemyResourceBefore = harness.Controller.ResourcePool[TheatreFaction.Enemy];
                harness.AdvanceTurn();
                Expect(harness.Controller.CurrentTurn == 1, "AdvanceTurn should increment the turn counter");
                Expect(harness.Controller.ResourcePool[TheatreFaction.Player] == playerResourceBefore + harness.Controller.ResourcePerOperationalFactoryPerTurn,
                    "Player's operational factory should have produced resources this turn");
                Expect(harness.Controller.ResourcePool[TheatreFaction.Enemy] == enemyResourceBefore + harness.Controller.ResourcePerOperationalFactoryPerTurn,
                    "Enemy's operational factory should have produced resources this turn");
            }
            finally
            {
                // Everything Build() creates is parented under the harness GameObject,
                // so destroying it alone is sufficient cleanup.
                UnityEngine.Object.DestroyImmediate(harnessGo);
            }
        }

        private static void TestTheatreMapHarnessBuildMenu()
        {
            var harnessGo = new GameObject("SmokeTest_TheatreMapHarness_BuildMenu");
            try
            {
                var harness = harnessGo.AddComponent<TheatreMapHarness>();
                harness.Build();

                int siteCountBefore = harnessGo.GetComponentsInChildren<SiteMarkerView>().Length;
                Expect(siteCountBefore == 4, $"Should start with the 4 seeded sites, got {siteCountBefore}");

                // An empty, Open, Player-owned hex should be buildable.
                HexTile emptyOpenPlayerHex = harness.World.Grid.Tiles.FirstOrDefault(t =>
                    t.Owner == TheatreFaction.Player && t.Terrain == TerrainType.Open && !harness.HasActiveSite(t.Coordinate));
                Expect(emptyOpenPlayerHex != null, "There should be at least one empty, open, Player-owned hex");

                harness.SelectTile(emptyOpenPlayerHex);
                Expect(harness.CanBuildOnSelectedTile(), "Empty open Player-owned hex should be buildable");

                bool started = harness.TryBeginConstructionOnSelectedTile(SiteType.Warehouse, 2);
                Expect(started, "TryBeginConstructionOnSelectedTile should succeed on a buildable hex");
                Expect(harness.HasActiveSite(emptyOpenPlayerHex.Coordinate), "Hex should now have an active (under-construction) site");
                Expect(!harness.CanBuildOnSelectedTile(), "Hex should no longer be buildable once occupied");

                int siteCountAfter = harnessGo.GetComponentsInChildren<SiteMarkerView>().Length;
                Expect(siteCountAfter == siteCountBefore + 1, $"Should have created one new SiteMarkerView, got {siteCountAfter - siteCountBefore} new");

                Site newSite = harness.World.Sites.First(s => s.Location.Equals(emptyOpenPlayerHex.Coordinate) && s.Type == SiteType.Warehouse);
                Expect(newSite.State == SiteState.UnderConstruction, "New site should start under construction");
                Expect(newSite.TurnsRemaining == 2, $"New site should need 2 turns, got {newSite.TurnsRemaining}");

                harness.AdvanceTurn();
                Expect(newSite.State == SiteState.UnderConstruction && newSite.TurnsRemaining == 1, "Site should still be under construction after 1 of 2 turns");

                harness.AdvanceTurn();
                Expect(newSite.IsOperational, "Site should become Operational after its construction turns complete via the normal Advance Turn flow");

                // A Player-owned Road hex should NOT be buildable (blocked terrain).
                HexTile playerRoadHex = harness.World.Grid.Tiles.FirstOrDefault(t =>
                    t.Owner == TheatreFaction.Player && t.Terrain == TerrainType.Road && !harness.HasActiveSite(t.Coordinate));
                Expect(playerRoadHex != null, "There should be at least one empty Player-owned Road hex");
                harness.SelectTile(playerRoadHex);
                Expect(!harness.CanBuildOnSelectedTile(), "Road terrain should not be buildable");

                // An Enemy-owned hex should never be buildable regardless of terrain.
                HexTile enemyHex = harness.World.Grid.Tiles.First(t => t.Owner == TheatreFaction.Enemy);
                harness.SelectTile(enemyHex);
                Expect(!harness.CanBuildOnSelectedTile(), "Enemy-owned hex should never be buildable");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(harnessGo);
            }
        }

        private static void TestTheatreMapHarnessLabAndFactoryProduction()
        {
            var harnessGo = new GameObject("SmokeTest_TheatreMapHarness_LabFactory");
            try
            {
                var harness = harnessGo.AddComponent<TheatreMapHarness>();
                harness.Build();

                SiteBuildOption labOption = SiteBuildCatalog.Options.FirstOrDefault(o => o.Type == SiteType.Lab);
                Expect(labOption.DisplayName != null, "SiteBuildCatalog should offer a Lab option");

                // --- Build a Lab ---
                HexTile labHex = harness.World.Grid.Tiles.FirstOrDefault(t =>
                    t.Owner == TheatreFaction.Player && t.Terrain == TerrainType.Open && !harness.HasActiveSite(t.Coordinate));
                Expect(labHex != null, "There should be an empty open Player hex to build a Lab on");
                harness.SelectTile(labHex);

                Expect(!harness.CanDesignPlanAtSelectedTile(), "No Lab exists yet — should not be able to design a plan");
                Expect(harness.TryBeginConstructionOnSelectedTile(SiteType.Lab, labOption.TurnsToBuild), "Should be able to start building a Lab here");

                for (int i = 0; i < labOption.TurnsToBuild; i++)
                    harness.AdvanceTurn();

                harness.SelectTile(labHex); // re-select: AdvanceTurn doesn't change SelectedTile, but be explicit
                Expect(harness.CanDesignPlanAtSelectedTile(), "Lab should be operational and selectable for plan design after enough turns");

                // --- Design plans ---
                Expect(!harness.TryCreatePlan("", UnitCategory.Quadcopter, out _), "Empty plan name should be rejected");
                Expect(harness.TryCreatePlan("Falcon", UnitCategory.Quadcopter, out string err1), $"Valid plan should be accepted, got error: {err1}");
                Expect(harness.PlayerPlans.Count == 1, $"Should have 1 designed plan, got {harness.PlayerPlans.Count}");
                Expect(!harness.TryCreatePlan("falcon", UnitCategory.Missile, out string err2), "Duplicate name (case-insensitive) should be rejected");
                Expect(err2 != null, "Duplicate-name rejection should include an error message");

                GameObject showcase = GameObject.Find("PlanShowcase");
                Expect(showcase != null, "Selecting the Lab with designed plans should build a PlanShowcase");
                Expect(showcase != null && showcase.transform.childCount == 1, $"Showcase should have one preview slot per plan, got {showcase?.transform.childCount}");

                DronePlan falcon = harness.PlayerPlans[0];

                // --- Queue production at the seeded Player factory ---
                Site playerFactory = harness.World.Sites.First(s => s.Owner == TheatreFaction.Player && s.Type == SiteType.Factory);
                HexTile factoryHex = harness.World.Grid.GetTile(playerFactory.Location);
                harness.SelectTile(factoryHex);

                Expect(harness.SelectedFactory() == playerFactory, "Selecting the Player factory's hex should resolve it via SelectedFactory()");
                Expect(harness.TryQueueProduction(falcon), "Should be able to queue the Falcon for production");

                var queue = harness.ProductionQueueAt(playerFactory);
                Expect(queue.Count == 1, $"Factory should have 1 queued order, got {queue.Count}");
                int expectedTurns = TheatreMapHarness.TurnsToProduce(UnitCategory.Quadcopter);
                Expect(queue[0].TurnsRemaining == expectedTurns, $"Queued order should need {expectedTurns} turns, got {queue[0].TurnsRemaining}");

                for (int i = 0; i < expectedTurns - 1; i++)
                    harness.AdvanceTurn();
                Expect(harness.ProductionQueueAt(playerFactory).Count == 1, "Order should still be in progress before its last turn");
                Expect(!harness.PlayerInventory.ContainsKey(falcon), "Inventory should not have the plan yet before production completes");

                harness.AdvanceTurn();
                Expect(harness.ProductionQueueAt(playerFactory).Count == 0, "Order should be removed from the queue once complete");
                Expect(harness.PlayerInventory.TryGetValue(falcon, out int producedCount) && producedCount == 1,
                    $"Inventory should have 1 Falcon after production completes, got {(harness.PlayerInventory.TryGetValue(falcon, out int c) ? c : -1)}");

                // Selecting a non-factory tile should not resolve a factory.
                harness.SelectTile(labHex);
                Expect(harness.SelectedFactory() == null, "A Lab hex should not resolve as a factory");
                Expect(!harness.TryQueueProduction(falcon), "Queuing production without a selected factory should fail");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(harnessGo);
            }
        }

        private static void TestTheatreMapHarnessSaveLoadRoundTrip()
        {
            var harnessGo = new GameObject("SmokeTest_TheatreMapHarness_SaveLoad");
            try
            {
                var harness = harnessGo.AddComponent<TheatreMapHarness>();
                harness.Build();

                // --- Build up some state worth round-tripping ---
                HexTile capturedHex = harness.World.Grid.Tiles.First(t => t.Owner == TheatreFaction.Enemy &&
                    harness.World.Grid.NeighborsOf(t.Coordinate).Any(n => n.Owner == TheatreFaction.Player));
                harness.SelectTile(capturedHex);
                Expect(harness.TryCaptureSelectedTile(), "Setup: capturing a front-line hex should succeed");
                HexCoordinate capturedCoord = capturedHex.Coordinate;

                HexTile labHex = harness.World.Grid.Tiles.First(t =>
                    t.Owner == TheatreFaction.Player && t.Terrain == TerrainType.Open && !harness.HasActiveSite(t.Coordinate));
                HexCoordinate labCoord = labHex.Coordinate;
                harness.SelectTile(labHex);
                Expect(harness.TryBeginConstructionOnSelectedTile(SiteType.Lab, 1), "Setup: should be able to start building a Lab");
                harness.AdvanceTurn();
                harness.SelectTile(labHex);
                Expect(harness.CanDesignPlanAtSelectedTile(), "Setup: Lab should be operational");
                Expect(harness.TryCreatePlan("Falcon", UnitCategory.Quadcopter, out _), "Setup: designing 'Falcon' should succeed");

                Site playerFactory = harness.World.Sites.First(s => s.Owner == TheatreFaction.Player && s.Type == SiteType.Factory);
                harness.SelectTile(harness.World.Grid.GetTile(playerFactory.Location));
                DronePlan falcon = harness.PlayerPlans[0];
                Expect(harness.TryQueueProduction(falcon), "Setup: queuing Falcon production should succeed");
                int turns = TheatreMapHarness.TurnsToProduce(UnitCategory.Quadcopter);
                for (int i = 0; i < turns; i++)
                    harness.AdvanceTurn();
                Expect(harness.PlayerInventory.TryGetValue(falcon, out int producedBefore) && producedBefore == 1, "Setup: Falcon production should have completed");

                // Queue a second order that will NOT complete before saving, so the
                // in-progress queue itself (not just completed inventory) round-trips.
                Expect(harness.TryQueueProduction(falcon), "Setup: queuing a second Falcon should succeed");

                int turnBeforeSave = harness.Controller.CurrentTurn;
                int playerResourceBeforeSave = harness.Controller.ResourcePool[TheatreFaction.Player];
                int enemyResourceBeforeSave = harness.Controller.ResourcePool[TheatreFaction.Enemy];
                Expect(turnBeforeSave > 0, "Setup: some turns should have passed by now");
                Expect(playerResourceBeforeSave > 0, "Setup: the Player factory should have produced some resources by now");

                // --- Save, reset to a brand new game, then load back ---
                harness.SaveGame();
                harness.NewGame();

                Expect(harness.World.Grid.GetTile(capturedCoord).Owner == TheatreFaction.Enemy, "New game should reset the captured hex back to its default owner");
                Expect(harness.PlayerPlans.Count == 0, "New game should clear designed plans");
                Expect(harness.Controller.CurrentTurn == 0, "New game should reset the turn counter");

                Expect(harness.LoadGame(), "LoadGame should succeed since a save was just written");

                Expect(harness.World.Grid.GetTile(capturedCoord).Owner == TheatreFaction.Player, "Loaded game should restore the captured hex's ownership");

                Site loadedLab = harness.World.Sites.FirstOrDefault(s => s.Location.Equals(labCoord) && s.Type == SiteType.Lab);
                Expect(loadedLab != null, "Loaded game should restore the built Lab");
                Expect(loadedLab != null && loadedLab.IsOperational, "Loaded Lab should be restored as Operational");

                Expect(harness.PlayerPlans.Count == 1 && harness.PlayerPlans[0].Name == "Falcon", "Loaded game should restore the designed plan");
                Expect(harness.PlayerPlans[0].Category == UnitCategory.Quadcopter, "Loaded plan should restore its category");

                DronePlan loadedFalcon = harness.PlayerPlans[0];
                Expect(harness.PlayerInventory.TryGetValue(loadedFalcon, out int producedAfter) && producedAfter == 1,
                    "Loaded game should restore the produced inventory count");

                Site loadedFactory = harness.World.Sites.FirstOrDefault(s => s.Owner == TheatreFaction.Player && s.Type == SiteType.Factory);
                Expect(loadedFactory != null && loadedFactory.IsOperational, "Loaded game should restore the seeded Player factory");

                Expect(harness.Controller.CurrentTurn == turnBeforeSave, $"Loaded game should restore the turn counter (expected {turnBeforeSave}, got {harness.Controller.CurrentTurn})");
                Expect(harness.Controller.Result == TheatreResult.InProgress, "Loaded game should restore the result");
                Expect(harness.Controller.ResourcePool[TheatreFaction.Player] == playerResourceBeforeSave, "Loaded game should restore the Player resource pool");
                Expect(harness.Controller.ResourcePool[TheatreFaction.Enemy] == enemyResourceBeforeSave, "Loaded game should restore the Enemy resource pool");

                var loadedQueue = harness.ProductionQueueAt(loadedFactory);
                Expect(loadedQueue.Count == 1, $"Loaded game should restore the still-in-progress production order, got {loadedQueue.Count}");
                Expect(loadedQueue.Count == 1 && loadedQueue[0].Plan.Name == "Falcon" && loadedQueue[0].TurnsRemaining == TheatreMapHarness.TurnsToProduce(UnitCategory.Quadcopter),
                    "Loaded production order should restore the correct plan and full remaining turns (it hadn't ticked at all before saving)");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(harnessGo);
                SaveSystem.DeleteSave(); // don't leave a stray save file behind for a human tester to trip over
            }
        }

        private static void TestTheatreMapHarnessPointerOverUIDoesNotThrow()
        {
            var harnessGo = new GameObject("SmokeTest_TheatreMapHarness_PointerOverUI");
            try
            {
                var harness = harnessGo.AddComponent<TheatreMapHarness>();
                harness.Build();

                // Regression coverage for "focus goes when I click build": the pointer-
                // over-UI check must exist and be callable (wired into both the
                // harness's own click handling and the camera controller), and must
                // reflect the menu's open/closed state via the delegate chain.
                bool overUiWhenClosed = harness.IsPointerOverUI();
                Expect(!overUiWhenClosed, "At the harness's own HUD-panel-relative default mouse position in a headless run, IsPointerOverUI should be false (menu closed, and (0,0) is outside the corner panel)");

                var cameraController = harnessGo.GetComponentInChildren<TheatreMapCameraController>();
                Expect(cameraController != null && cameraController.isPointerOverUI != null,
                    "Camera controller should have its isPointerOverUI delegate wired up by the harness");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(harnessGo);
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
