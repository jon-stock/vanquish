using System;
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
    /// <summary>Which archetype/control component to attach to a spawned test enemy.</summary>
    public enum TestArchetype
    {
        Interceptor,
        ScoutHunter,
        /// <summary>Non-combatant: patrols only, never fires — a "bait" scout target for
        /// exercising Scout-hunter/TeamAwareness without a full symmetrical fight.</summary>
        ScoutPatrolOnly,
        /// <summary>Static base defense (SamSiteAI) — not a drone at all, spawned via
        /// InstallationFactory instead of VehicleFactory. See SpawnEnemyRoster's
        /// handling of this case for why it can't go through the same code path as
        /// the drone-based archetypes above.</summary>
        SamSite,
    }

    /// <summary>One row of enemy composition: spawn `count` drones of this archetype,
    /// each either armed (strike loadout + weapon) or unarmed (scout loadout, no weapon).</summary>
    [Serializable]
    public class EnemySpawnGroup
    {
        public TestArchetype archetype = TestArchetype.Interceptor;
        public bool armed = true;
        public int count = 1;

        [Tooltip("Seconds between shots for this group's WeaponController, if armed. Only " +
            "applies when armed=true — VehicleFactory.SpawnDrone doesn't attach a " +
            "WeaponController to unarmed (scout) loadouts at all. Overrides " +
            "WeaponController's own default (2.5s) post-spawn, for testing how a " +
            "faster/slower-firing enemy plays without touching any loadout/part data.")]
        public float fireCooldownSeconds = 2.5f;
    }

    /// <summary>
    /// Phase 2D dev-testing infrastructure: builds a combat scene with an arbitrary,
    /// caller-specified enemy roster (any mix/count of the archetypes in Scripts/AI/),
    /// instead of Phase1CombatSceneBuilder's single hardcoded Interceptor. This exists
    /// specifically so new AI archetypes (and future ones — SAM site, etc.) have
    /// somewhere to be exercised live/visually as they're added, without hand-editing
    /// scene-building code per feature or permanently changing the fixed Phase 1 MVP
    /// arena (Combat_Arena01.unity, which Phase1BatchRunner's regression targets and
    /// should stay stable). Writes to a separate scene file by default so the two never
    /// collide. Reuses Phase1CombatSceneBuilder's loadout-loading and scene-boilerplate
    /// helpers (ground/light/camera/HUD, player+scout spawn) rather than duplicating
    /// them — only enemy composition differs.
    ///
    /// See CombatTestSceneBuilderWindow for the interactive menu (archetype/count
    /// picker) this is built to serve; the [MenuItem] below is a fixed-composition
    /// convenience for headless/-executeMethod regression runs.
    /// </summary>
    public static class CombatTestSceneBuilder
    {
        public const string DefaultTestScenePath = "Assets/_Project/Scenes/Combat_TestArena.unity";

        /// <summary>
        /// Demonstrates 2D's own exit criteria directly: "a single battle can contain
        /// an interceptor, a scout-hunter, and a SAM site simultaneously, each behaving
        /// visibly differently" — plus one unarmed bait scout so Scout-hunter actually
        /// has a scout to specifically go after instead of only ever falling back to
        /// nearest-any.
        /// </summary>
        [MenuItem("Vanquish/Phase 2D/Build Default Multi-Archetype Test Scene (Headless)")]
        public static void BuildDefaultMultiArchetypeTestScene()
        {
            BuildScene(new List<EnemySpawnGroup>
            {
                new EnemySpawnGroup { archetype = TestArchetype.Interceptor, armed = true, count = 1 },
                new EnemySpawnGroup { archetype = TestArchetype.ScoutHunter, armed = true, count = 1 },
                new EnemySpawnGroup { archetype = TestArchetype.ScoutPatrolOnly, armed = false, count = 1 },
                // fireCooldownSeconds matches BaseDefense_SamSite_Basic's own
                // rateOfFirePerSecond (1/s) explicitly — otherwise EnemySpawnGroup's
                // generic 2.5s default would mask this archetype's own "high rate of
                // fire" flavor in this specific demo composition.
                new EnemySpawnGroup { archetype = TestArchetype.SamSite, count = 1, fireCooldownSeconds = 1f },
            });
        }

        [MenuItem("Vanquish/Phase 2D/Run Headless Test On Default Multi-Archetype Scene")]
        public static void RunHeadlessTestOnDefaultMultiArchetypeScene()
        {
            Phase1BatchRunner.RunHeadlessTest(DefaultTestScenePath);
        }

        /// <summary>
        /// Builds (and saves) a combat test scene: same ground/lighting/camera/HUD/
        /// player/friendly-scout setup as Phase1CombatSceneBuilder, but the enemy team
        /// is spawned from `enemyGroups` instead of one hardcoded Interceptor.
        /// </summary>
        public static void BuildScene(List<EnemySpawnGroup> enemyGroups, string scenePath = DefaultTestScenePath)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[CombatTestSceneBuilder] Cannot rebuild while in Play mode.");
                return;
            }

            if (enemyGroups == null || enemyGroups.Count == 0)
            {
                Debug.LogError("[CombatTestSceneBuilder] No enemy groups specified — nothing to build.");
                return;
            }

            // Same asset-lifetime-vs-domain-teardown ordering caveat as
            // Phase1CombatSceneBuilder.BuildScene: create the scene before loading part
            // assets, not after.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            MissileLoadout missileLoadout = Phase1CombatSceneBuilder.LoadMissileLoadout();
            DroneLoadout strikeLoadout = Phase1CombatSceneBuilder.LoadStrikeDroneLoadout(missileLoadout);
            DroneLoadout scoutLoadout = Phase1CombatSceneBuilder.LoadScoutDroneLoadout();

            if (missileLoadout == null || strikeLoadout == null || scoutLoadout == null)
            {
                Debug.LogError("[CombatTestSceneBuilder] Missing seeded data assets — run Vanquish/Phase 1/Seed Tier-0 Data first.");
                return;
            }

            Phase1CombatSceneBuilder.BuildGround();
            Phase1CombatSceneBuilder.BuildLight();

            var combatManagerGo = new GameObject("CombatManager");
            combatManagerGo.AddComponent<CombatManager>();
            combatManagerGo.AddComponent<CombatPlayerLoadoutApplier>();

            var teamAwarenessGo = new GameObject("TeamAwareness");
            teamAwarenessGo.AddComponent<TeamAwareness>();

            Vector3 arenaCenter = new Vector3(0f, 5f, 0f);

            GameObject player = VehicleFactory.SpawnDrone(strikeLoadout, new Vector3(0f, 5f, -200f), Quaternion.identity, Team.Player);
            player.name = "Player_Drone";
            var playerController = player.AddComponent<PlayerDroneController>();

            GameObject scout = VehicleFactory.SpawnDrone(scoutLoadout, new Vector3(30f, 5f, -190f), Quaternion.identity, Team.Player);
            scout.name = "Scout_Drone";
            var scoutPatrol = scout.AddComponent<ScoutPatrol>();
            scoutPatrol.arenaCenter = arenaCenter;
            scoutPatrol.patrolRadius = 250f;

            SpawnEnemyRoster(enemyGroups, strikeLoadout, scoutLoadout, arenaCenter);

            Phase1CombatSceneBuilder.BuildCamera(player.transform);
            Phase1CombatSceneBuilder.BuildHud(player, playerController);

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(scenePath));
            EditorSceneManager.SaveScene(scene, scenePath);

            int totalEnemies = enemyGroups.Count > 0 ? SumCounts(enemyGroups) : 0;
            string composition = string.Join(", ", enemyGroups.ConvertAll(DescribeGroup));
            Debug.Log($"[CombatTestSceneBuilder] Scene built with {totalEnemies} enemies ({composition}) and saved to {scenePath}");
        }

        private static string DescribeGroup(EnemySpawnGroup g)
        {
            // SamSite ignores the armed toggle entirely (see SpawnEnemyRoster) — it's
            // always armed via its BaseDefenseDefinition, so describe it consistently
            // regardless of whatever the window's (irrelevant, for this archetype) toggle is set to.
            if (g.archetype == TestArchetype.SamSite)
                return $"{g.count}x {g.archetype}({g.fireCooldownSeconds:F1}s/shot)";

            return g.armed
                ? $"{g.count}x {g.archetype}(armed, {g.fireCooldownSeconds:F1}s/shot)"
                : $"{g.count}x {g.archetype}(unarmed)";
        }

        private static int SumCounts(List<EnemySpawnGroup> groups)
        {
            int total = 0;
            foreach (var group in groups)
                total += Mathf.Max(0, group.count);
            return total;
        }

        /// <summary>
        /// Spawns every requested enemy, spread along the far edge of the arena facing
        /// the player so multiple groups/counts don't all stack on the same spawn point.
        /// </summary>
        private static void SpawnEnemyRoster(List<EnemySpawnGroup> enemyGroups, DroneLoadout strikeLoadout,
            DroneLoadout scoutLoadout, Vector3 arenaCenter)
        {
            const float spacingMeters = 40f;
            const float spawnZ = 200f;
            const float patrolRadius = 250f;

            int totalCount = SumCounts(enemyGroups);
            if (totalCount == 0)
                return;

            float startX = -(totalCount - 1) * spacingMeters * 0.5f;
            int slotIndex = 0;

            // Loaded lazily (only if a SamSite group is actually requested) rather than
            // unconditionally, so scenes with no SAM site in the roster don't fail just
            // because BaseDefense_SamSite_Basic hasn't been seeded yet.
            BaseDefenseDefinition samSiteDefinition = null;

            foreach (var group in enemyGroups)
            {
                for (int i = 0; i < group.count; i++)
                {
                    Vector3 position = new Vector3(startX + slotIndex * spacingMeters, 5f, spawnZ);

                    GameObject enemy;
                    if (group.archetype == TestArchetype.SamSite)
                    {
                        // Not a drone at all — InstallationFactory, not VehicleFactory,
                        // per this archetype's own PLAN.md instruction. No orientation/
                        // patrol point needed since it never moves.
                        samSiteDefinition ??= Phase1CombatSceneBuilder.Load<BaseDefenseDefinition>(
                            "Assets/_Project/Data/Support/BaseDefense_SamSite_Basic.asset");
                        if (samSiteDefinition == null)
                        {
                            Debug.LogError("[CombatTestSceneBuilder] Missing BaseDefense_SamSite_Basic — " +
                                "run Vanquish/Phase 2D/Seed SAM Site Definition first. Skipping this SAM site slot.");
                            slotIndex++;
                            continue;
                        }

                        enemy = InstallationFactory.SpawnBaseDefense(samSiteDefinition, position, Quaternion.identity, Team.Enemy);
                        enemy.name = $"Enemy_{group.archetype}_{slotIndex}";

                        var samAI = enemy.AddComponent<SamSiteAI>();
                        samAI.engagementRangeMeters = samSiteDefinition.engagementRangeMeters;
                    }
                    else
                    {
                        DroneLoadout loadout = group.armed ? strikeLoadout : scoutLoadout;
                        enemy = VehicleFactory.SpawnDrone(loadout, position, Quaternion.Euler(0f, 180f, 0f), Team.Enemy);
                        enemy.name = $"Enemy_{group.archetype}_{slotIndex}";

                        switch (group.archetype)
                        {
                            case TestArchetype.Interceptor:
                                var interceptor = enemy.AddComponent<InterceptorAI>();
                                interceptor.arenaCenter = arenaCenter;
                                interceptor.patrolRadius = patrolRadius;
                                break;
                            case TestArchetype.ScoutHunter:
                                var scoutHunter = enemy.AddComponent<ScoutHunterAI>();
                                scoutHunter.arenaCenter = arenaCenter;
                                scoutHunter.patrolRadius = patrolRadius;
                                break;
                            case TestArchetype.ScoutPatrolOnly:
                                var patrol = enemy.AddComponent<ScoutPatrol>();
                                patrol.arenaCenter = arenaCenter;
                                patrol.patrolRadius = patrolRadius;
                                break;
                        }
                    }

                    // Only present when armed/a SAM site (see VehicleFactory.SpawnDrone
                    // and InstallationFactory.SpawnBaseDefense) — override the default
                    // fire rate with whatever this group's test config asks for.
                    var weapon = enemy.GetComponent<WeaponController>();
                    if (weapon != null)
                        weapon.fireCooldownSeconds = Mathf.Max(0.05f, group.fireCooldownSeconds);

                    slotIndex++;
                }
            }
        }
    }
}
