using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vanquish.AI;
using Vanquish.Combat;
using Vanquish.Core;
using Vanquish.Data.Drones;
using Vanquish.Data.Missiles;
using Vanquish.Data.Support;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Phase 2E: two additional arena layouts beyond the flat Phase 1 MVP arena
    /// (Combat_Arena01.unity, left unchanged/untouched for stability — the existing
    /// Phase1BatchRunner regression and the "Core First" MVP loop both depend on it),
    /// each built with TerrainArenaBuilder's procedural heightmap terrain instead of a
    /// flat primitive plane, and each demonstrating a genuinely different tactical
    /// shape/engagement distance/objective per this sub-milestone's own goal.
    /// Reuses Phase1CombatSceneBuilder's loadout-loading/light/camera/HUD helpers
    /// (already `internal static` for exactly this kind of reuse — see
    /// CombatTestSceneBuilder for the prior precedent) rather than duplicating them.
    /// </summary>
    public static class Phase2EArenaBuilder
    {
        private const string ValleyScenePath = "Assets/_Project/Scenes/Combat_Arena_Valley.unity";
        private const string PlateauScenePath = "Assets/_Project/Scenes/Combat_Arena_Plateau.unity";

        /// <summary>
        /// Long valley: player and a defending Interceptor/SAM site are ~1000m apart
        /// down the valley floor (much longer engagement distance than the flat MVP
        /// arena's ~400m), with the valley walls acting as terrain cover from anything
        /// off the direct line. Non-skirmish objective: destroy the SAM site
        /// specifically, not every enemy unit — demonstrates CombatManager's new
        /// pluggable DestroyTarget objective.
        /// </summary>
        [MenuItem("Vanquish/Phase 2E/Build Valley Arena (Destroy SAM Site)")]
        public static void BuildValleyArena()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Phase2EArenaBuilder] Cannot rebuild while in Play mode.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            MissileLoadout missileLoadout = Phase1CombatSceneBuilder.LoadMissileLoadout();
            DroneLoadout strikeLoadout = Phase1CombatSceneBuilder.LoadStrikeDroneLoadout(missileLoadout);
            DroneLoadout scoutLoadout = Phase1CombatSceneBuilder.LoadScoutDroneLoadout();
            BaseDefenseDefinition samSiteDefinition = Phase1CombatSceneBuilder.Load<BaseDefenseDefinition>(
                "Assets/_Project/Data/Support/BaseDefense_SamSite_Basic.asset");

            if (missileLoadout == null || strikeLoadout == null || scoutLoadout == null)
            {
                Debug.LogError("[Phase2EArenaBuilder] Missing seeded Tier-0 data — run Vanquish/Phase 1/Seed Tier-0 Data first.");
                return;
            }
            if (samSiteDefinition == null)
            {
                Debug.LogError("[Phase2EArenaBuilder] Missing BaseDefense_SamSite_Basic — run Vanquish/Phase 2D/Seed SAM Site Definition first.");
                return;
            }

            const float width = 800f;
            const float depth = 1200f;
            const float maxHeight = 80f;
            Terrain terrain = TerrainArenaBuilder.BuildTerrain("Terrain_Valley", width, maxHeight, depth,
                TerrainArenaBuilder.ValleyHeight, new Color(0.45f, 0.38f, 0.28f));

            Phase1CombatSceneBuilder.BuildLight();

            var combatManagerGo = new GameObject("CombatManager");
            var combatManager = combatManagerGo.AddComponent<CombatManager>();
            combatManagerGo.AddComponent<CombatPlayerLoadoutApplier>();

            var teamAwarenessGo = new GameObject("TeamAwareness");
            teamAwarenessGo.AddComponent<TeamAwareness>();

            const float clearanceMeters = 50f; // altitude above terrain surface units spawn at, since AI has no ground-avoidance
            Vector3 playerSpawnXZ = new Vector3(0f, 0f, -500f);
            Vector3 playerSpawnPos = AboveTerrain(terrain, playerSpawnXZ, clearanceMeters);

            GameObject player = VehicleFactory.SpawnDrone(strikeLoadout, playerSpawnPos, Quaternion.identity, Team.Player);
            player.name = "Player_Drone"; // matches CombatPlayerLoadoutApplier's expected name, same as every other arena
            var playerController = player.AddComponent<PlayerDroneController>();

            Vector3 scoutSpawnPos = playerSpawnPos + new Vector3(30f, 0f, 10f);
            GameObject scout = VehicleFactory.SpawnDrone(scoutLoadout, scoutSpawnPos, Quaternion.identity, Team.Player);
            scout.name = "Scout_Drone";
            var scoutPatrol = scout.AddComponent<ScoutPatrol>();
            scoutPatrol.arenaCenter = playerSpawnPos;
            scoutPatrol.patrolRadius = 200f;

            Vector3 samSpawnXZ = new Vector3(0f, 0f, 500f);
            Vector3 samSpawnPos = AboveTerrain(terrain, samSpawnXZ, 0f); // sits on the ground, not floating — it's static
            GameObject samSite = InstallationFactory.SpawnBaseDefense(samSiteDefinition, samSpawnPos, Quaternion.identity, Team.Enemy);
            samSite.name = "Enemy_SamSite_Objective";
            var samAI = samSite.AddComponent<SamSiteAI>();
            samAI.engagementRangeMeters = samSiteDefinition.engagementRangeMeters;

            // A defending Interceptor near the SAM site, so the valley isn't purely
            // "fly in a straight line and shoot the static target" — it also
            // demonstrates 2D's Interceptor archetype in a non-flat, longer-range setting.
            Vector3 interceptorSpawnPos = samSpawnPos + new Vector3(60f, 40f, -80f);
            GameObject interceptor = VehicleFactory.SpawnDrone(strikeLoadout, interceptorSpawnPos, Quaternion.Euler(0f, 180f, 0f), Team.Enemy);
            interceptor.name = "Enemy_Interceptor_Guard";
            var interceptorAI = interceptor.AddComponent<InterceptorAI>();
            interceptorAI.arenaCenter = samSpawnPos;
            interceptorAI.patrolRadius = 200f;

            combatManager.objectiveType = ObjectiveType.DestroyTarget;
            combatManager.objectiveTarget = samSite;
            combatManager.objectiveTargetDescription = "Destroy the enemy SAM site guarding the valley.";

            Phase1CombatSceneBuilder.BuildCamera(player.transform);
            Phase1CombatSceneBuilder.BuildHud(player, playerController);

            SpawnCoverRocks(terrain, new[]
            {
                new Vector3(-150f, 0f, -200f), new Vector3(180f, 0f, -100f),
                new Vector3(-120f, 0f, 150f), new Vector3(140f, 0f, 250f),
            });

            System.IO.Directory.CreateDirectory("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, ValleyScenePath);
            Phase1CombatSceneBuilder.EnsureSceneInBuildSettings(ValleyScenePath);

            Debug.Log($"[Phase2EArenaBuilder] Valley arena built and saved to {ValleyScenePath}");
        }

        /// <summary>
        /// Raised plateau with steep cliff edges: 2 enemies (Interceptor + Scout-hunter)
        /// at a shorter, closer engagement distance than the flat MVP arena, with the
        /// cliffs themselves blocking sightlines around the plateau's edges — a
        /// different tactical shape from the valley's long, open sightline. Objective
        /// stays the default DestroyAllEnemies (not every scenario needs a special
        /// objective — this one's variation is purely terrain/engagement-distance).
        /// </summary>
        [MenuItem("Vanquish/Phase 2E/Build Plateau Arena (Destroy All Enemies)")]
        public static void BuildPlateauArena()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Phase2EArenaBuilder] Cannot rebuild while in Play mode.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            MissileLoadout missileLoadout = Phase1CombatSceneBuilder.LoadMissileLoadout();
            DroneLoadout strikeLoadout = Phase1CombatSceneBuilder.LoadStrikeDroneLoadout(missileLoadout);
            DroneLoadout scoutLoadout = Phase1CombatSceneBuilder.LoadScoutDroneLoadout();

            if (missileLoadout == null || strikeLoadout == null || scoutLoadout == null)
            {
                Debug.LogError("[Phase2EArenaBuilder] Missing seeded Tier-0 data — run Vanquish/Phase 1/Seed Tier-0 Data first.");
                return;
            }

            const float width = 600f;
            const float depth = 600f;
            const float maxHeight = 50f;
            Terrain terrain = TerrainArenaBuilder.BuildTerrain("Terrain_Plateau", width, maxHeight, depth,
                TerrainArenaBuilder.PlateauHeight, new Color(0.4f, 0.42f, 0.38f));

            Phase1CombatSceneBuilder.BuildLight();

            var combatManagerGo = new GameObject("CombatManager");
            combatManagerGo.AddComponent<CombatManager>(); // default ObjectiveType.DestroyAllEnemies — no override needed
            combatManagerGo.AddComponent<CombatPlayerLoadoutApplier>();

            var teamAwarenessGo = new GameObject("TeamAwareness");
            teamAwarenessGo.AddComponent<TeamAwareness>();

            const float clearanceMeters = 40f;
            Vector3 arenaCenterXZ = Vector3.zero;
            Vector3 arenaCenter = AboveTerrain(terrain, arenaCenterXZ, clearanceMeters);

            Vector3 playerSpawnPos = AboveTerrain(terrain, new Vector3(0f, 0f, -120f), clearanceMeters);
            GameObject player = VehicleFactory.SpawnDrone(strikeLoadout, playerSpawnPos, Quaternion.identity, Team.Player);
            player.name = "Player_Drone";
            var playerController = player.AddComponent<PlayerDroneController>();

            Vector3 scoutSpawnPos = playerSpawnPos + new Vector3(25f, 0f, 5f);
            GameObject scout = VehicleFactory.SpawnDrone(scoutLoadout, scoutSpawnPos, Quaternion.identity, Team.Player);
            scout.name = "Scout_Drone";
            var scoutPatrol = scout.AddComponent<ScoutPatrol>();
            scoutPatrol.arenaCenter = arenaCenter;
            scoutPatrol.patrolRadius = 150f;

            Vector3 interceptorSpawnPos = AboveTerrain(terrain, new Vector3(60f, 0f, 120f), clearanceMeters);
            GameObject interceptor = VehicleFactory.SpawnDrone(strikeLoadout, interceptorSpawnPos, Quaternion.Euler(0f, 180f, 0f), Team.Enemy);
            interceptor.name = "Enemy_Interceptor";
            var interceptorAI = interceptor.AddComponent<InterceptorAI>();
            interceptorAI.arenaCenter = arenaCenter;
            interceptorAI.patrolRadius = 150f; // tighter than the MVP arena's 250f — a deliberately closer-range skirmish

            Vector3 scoutHunterSpawnPos = AboveTerrain(terrain, new Vector3(-70f, 0f, 110f), clearanceMeters);
            GameObject scoutHunter = VehicleFactory.SpawnDrone(scoutLoadout, scoutHunterSpawnPos, Quaternion.Euler(0f, 180f, 0f), Team.Enemy);
            scoutHunter.name = "Enemy_ScoutHunter";
            var scoutHunterAI = scoutHunter.AddComponent<ScoutHunterAI>();
            scoutHunterAI.arenaCenter = arenaCenter;
            scoutHunterAI.patrolRadius = 150f;
            // scoutLoadout has no missileLoadout (it's the same unarmed sensor-focused
            // design the friendly scout uses), so VehicleFactory.SpawnDrone never adds
            // a WeaponController — add one manually so the enemy team isn't fielding a
            // defenseless unit, without changing this drone's sensor-suite-driven
            // isScout role (which is what ScoutHunterAI on the *other* side would
            // target it for, in a symmetric fight — not relevant to this one-sided
            // scenario, but keeping the loadout itself unmodified is still correct).
            var scoutHunterWeapon = scoutHunter.AddComponent<WeaponController>();
            scoutHunterWeapon.missileLoadout = missileLoadout;
            scoutHunterWeapon.ammoRemaining = 4;
            scoutHunterWeapon.ownerTeam = Team.Enemy;

            Phase1CombatSceneBuilder.BuildCamera(player.transform);
            Phase1CombatSceneBuilder.BuildHud(player, playerController);

            SpawnCoverRocks(terrain, new[]
            {
                new Vector3(-100f, 0f, 0f), new Vector3(100f, 0f, -40f),
                new Vector3(0f, 0f, 80f), new Vector3(-60f, 0f, -100f),
            });

            System.IO.Directory.CreateDirectory("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, PlateauScenePath);
            Phase1CombatSceneBuilder.EnsureSceneInBuildSettings(PlateauScenePath);

            Debug.Log($"[Phase2EArenaBuilder] Plateau arena built and saved to {PlateauScenePath}");
        }

        [MenuItem("Vanquish/Phase 2E/Build All Arenas")]
        public static void BuildAllArenas()
        {
            BuildValleyArena();
            BuildPlateauArena();
        }

        [MenuItem("Vanquish/Phase 2E/Run Headless Test On Valley Arena")]
        public static void RunHeadlessTestOnValleyArena()
        {
            Phase1BatchRunner.RunHeadlessTest(ValleyScenePath);
        }

        [MenuItem("Vanquish/Phase 2E/Run Headless Test On Plateau Arena")]
        public static void RunHeadlessTestOnPlateauArena()
        {
            Phase1BatchRunner.RunHeadlessTest(PlateauScenePath);
        }

        private static Vector3 AboveTerrain(Terrain terrain, Vector3 worldXZ, float clearanceMeters)
        {
            float groundY = TerrainArenaBuilder.SampleWorldHeight(terrain, worldXZ);
            return new Vector3(worldXZ.x, groundY + clearanceMeters, worldXZ.z);
        }

        /// <summary>Cheap terrain-cover obstacles — scaled cube primitives sitting directly
        /// on the generated terrain surface, same "no imported art" convention as every
        /// other visual in this project.</summary>
        private static void SpawnCoverRocks(Terrain terrain, IEnumerable<Vector3> worldXZPositions)
        {
            int index = 0;
            foreach (var xz in worldXZPositions)
            {
                float groundY = TerrainArenaBuilder.SampleWorldHeight(terrain, xz);
                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = $"CoverRock_{index}";
                rock.transform.position = new Vector3(xz.x, groundY + 4f, xz.z);
                rock.transform.rotation = Quaternion.Euler(0f, index * 37f, 0f); // arbitrary per-rock variation, purely visual
                rock.transform.localScale = new Vector3(12f, 8f, 10f);

                var renderer = rock.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    var material = new Material(renderer.sharedMaterial) { color = new Color(0.5f, 0.48f, 0.45f) };
                    renderer.sharedMaterial = material;
                }

                index++;
            }
        }
    }
}
