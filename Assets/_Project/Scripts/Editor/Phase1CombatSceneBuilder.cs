using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vanquish.AI;
using Vanquish.Combat;
using Vanquish.Core;
using Vanquish.Data.Drones;
using Vanquish.Data.Missiles;
using Vanquish.Data.Shared;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Builds the Phase 1 MVP combat arena programmatically (see the headless testing
    /// workflow in docs/CODING_STANDARDS.md): one player drone (manually controlled),
    /// one scout drone (long-range detection, feeds TeamAwareness), one enemy drone
    /// (Phase 2D's Interceptor archetype: patrol → engage), ground, lighting, camera,
    /// HUD, and win/lose tracking via CombatManager. Requires Phase1DataSeeder to have
    /// been run first.
    /// </summary>
    public static class Phase1CombatSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Combat_Arena01.unity";

        [MenuItem("Vanquish/Phase 1/Build Combat Scene")]
        public static void BuildScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Phase1CombatSceneBuilder] Cannot rebuild while in Play mode.");
                return;
            }

            // Create the scene BEFORE loading part assets: EditorSceneManager.NewScene
            // tears down the previous scene and can unload ScriptableObject references
            // that are only held by loose local variables at that point (Unity's native
            // object lifetime tracking is separate from .NET's GC and doesn't see plain
            // C# fields as keeping an asset alive) — loading parts first caused every
            // MissileLoadout field to silently go "fake null" by the time SpawnDrone
            // read them, with no load error ever logged (the load itself succeeded).
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            MissileLoadout missileLoadout = LoadMissileLoadout();
            DroneLoadout strikeLoadout = LoadStrikeDroneLoadout(missileLoadout);
            DroneLoadout scoutLoadout = LoadScoutDroneLoadout();

            if (missileLoadout == null || strikeLoadout == null || scoutLoadout == null)
            {
                Debug.LogError("[Phase1CombatSceneBuilder] Missing seeded data assets — run Vanquish/Phase 1/Seed Tier-0 Data first.");
                return;
            }

            BuildGround();
            BuildLight();

            var combatManagerGo = new GameObject("CombatManager");
            var combatManager = combatManagerGo.AddComponent<CombatManager>();

            // Runs in Awake(), before CombatManager's own Start() scans the scene for
            // units — see CombatPlayerLoadoutApplier's doc comment. Swaps the player/
            // scout drones below for whatever was configured in the Workshop, if
            // anything was (no-ops otherwise, e.g. for headless regression tests that
            // open this scene directly without a PlayerProgress pending loadout).
            combatManagerGo.AddComponent<CombatPlayerLoadoutApplier>();

            var teamAwarenessGo = new GameObject("TeamAwareness");
            teamAwarenessGo.AddComponent<TeamAwareness>();

            GameObject player = VehicleFactory.SpawnDrone(strikeLoadout, new Vector3(0f, 5f, -200f), Quaternion.identity, Team.Player);
            player.name = "Player_Drone";
            var playerController = player.AddComponent<PlayerDroneController>();

            GameObject scout = VehicleFactory.SpawnDrone(scoutLoadout, new Vector3(30f, 5f, -190f), Quaternion.identity, Team.Player);
            scout.name = "Scout_Drone";
            var scoutPatrol = scout.AddComponent<ScoutPatrol>();
            scoutPatrol.arenaCenter = new Vector3(0f, 5f, 0f);
            scoutPatrol.patrolRadius = 250f;

            GameObject enemy = VehicleFactory.SpawnDrone(strikeLoadout, new Vector3(0f, 5f, 200f), Quaternion.Euler(0f, 180f, 0f), Team.Enemy);
            enemy.name = "Enemy_Drone";
            // Phase 2D: the Phase 1 "patrol -> engage" AI is now formalized as the
            // Interceptor archetype (Vanquish.AI.InterceptorAI) rather than "the" enemy AI.
            var interceptor = enemy.AddComponent<InterceptorAI>();
            interceptor.arenaCenter = new Vector3(0f, 5f, 0f);
            interceptor.patrolRadius = 250f;

            BuildCamera(player.transform);
            BuildHud(player, playerController);

            System.IO.Directory.CreateDirectory("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log($"[Phase1CombatSceneBuilder] Scene built and saved to {ScenePath}");
        }

        // internal (not private) below: reused by CombatTestSceneBuilder so the
        // dynamic multi-archetype test scene builder doesn't duplicate loadout-loading
        // and scene-boilerplate code that has nothing to do with enemy composition.
        internal static MissileLoadout LoadMissileLoadout()
        {
            var loadout = new MissileLoadout { designName = "Basic Missile" };
            loadout.airframe = Load<MissileAirframeDefinition>("Assets/_Project/Data/Missiles/Airframe_Basic.asset");
            loadout.engine = Load<MissileEngineDefinition>("Assets/_Project/Data/Missiles/Engine_SolidRocket_Basic.asset");
            loadout.seeker = Load<SeekerDefinition>("Assets/_Project/Data/Missiles/Seeker_IR_Basic.asset");
            loadout.payload = Load<MissilePayloadDefinition>("Assets/_Project/Data/Missiles/Payload_HEFrag_Small.asset");
            loadout.fuel = Load<FuelDefinition>("Assets/_Project/Data/Shared/Fuel_Solid_Basic.asset");
            return loadout.IsComplete ? loadout : null;
        }

        internal static DroneLoadout LoadStrikeDroneLoadout(MissileLoadout missileLoadout)
        {
            var loadout = new DroneLoadout { designName = "Basic Strike Drone" };
            loadout.propulsion = Load<PropulsionDefinition>("Assets/_Project/Data/Drones/Propulsion_Electric_Basic.asset");
            loadout.airframe = Load<DroneAirframeDefinition>("Assets/_Project/Data/Drones/Airframe_SmallQuad.asset");
            loadout.wingOrPropeller = Load<WingOrPropellerDefinition>("Assets/_Project/Data/Drones/Propeller_Basic.asset");
            loadout.hullMaterial = Load<HullMaterialDefinition>("Assets/_Project/Data/Drones/Hull_CompositePlastic.asset");
            loadout.engine = Load<DroneEngineDefinition>("Assets/_Project/Data/Drones/Engine_Electric_Basic.asset");
            loadout.fuel = Load<FuelDefinition>("Assets/_Project/Data/Shared/Fuel_Battery_Basic.asset");
            loadout.weaponBay = Load<WeaponBayDefinition>("Assets/_Project/Data/Drones/WeaponBay_Small.asset");
            loadout.sensorSuite = Load<SensorSuiteDefinition>("Assets/_Project/Data/Drones/Sensor_Basic.asset");
            loadout.missileLoadout = missileLoadout;
            loadout.ammoCount = 4;
            return loadout.IsComplete ? loadout : null;
        }

        internal static DroneLoadout LoadScoutDroneLoadout()
        {
            var loadout = new DroneLoadout { designName = "Basic Scout Drone" };
            loadout.propulsion = Load<PropulsionDefinition>("Assets/_Project/Data/Drones/Propulsion_Electric_Basic.asset");
            loadout.airframe = Load<DroneAirframeDefinition>("Assets/_Project/Data/Drones/Airframe_SmallQuad.asset");
            loadout.wingOrPropeller = Load<WingOrPropellerDefinition>("Assets/_Project/Data/Drones/Propeller_Basic.asset");
            loadout.hullMaterial = Load<HullMaterialDefinition>("Assets/_Project/Data/Drones/Hull_CompositePlastic.asset");
            loadout.engine = Load<DroneEngineDefinition>("Assets/_Project/Data/Drones/Engine_Electric_Basic.asset");
            loadout.fuel = Load<FuelDefinition>("Assets/_Project/Data/Shared/Fuel_Battery_Basic.asset");
            loadout.weaponBay = Load<WeaponBayDefinition>("Assets/_Project/Data/Drones/WeaponBay_Small.asset");
            loadout.sensorSuite = Load<SensorSuiteDefinition>("Assets/_Project/Data/Drones/Sensor_Scout.asset");
            loadout.missileLoadout = null; // unarmed
            loadout.ammoCount = 0;
            return loadout.IsComplete ? loadout : null;
        }

        internal static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                Debug.LogError($"[Phase1CombatSceneBuilder] Could not load asset at {path}");
            return asset;
        }

        internal static void BuildGround()
        {
            // Depth pass (direct user feedback: "can the sandbox have infinite grass?
            // the current terrain is tiny"): was scale 60 (~600x600m) — meanwhile
            // seeded sensor/seeker ranges now reach up to 10000m (Sensor_Radar_LongRange)
            // and the camera's far clip plane is already 12000m specifically to see
            // that far. A 600m ground plane left most of a long-range engagement
            // happening over visibly untextured nothing. Scaled up to 2000
            // (~20000x20000m) — comfortably bigger than the far clip plane in every
            // direction, so the ground edge is never actually visible regardless of
            // engagement range, reading as effectively infinite without the cost of a
            // real streaming/tiled terrain system.
            const float groundScale = 2000f;
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(groundScale, 1f, groundScale);

            // Plain grey gives no visual reference for motion/speed — apply a simple
            // procedural grid texture, tiled to the arena size, so movement and
            // distance are actually perceivable. No art asset needed.
            var renderer = ground.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                var material = new Material(renderer.sharedMaterial);
                material.mainTexture = CreateGridTexture();
                material.mainTextureScale = new Vector2(groundScale, groundScale); // one grid cell per ~10m, same density as before
                renderer.sharedMaterial = material;
            }
        }

        private static Texture2D CreateGridTexture()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "ProceduralGroundGrid",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
            };

            Color background = new Color(0.35f, 0.37f, 0.35f);
            Color line = new Color(0.55f, 0.58f, 0.55f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isLine = x < 2 || y < 2;
                    texture.SetPixel(x, y, isLine ? line : background);
                }
            }

            texture.Apply();
            return texture;
        }

        internal static void BuildLight()
        {
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Dev-visibility pass (Phase 2D): explicitly assert fog is off with a
            // generous fallback end distance, rather than relying on whatever
            // EditorSceneManager.NewScene happens to default RenderSettings to (which
            // is currently off, but with fogEndDistance=300 baked in — a landmine for
            // anyone who later enables fog without noticing it'd cut off well inside
            // the seeker ranges some parts already have, e.g. Seeker_ARH's 8000m).
            RenderSettings.fog = false;
            RenderSettings.fogEndDistance = 10000f;
        }

        internal static void BuildCamera(Transform followTarget)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            // Dev-visibility pass (Phase 2D): headroom for the longest seeker range
            // currently seeded (Seeker_MultiSpectral, 9000m) plus margin, not just the
            // ~600x600m Phase 1 arena — was 3000f, which would have started clipping
            // long-range engagements before they were even visually spottable.
            cam.farClipPlane = 12000f;
            camGo.AddComponent<AudioListener>();

            camGo.transform.position = followTarget.position + new Vector3(0f, 60f, -40f);
            camGo.transform.LookAt(followTarget.position);

            var chaseCam = camGo.AddComponent<Phase0ChaseCamera>();
            chaseCam.missile = followTarget; // reused component; "missile" is just the primary followed transform
            chaseCam.target = followTarget;
            chaseCam.minDistance = 60f;
            chaseCam.distancePadding = 20f;
        }

        internal static void BuildHud(GameObject player, PlayerDroneController controller)
        {
            var hudGo = new GameObject("HUD");
            var hud = hudGo.AddComponent<HUDController>();
            hud.player = player.transform;
            hud.playerHealth = player.GetComponent<Health>();
            hud.playerWeapon = player.GetComponent<WeaponController>();

            // Every caller of BuildHud (Combat_Arena01, the Phase 2E arenas, and the
            // Test Range via TestRangeSceneBuilder) gets ESC-to-pause/quit for free
            // this way, rather than each scene builder wiring it separately.
            var escapeMenuGo = new GameObject("EscapeMenu");
            escapeMenuGo.AddComponent<EscapeMenuController>();
        }

        /// <summary>
        /// Registers a scene in Build Settings if it isn't already there —
        /// SceneManager.LoadScene(string) fails at runtime for any scene not in this
        /// list. Shared by every Editor scene-builder that creates a new loadable
        /// scene (Phase2EArenaBuilder, TestRangeSceneBuilder) rather than each keeping
        /// its own copy.
        /// </summary>
        internal static void EnsureSceneInBuildSettings(string scenePath)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var existing in scenes)
            {
                if (existing.path == scenePath)
                    return;
            }
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>
        /// Phase 3A: same as EnsureSceneInBuildSettings, but guarantees the scene sits
        /// at a specific build index — needed for MainMenuSceneBuilder, since
        /// SceneManager/the Editor Play button treat build index 0 as the app's actual
        /// entry point. Removes any existing entry for this path first so re-running a
        /// scene builder never produces a duplicate, then (re)inserts at the requested
        /// index, shifting everything else down.
        /// </summary>
        internal static void EnsureSceneInBuildSettingsAtIndex(string scenePath, int index)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(existing => existing.path == scenePath);

            int clampedIndex = Mathf.Clamp(index, 0, scenes.Count);
            scenes.Insert(clampedIndex, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
