using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Vanquish.Core;
using Vanquish.Data.Drones;
using Vanquish.Data.Missiles;
using Vanquish.Data.Shared;
using Vanquish.Data.TechTree;
using Vanquish.Workshop;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Builds the Workshop scene: a PlayerProgress bootstrap (persists across scenes)
    /// and the WorkshopController UI Toolkit UI wired to the seeded tech tree and
    /// Tier-0 parts. Requires Phase1DataSeeder to have been run first. See the headless
    /// testing workflow in docs/CODING_STANDARDS.md.
    /// </summary>
    public static class Phase1WorkshopSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Workshop.unity";
        private const string VisualTreeAssetPath = "Assets/_Project/UI/Workshop/Workshop.uxml";
        private const string StyleSheetPath = "Assets/_Project/UI/Workshop/Workshop.uss";
        private const string PanelSettingsPath = "Assets/_Project/UI/Workshop/WorkshopPanelSettings.asset";

        [MenuItem("Vanquish/Phase 1/Build Workshop Scene")]
        public static void BuildScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Phase1WorkshopSceneBuilder] Cannot rebuild while in Play mode.");
                return;
            }

            // Create the scene before loading any part assets — see the detailed
            // comment in Phase1CombatSceneBuilder.BuildScene about why loading assets
            // before EditorSceneManager.NewScene can cause them to go "fake null".
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootstrapGo = new GameObject("GameBootstrap");
            bootstrapGo.AddComponent<PlayerProgress>();

            var workshopGo = new GameObject("WorkshopController");

            var uiDocument = workshopGo.AddComponent<UIDocument>();
            uiDocument.visualTreeAsset = Load<VisualTreeAsset>(VisualTreeAssetPath);
            uiDocument.panelSettings = GetOrCreatePanelSettings();

            var workshop = workshopGo.AddComponent<WorkshopController>();
            workshop.combatSceneName = "Combat_Arena01";

            workshop.techTree = new[]
            {
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_01_MissileAirframe.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_02_MissileEngine.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_03_MissileSeeker.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_04_MissilePayload.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_05_MissileFuel.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_06_DroneBasics.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_07_DronePower.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_08_DroneStructure.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_09_DroneSystems.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_10_ScoutSensor.asset"),
                // Phase 2A missile breadth — seeded by Phase2AMissileBreadthSeeder.SeedTechTreeNodes().
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2A_missile_payload_grenade.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2A_missile_payload_shapedcharge.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2A_missile_payload_kinetic.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2A_missile_payload_cluster.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2A_missile_engine_liquid_basic.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2A_missile_engine_ramjet_basic.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2A_missile_engine_scramjet_basic.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2A_missile_seeker_wire_saclos.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2A_missile_seeker_laser.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2A_missile_seeker_optical_tv.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2A_missile_seeker_sarh.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2A_missile_seeker_arh.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2A_missile_seeker_imaging_ir.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2A_missile_seeker_multispectral.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2A_missile_countermeasure_flarechaff.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2A_missile_countermeasure_rcsshaping.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2A_missile_jamming_ecmpod.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2A_missile_jamming_eccmsuite.asset"),
            };

            workshop.missileAirframe = Load<MissileAirframeDefinition>("Assets/_Project/Data/Missiles/Airframe_Basic.asset");
            workshop.missileFuel = Load<FuelDefinition>("Assets/_Project/Data/Shared/Fuel_Solid_Basic.asset");

            workshop.missileEngineOptions = new[]
            {
                Load<MissileEngineDefinition>("Assets/_Project/Data/Missiles/Engine_SolidRocket_Basic.asset"),
                Load<MissileEngineDefinition>("Assets/_Project/Data/Missiles/Engine_LiquidRocket.asset"),
                Load<MissileEngineDefinition>("Assets/_Project/Data/Missiles/Engine_Ramjet.asset"),
                Load<MissileEngineDefinition>("Assets/_Project/Data/Missiles/Engine_Scramjet.asset"),
            };

            workshop.missileSeekerOptions = new[]
            {
                Load<SeekerDefinition>("Assets/_Project/Data/Missiles/Seeker_IR_Basic.asset"),
                Load<SeekerDefinition>("Assets/_Project/Data/Missiles/Seeker_WireSaclos.asset"),
                Load<SeekerDefinition>("Assets/_Project/Data/Missiles/Seeker_Laser.asset"),
                Load<SeekerDefinition>("Assets/_Project/Data/Missiles/Seeker_OpticalTv.asset"),
                Load<SeekerDefinition>("Assets/_Project/Data/Missiles/Seeker_SARH.asset"),
                Load<SeekerDefinition>("Assets/_Project/Data/Missiles/Seeker_ARH.asset"),
                Load<SeekerDefinition>("Assets/_Project/Data/Missiles/Seeker_ImagingIR.asset"),
                Load<SeekerDefinition>("Assets/_Project/Data/Missiles/Seeker_MultiSpectral.asset"),
            };

            workshop.missilePayloadOptions = new[]
            {
                Load<MissilePayloadDefinition>("Assets/_Project/Data/Missiles/Payload_HEFrag_Small.asset"),
                Load<MissilePayloadDefinition>("Assets/_Project/Data/Missiles/Payload_Grenade.asset"),
                Load<MissilePayloadDefinition>("Assets/_Project/Data/Missiles/Payload_ShapedCharge.asset"),
                Load<MissilePayloadDefinition>("Assets/_Project/Data/Missiles/Payload_Kinetic.asset"),
                Load<MissilePayloadDefinition>("Assets/_Project/Data/Missiles/Payload_Cluster.asset"),
            };

            workshop.missileCountermeasureOptions = new[]
            {
                Load<CountermeasureDefinition>("Assets/_Project/Data/Missiles/Countermeasure_FlareChaffDispenser.asset"),
                Load<CountermeasureDefinition>("Assets/_Project/Data/Missiles/Countermeasure_RcsShaping.asset"),
            };

            workshop.missileJammingOptions = new[]
            {
                Load<JammingDefinition>("Assets/_Project/Data/Missiles/Jamming_EcmPod.asset"),
                Load<JammingDefinition>("Assets/_Project/Data/Missiles/Jamming_EccmSuite.asset"),
            };

            workshop.dronePropulsion = Load<PropulsionDefinition>("Assets/_Project/Data/Drones/Propulsion_Electric_Basic.asset");
            workshop.droneAirframe = Load<DroneAirframeDefinition>("Assets/_Project/Data/Drones/Airframe_SmallQuad.asset");
            workshop.droneWing = Load<WingOrPropellerDefinition>("Assets/_Project/Data/Drones/Propeller_Basic.asset");
            workshop.droneHull = Load<HullMaterialDefinition>("Assets/_Project/Data/Drones/Hull_CompositePlastic.asset");
            workshop.droneEngine = Load<DroneEngineDefinition>("Assets/_Project/Data/Drones/Engine_Electric_Basic.asset");
            workshop.droneFuel = Load<FuelDefinition>("Assets/_Project/Data/Shared/Fuel_Battery_Basic.asset");
            workshop.droneWeaponBay = Load<WeaponBayDefinition>("Assets/_Project/Data/Drones/WeaponBay_Small.asset");
            workshop.droneSensorBasic = Load<SensorSuiteDefinition>("Assets/_Project/Data/Drones/Sensor_Basic.asset");
            workshop.droneSensorScout = Load<SensorSuiteDefinition>("Assets/_Project/Data/Drones/Sensor_Scout.asset");

            System.IO.Directory.CreateDirectory("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log($"[Phase1WorkshopSceneBuilder] Scene built and saved to {ScenePath}");
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                Debug.LogError($"[Phase1WorkshopSceneBuilder] Could not load asset at {path}");
            return asset;
        }

        /// <summary>
        /// Loads the shared Workshop PanelSettings asset, or creates it the first time
        /// this scene is built. Deliberately left with the default (unassigned)
        /// themeStyleSheet — Workshop.uss styles every element we actually use
        /// explicitly, so this doesn't depend on locating Unity's built-in runtime
        /// theme asset from editor script code.
        /// </summary>
        private static PanelSettings GetOrCreatePanelSettings()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (existing != null)
                return existing;

            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            AssetDatabase.CreateAsset(settings, PanelSettingsPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Phase1WorkshopSceneBuilder] Created new PanelSettings asset at {PanelSettingsPath}");
            return settings;
        }
    }
}
