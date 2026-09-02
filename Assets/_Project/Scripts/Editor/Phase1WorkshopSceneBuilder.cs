using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Vanquish.Core;
using Vanquish.Data.Drones;
using Vanquish.Data.Missiles;
using Vanquish.Data.Scenarios;
using Vanquish.Data.Shared;
using Vanquish.Data.Support;
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

            // The Workshop UI is pure UI Toolkit (UIDocument, Screen Space - Overlay) and
            // renders fine with zero cameras — but the Editor Game View shows a "No
            // cameras rendering" placeholder over everything whenever a scene has none,
            // which is a harmless but annoying overlay during playtesting. A minimal
            // solid-color camera silences that with no visible effect otherwise (nothing
            // else in this scene needs 3D rendering).
            var cameraGo = new GameObject("Background Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 0; // nothing to render — this exists solely to clear the Game View
            cameraGo.AddComponent<AudioListener>();

            var workshopGo = new GameObject("WorkshopController");

            var uiDocument = workshopGo.AddComponent<UIDocument>();
            uiDocument.visualTreeAsset = Load<VisualTreeAsset>(VisualTreeAssetPath);
            uiDocument.panelSettings = GetOrCreatePanelSettings();

            var workshop = workshopGo.AddComponent<WorkshopController>();
            workshop.combatSceneName = "Combat_Arena01";

            // Phase 2E: "scenario selection needs a place to live" — a small OnGUI
            // picker overlay listing every seeded ScenarioDefinition (see
            // ScenarioPickerOverlay's own doc comment for why OnGUI rather than a
            // Workshop.uxml addition). Missing/unseeded assets are logged, not fatal —
            // an older/partial data-seed state just means the overlay renders nothing
            // and Enter Combat falls back to combatSceneName above, same as before 2E.
            var scenarioPickerGo = new GameObject("ScenarioPickerOverlay");
            var scenarioPicker = scenarioPickerGo.AddComponent<ScenarioPickerOverlay>();
            scenarioPicker.scenarios = new[]
            {
                Load<ScenarioDefinition>("Assets/_Project/Data/Scenarios/Scenario_TierZeroSkirmish.asset"),
                Load<ScenarioDefinition>("Assets/_Project/Data/Scenarios/Scenario_ValleyInterdiction.asset"),
                Load<ScenarioDefinition>("Assets/_Project/Data/Scenarios/Scenario_PlateauSkirmish.asset"),
            };

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
                // Phase 2B drone breadth — seeded by Phase2BDroneBreadthSeeder.SeedTechTreeNodes().
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_airframe_smallhexa.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_airframe_fixedwing.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_airframe_flyingwingstealth.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_airframe_ccascale.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_propeller_plastic_small.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_propeller_plastic_medium.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_propeller_plastic_large.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_propeller_carbonfiber_small.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_propeller_carbonfiber_medium.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_propeller_carbonfiber_large.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_propeller_metal_small.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_propeller_metal_medium.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_propeller_metal_large.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_wing_fixedwing.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_wing_deltawing.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_wing_variablesweepwing.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_hull_aluminumalloy.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_hull_carbonfiber.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_hull_radarabsorbentmaterial.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_hull_titaniumalloy.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_propulsion_ice_basic.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_engine_ice_basic.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_fuel_petrol_basic.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_fuel_diesel_basic.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_propulsion_jet_subsonic.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_engine_jet_subsonic.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_fuel_jetfuel_basic.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_propulsion_jet_supersonic.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_engine_jet_supersonic.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_weaponbay_large.asset"),
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2B_drone_weaponbay_internalmedium.asset"),
                // Phase 2C guidance depth — seeded by Phase2CGuidanceDepthSeeder.
                Load<TechNode>("Assets/_Project/Data/TechTree/TN_2C_support_datalink_midcourserelay.asset"),
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

            workshop.missileDatalinkOptions = new[]
            {
                Load<DatalinkNetworkDefinition>("Assets/_Project/Data/Support/Datalink_MidCourseRelay.asset"),
            };

            workshop.droneSensorBasic = Load<SensorSuiteDefinition>("Assets/_Project/Data/Drones/Sensor_Basic.asset");
            workshop.droneSensorScout = Load<SensorSuiteDefinition>("Assets/_Project/Data/Drones/Sensor_Scout.asset");

            workshop.dronePropulsionOptions = new[]
            {
                Load<PropulsionDefinition>("Assets/_Project/Data/Drones/Propulsion_Electric_Basic.asset"),
                Load<PropulsionDefinition>("Assets/_Project/Data/Drones/Propulsion_ICE_Basic.asset"),
                Load<PropulsionDefinition>("Assets/_Project/Data/Drones/Propulsion_Jet_Subsonic.asset"),
                Load<PropulsionDefinition>("Assets/_Project/Data/Drones/Propulsion_Jet_Supersonic.asset"),
            };

            workshop.droneAirframeOptions = new[]
            {
                Load<DroneAirframeDefinition>("Assets/_Project/Data/Drones/Airframe_SmallQuad.asset"),
                Load<DroneAirframeDefinition>("Assets/_Project/Data/Drones/Airframe_SmallHexa.asset"),
                Load<DroneAirframeDefinition>("Assets/_Project/Data/Drones/Airframe_FixedWing.asset"),
                Load<DroneAirframeDefinition>("Assets/_Project/Data/Drones/Airframe_FlyingWingStealth.asset"),
                Load<DroneAirframeDefinition>("Assets/_Project/Data/Drones/Airframe_CcaScale.asset"),
            };

            workshop.droneWingOptions = new[]
            {
                Load<WingOrPropellerDefinition>("Assets/_Project/Data/Drones/Propeller_Basic.asset"),
                Load<WingOrPropellerDefinition>("Assets/_Project/Data/Drones/Propeller_Plastic_Small.asset"),
                Load<WingOrPropellerDefinition>("Assets/_Project/Data/Drones/Propeller_Plastic_Medium.asset"),
                Load<WingOrPropellerDefinition>("Assets/_Project/Data/Drones/Propeller_Plastic_Large.asset"),
                Load<WingOrPropellerDefinition>("Assets/_Project/Data/Drones/Propeller_CarbonFiber_Small.asset"),
                Load<WingOrPropellerDefinition>("Assets/_Project/Data/Drones/Propeller_CarbonFiber_Medium.asset"),
                Load<WingOrPropellerDefinition>("Assets/_Project/Data/Drones/Propeller_CarbonFiber_Large.asset"),
                Load<WingOrPropellerDefinition>("Assets/_Project/Data/Drones/Propeller_Metal_Small.asset"),
                Load<WingOrPropellerDefinition>("Assets/_Project/Data/Drones/Propeller_Metal_Medium.asset"),
                Load<WingOrPropellerDefinition>("Assets/_Project/Data/Drones/Propeller_Metal_Large.asset"),
                Load<WingOrPropellerDefinition>("Assets/_Project/Data/Drones/Wing_FixedWing.asset"),
                Load<WingOrPropellerDefinition>("Assets/_Project/Data/Drones/Wing_DeltaWing.asset"),
                Load<WingOrPropellerDefinition>("Assets/_Project/Data/Drones/Wing_VariableSweepWing.asset"),
            };

            workshop.droneHullOptions = new[]
            {
                Load<HullMaterialDefinition>("Assets/_Project/Data/Drones/Hull_CompositePlastic.asset"),
                Load<HullMaterialDefinition>("Assets/_Project/Data/Drones/Hull_AluminumAlloy.asset"),
                Load<HullMaterialDefinition>("Assets/_Project/Data/Drones/Hull_CarbonFiber.asset"),
                Load<HullMaterialDefinition>("Assets/_Project/Data/Drones/Hull_RadarAbsorbentMaterial.asset"),
                Load<HullMaterialDefinition>("Assets/_Project/Data/Drones/Hull_TitaniumAlloy.asset"),
            };

            workshop.droneEngineOptions = new[]
            {
                Load<DroneEngineDefinition>("Assets/_Project/Data/Drones/Engine_Electric_Basic.asset"),
                Load<DroneEngineDefinition>("Assets/_Project/Data/Drones/Engine_ICE_Basic.asset"),
                Load<DroneEngineDefinition>("Assets/_Project/Data/Drones/Engine_Jet_Subsonic.asset"),
                Load<DroneEngineDefinition>("Assets/_Project/Data/Drones/Engine_Jet_Supersonic.asset"),
            };

            workshop.droneFuelOptions = new[]
            {
                Load<FuelDefinition>("Assets/_Project/Data/Shared/Fuel_Battery_Basic.asset"),
                Load<FuelDefinition>("Assets/_Project/Data/Shared/Fuel_Petrol_Basic.asset"),
                Load<FuelDefinition>("Assets/_Project/Data/Shared/Fuel_Diesel_Basic.asset"),
                Load<FuelDefinition>("Assets/_Project/Data/Shared/Fuel_JetFuel_Basic.asset"),
            };

            workshop.droneWeaponBayOptions = new[]
            {
                Load<WeaponBayDefinition>("Assets/_Project/Data/Drones/WeaponBay_Small.asset"),
                Load<WeaponBayDefinition>("Assets/_Project/Data/Drones/WeaponBay_Large.asset"),
                Load<WeaponBayDefinition>("Assets/_Project/Data/Drones/WeaponBay_InternalMedium.asset"),
            };

            // Phase 2C: reuses the exact same CountermeasureDefinition assets already
            // seeded for the missile slot above — see Phase2CGuidanceDepthSeeder's doc
            // comment for why this isn't a duplicate set of drone-specific assets.
            workshop.droneCountermeasureOptions = new[]
            {
                Load<CountermeasureDefinition>("Assets/_Project/Data/Missiles/Countermeasure_FlareChaffDispenser.asset"),
                Load<CountermeasureDefinition>("Assets/_Project/Data/Missiles/Countermeasure_RcsShaping.asset"),
            };

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
