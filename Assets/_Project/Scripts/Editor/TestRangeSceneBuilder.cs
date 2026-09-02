using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vanquish.Combat;
using Vanquish.Core;
using Vanquish.Data.Drones;
using Vanquish.Data.Missiles;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Phase 2G: builds the Workshop's "Test Range" scene — the player's real design
    /// (via CombatPlayerLoadoutApplier, exactly like a real Combat scene) against one
    /// stationary and one simple-moving unarmed dummy target, no CombatManager at all
    /// (no win/lose, no currency — purely observational, per this sub-milestone's own
    /// scope). Reuses Phase1CombatSceneBuilder's ground/light/camera/HUD/loadout
    /// helpers and VehicleFactory the same way every other combat-scene builder does —
    /// this genuinely was "the cheapest sub-milestone to build" once 2A/2B/2D/2E
    /// existed, exactly as PLAN.md's own technical note predicted.
    /// </summary>
    public static class TestRangeSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/TestRange.unity";

        [MenuItem("Vanquish/Phase 2G/Build Test Range Scene")]
        public static void BuildScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[TestRangeSceneBuilder] Cannot rebuild while in Play mode.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            MissileLoadout missileLoadout = Phase1CombatSceneBuilder.LoadMissileLoadout();
            DroneLoadout strikeLoadout = Phase1CombatSceneBuilder.LoadStrikeDroneLoadout(missileLoadout);
            DroneLoadout scoutLoadout = Phase1CombatSceneBuilder.LoadScoutDroneLoadout();

            if (missileLoadout == null || strikeLoadout == null || scoutLoadout == null)
            {
                Debug.LogError("[TestRangeSceneBuilder] Missing seeded Tier-0 data — run Vanquish/Phase 1/Seed Tier-0 Data first.");
                return;
            }

            Phase1CombatSceneBuilder.BuildGround();
            Phase1CombatSceneBuilder.BuildLight();

            var teamAwarenessGo = new GameObject("TeamAwareness");
            teamAwarenessGo.AddComponent<TeamAwareness>();

            // "Player_Drone" — this edit-time-baked Tier-0 loadout is only ever seen if
            // the scene is opened directly (e.g. a headless regression run); the real
            // Test Range flow always overrides it with the player's actual
            // currently-configured design via CombatPlayerLoadoutApplier below,
            // exactly like every real Combat scene.
            GameObject player = VehicleFactory.SpawnDrone(strikeLoadout, new Vector3(0f, 5f, -150f), Quaternion.identity, Team.Player);
            player.name = "Player_Drone";
            var playerController = player.AddComponent<PlayerDroneController>();

            var applierGo = new GameObject("TestRangeLoadoutApplier");
            // CombatPlayerLoadoutApplier has no dependency on CombatManager existing —
            // it only needs to find "Player_Drone"/"Scout_Drone" by name — so it's
            // reusable here verbatim despite the Test Range never spawning a
            // CombatManager at all.
            applierGo.AddComponent<CombatPlayerLoadoutApplier>();

            // Stationary dummy: an unarmed drone with no AI component attached at all —
            // VehicleFactory.SpawnDrone gives it a collider/DetectableSignature/
            // DetectionSensor/Health/visual like any real unit, but with zero steering
            // force ever applied, so it simply sits still (electric multirotor
            // propulsion has no constant thrust, unlike fixed-wing/jet).
            GameObject stationaryDummy = VehicleFactory.SpawnDrone(scoutLoadout, new Vector3(0f, 5f, 150f), Quaternion.identity, Team.Enemy);
            stationaryDummy.name = "Dummy_Stationary";

            // Simple-moving dummy: same unarmed loadout, plus ScoutPatrol for slow
            // wandering movement — identical in spirit to CombatTestSceneBuilder's
            // ScoutPatrolOnly archetype (Phase 2D), reused here for its own purpose.
            GameObject movingDummy = VehicleFactory.SpawnDrone(scoutLoadout, new Vector3(60f, 5f, 200f), Quaternion.identity, Team.Enemy);
            movingDummy.name = "Dummy_Moving";
            var patrol = movingDummy.AddComponent<ScoutPatrol>();
            patrol.arenaCenter = new Vector3(60f, 5f, 200f);
            patrol.patrolRadius = 80f;

            var telemetryGo = new GameObject("TestRangeTelemetry");
            var telemetry = telemetryGo.AddComponent<TestRangeTelemetry>();
            telemetry.player = player.transform;
            telemetry.workshopSceneName = "Workshop";

            Phase1CombatSceneBuilder.BuildCamera(player.transform);
            Phase1CombatSceneBuilder.BuildHud(player, playerController);

            System.IO.Directory.CreateDirectory("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Phase1CombatSceneBuilder.EnsureSceneInBuildSettings(ScenePath);

            Debug.Log($"[TestRangeSceneBuilder] Test Range scene built and saved to {ScenePath}");
        }

        [MenuItem("Vanquish/Phase 2G/Run Headless Test On Test Range")]
        public static void RunHeadlessTest()
        {
            Phase1BatchRunner.RunHeadlessTest(ScenePath);
        }
    }
}
