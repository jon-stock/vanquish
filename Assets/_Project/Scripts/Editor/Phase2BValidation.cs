using UnityEditor;
using UnityEngine;
using Vanquish.Data;
using Vanquish.Data.Drones;
using Vanquish.Data.Shared;
using Vanquish.Data.TechTree;
using Vanquish.Simulation.Flight;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Headless sanity checks for Phase 2B's drone part breadth, MTOW validation, and
    /// altitude/landing math — same pattern as Phase2AValidation: load seeded assets
    /// and exercise the calculators/pure functions directly, logging PASS/FAIL for a
    /// human (or a batchmode CI log grep) to check. Run via `Unity.exe -batchmode -quit
    /// -executeMethod Vanquish.EditorTools.Phase2BValidation.&lt;MethodName&gt;`.
    /// </summary>
    public static class Phase2BValidation
    {
        private const string DronesDir = "Assets/_Project/Data/Drones";
        private const string SharedDir = "Assets/_Project/Data/Shared";

        [MenuItem("Vanquish/Phase 2B/Validate Tier-0 Drone MTOW (Headless)")]
        public static void ValidateTier0DroneMtow()
        {
            var loadout = new DroneLoadout
            {
                designName = "Validation Basic Strike Drone",
                propulsion = Load<PropulsionDefinition>($"{DronesDir}/Propulsion_Electric_Basic.asset"),
                airframe = Load<DroneAirframeDefinition>($"{DronesDir}/Airframe_SmallQuad.asset"),
                wingOrPropeller = Load<WingOrPropellerDefinition>($"{DronesDir}/Propeller_Basic.asset"),
                hullMaterial = Load<HullMaterialDefinition>($"{DronesDir}/Hull_CompositePlastic.asset"),
                engine = Load<DroneEngineDefinition>($"{DronesDir}/Engine_Electric_Basic.asset"),
                fuel = Load<FuelDefinition>($"{SharedDir}/Fuel_Battery_Basic.asset"),
                weaponBay = Load<WeaponBayDefinition>($"{DronesDir}/WeaponBay_Small.asset"),
                sensorSuite = Load<SensorSuiteDefinition>($"{DronesDir}/Sensor_Basic.asset"),
                fuelFillFraction = 1f,
            };

            if (!loadout.IsComplete)
            {
                Debug.LogError("[Phase2BValidation] FAILED: Tier-0 drone loadout incomplete — one or more seeded " +
                    "assets failed to load. Run Vanquish/Phase 1/Seed Tier-0 Data first.");
                return;
            }

            var stats = DesignStatsCalculator.Calculate(loadout);
            bool mtowConfigured = stats.maxTakeOffMassKg > 0f;
            bool withinMtow = stats.isWithinMtow;
            bool notForwardFlight = !stats.requiresForwardFlight; // electric quadcopter must stay omnidirectional

            Debug.Log($"[Phase2BValidation] Tier-0 drone (no missiles) @ full battery: massKg={stats.massKg:F1}, " +
                $"maxTakeOffMassKg={stats.maxTakeOffMassKg:F1}, isWithinMtow={stats.isWithinMtow}, " +
                $"fuelMassKg={stats.fuelMassKg:F1}, requiresForwardFlight={stats.requiresForwardFlight}. " +
                $"{(mtowConfigured && withinMtow && notForwardFlight ? "PASS" : "FAIL")}");

            // Confirm the fuel-fill slider affects mass (mirrors Phase2AValidation's missile check).
            loadout.fuelFillFraction = 0f;
            var emptyStats = DesignStatsCalculator.Calculate(loadout);
            bool sliderWorks = emptyStats.fuelMassKg < stats.fuelMassKg && emptyStats.massKg < stats.massKg;
            Debug.Log($"[Phase2BValidation] Tier-0 drone @ empty battery: massKg={emptyStats.massKg:F1}, " +
                $"fuelMassKg={emptyStats.fuelMassKg:F1}. Fuel slider affects mass: {(sliderWorks ? "PASS" : "FAIL")}");

            // A drone loaded with 4x Tier-0 missiles should still fit under the Tier-0
            // airframe's MTOW (this is the scenario Phase1WorkshopSceneBuilder actually
            // spawns via "Enter Combat") — confirms Airframe_SmallQuad.maxTakeOffMassKg=180
            // was set with real headroom, not just enough for the bare airframe.
            var missileLoadout = new Vanquish.Data.Missiles.MissileLoadout
            {
                airframe = Load<Vanquish.Data.Missiles.MissileAirframeDefinition>("Assets/_Project/Data/Missiles/Airframe_Basic.asset"),
                engine = Load<Vanquish.Data.Missiles.MissileEngineDefinition>("Assets/_Project/Data/Missiles/Engine_SolidRocket_Basic.asset"),
                seeker = Load<Vanquish.Data.Missiles.SeekerDefinition>("Assets/_Project/Data/Missiles/Seeker_IR_Basic.asset"),
                payload = Load<Vanquish.Data.Missiles.MissilePayloadDefinition>("Assets/_Project/Data/Missiles/Payload_HEFrag_Small.asset"),
                fuel = Load<FuelDefinition>($"{SharedDir}/Fuel_Solid_Basic.asset"),
                fuelFillFraction = 1f,
            };
            loadout.fuelFillFraction = 1f;
            loadout.missileLoadout = missileLoadout;
            loadout.ammoCount = 4;
            var armedStats = DesignStatsCalculator.Calculate(loadout);
            bool armedWithinMtow = armedStats.isWithinMtow;
            Debug.Log($"[Phase2BValidation] Tier-0 strike drone (4x Tier-0 missiles) @ full fuel: " +
                $"massKg={armedStats.massKg:F1} / MTOW {armedStats.maxTakeOffMassKg:F1}. " +
                $"{(armedWithinMtow ? "PASS" : "FAIL")}");

            if (!mtowConfigured || !withinMtow || !notForwardFlight || !sliderWorks || !armedWithinMtow)
                Debug.LogError("[Phase2BValidation] One or more Tier-0 drone MTOW checks FAILED — see log lines above.");
        }

        [MenuItem("Vanquish/Phase 2B/Validate Drone Breadth Assets (Headless)")]
        public static void ValidateDroneBreadthAssets()
        {
            bool allPass = true;

            // Airframes: Hexacopter/FixedWing/FlyingWingStealth/CcaScale all seeded with
            // sane rotorCount (0 for non-multirotor classes, >0 for Hexacopter) and MTOW.
            allPass &= CheckAirframe($"{DronesDir}/Airframe_SmallHexa.asset", DroneAirframeClass.Hexacopter, expectRotors: true);
            allPass &= CheckAirframe($"{DronesDir}/Airframe_FixedWing.asset", DroneAirframeClass.FixedWing, expectRotors: false);
            allPass &= CheckAirframe($"{DronesDir}/Airframe_FlyingWingStealth.asset", DroneAirframeClass.FlyingWingStealth, expectRotors: false);
            allPass &= CheckAirframe($"{DronesDir}/Airframe_CcaScale.asset", DroneAirframeClass.CcaScale, expectRotors: false);

            // Rotors: all 9 RotorMaterial x RotorSize combinations exist.
            foreach (RotorMaterial material in System.Enum.GetValues(typeof(RotorMaterial)))
            {
                foreach (RotorSize size in System.Enum.GetValues(typeof(RotorSize)))
                {
                    string path = $"{DronesDir}/Propeller_{material}_{size}.asset";
                    var rotor = AssetDatabase.LoadAssetAtPath<WingOrPropellerDefinition>(path);
                    if (rotor == null || rotor.rotorMaterial != material || rotor.rotorSize != size)
                    {
                        Debug.LogError($"[Phase2BValidation] FAIL: missing/misconfigured rotor asset {path}.");
                        allPass = false;
                    }
                }
            }

            // Wing types.
            allPass &= CheckAsset<WingOrPropellerDefinition>($"{DronesDir}/Wing_FixedWing.asset");
            allPass &= CheckAsset<WingOrPropellerDefinition>($"{DronesDir}/Wing_DeltaWing.asset");
            allPass &= CheckAsset<WingOrPropellerDefinition>($"{DronesDir}/Wing_VariableSweepWing.asset");

            // Hull materials.
            allPass &= CheckAsset<HullMaterialDefinition>($"{DronesDir}/Hull_AluminumAlloy.asset");
            allPass &= CheckAsset<HullMaterialDefinition>($"{DronesDir}/Hull_CarbonFiber.asset");
            allPass &= CheckAsset<HullMaterialDefinition>($"{DronesDir}/Hull_RadarAbsorbentMaterial.asset");
            allPass &= CheckAsset<HullMaterialDefinition>($"{DronesDir}/Hull_TitaniumAlloy.asset");

            // Propulsion/engine/fuel spectrum, including the requiresForwardFlight flag.
            allPass &= CheckPropulsion($"{DronesDir}/Propulsion_ICE_Basic.asset", expectForwardFlight: false);
            allPass &= CheckPropulsion($"{DronesDir}/Propulsion_Jet_Subsonic.asset", expectForwardFlight: true);
            allPass &= CheckPropulsion($"{DronesDir}/Propulsion_Jet_Supersonic.asset", expectForwardFlight: true);
            allPass &= CheckAsset<DroneEngineDefinition>($"{DronesDir}/Engine_ICE_Basic.asset");
            allPass &= CheckAsset<DroneEngineDefinition>($"{DronesDir}/Engine_Jet_Subsonic.asset");
            allPass &= CheckAsset<DroneEngineDefinition>($"{DronesDir}/Engine_Jet_Supersonic.asset");
            allPass &= CheckAsset<FuelDefinition>($"{SharedDir}/Fuel_Petrol_Basic.asset");
            allPass &= CheckAsset<FuelDefinition>($"{SharedDir}/Fuel_Diesel_Basic.asset");
            allPass &= CheckAsset<FuelDefinition>($"{SharedDir}/Fuel_JetFuel_Basic.asset");

            // Weapon bay variants.
            allPass &= CheckAsset<WeaponBayDefinition>($"{DronesDir}/WeaponBay_Large.asset");
            var internalBay = AssetDatabase.LoadAssetAtPath<WeaponBayDefinition>($"{DronesDir}/WeaponBay_InternalMedium.asset");
            if (internalBay == null || !internalBay.isInternal)
            {
                Debug.LogError("[Phase2BValidation] FAIL: WeaponBay_InternalMedium missing or isInternal != true.");
                allPass = false;
            }

            Debug.Log(allPass
                ? "[Phase2BValidation] Drone breadth asset check: ALL PASS"
                : "[Phase2BValidation] Drone breadth asset check: ONE OR MORE FAILURES ABOVE");

            if (!allPass)
                Debug.LogError("[Phase2BValidation] Drone breadth asset validation FAILED.");
        }

        [MenuItem("Vanquish/Phase 2B/Validate Drone Breadth Tech Wiring (Headless)")]
        public static void ValidateDroneBreadthTechWiring()
        {
            const string TechDir = "Assets/_Project/Data/TechTree";
            string[] nodeIds =
            {
                "TN_2B_drone_airframe_smallhexa", "TN_2B_drone_airframe_fixedwing",
                "TN_2B_drone_airframe_flyingwingstealth", "TN_2B_drone_airframe_ccascale",
                "TN_2B_drone_propeller_plastic_small", "TN_2B_drone_propeller_plastic_medium",
                "TN_2B_drone_propeller_plastic_large", "TN_2B_drone_propeller_carbonfiber_small",
                "TN_2B_drone_propeller_carbonfiber_medium", "TN_2B_drone_propeller_carbonfiber_large",
                "TN_2B_drone_propeller_metal_small", "TN_2B_drone_propeller_metal_medium",
                "TN_2B_drone_propeller_metal_large",
                "TN_2B_drone_wing_fixedwing", "TN_2B_drone_wing_deltawing", "TN_2B_drone_wing_variablesweepwing",
                "TN_2B_drone_hull_aluminumalloy", "TN_2B_drone_hull_carbonfiber",
                "TN_2B_drone_hull_radarabsorbentmaterial", "TN_2B_drone_hull_titaniumalloy",
                "TN_2B_drone_propulsion_ice_basic", "TN_2B_drone_engine_ice_basic",
                "TN_2B_fuel_petrol_basic", "TN_2B_fuel_diesel_basic",
                "TN_2B_drone_propulsion_jet_subsonic", "TN_2B_drone_engine_jet_subsonic", "TN_2B_fuel_jetfuel_basic",
                "TN_2B_drone_propulsion_jet_supersonic", "TN_2B_drone_engine_jet_supersonic",
                "TN_2B_drone_weaponbay_large", "TN_2B_drone_weaponbay_internalmedium",
            };

            bool allPass = true;
            int checkedCount = 0;

            foreach (var id in nodeIds)
            {
                var node = AssetDatabase.LoadAssetAtPath<TechNode>($"{TechDir}/{id}.asset");
                if (node == null)
                {
                    Debug.LogError($"[Phase2BValidation] FAIL: missing tech node {id}. Run " +
                        "Vanquish/Phase 2B/Seed Drone Breadth Tech Nodes (after the six variant seeders).");
                    allPass = false;
                    continue;
                }

                checkedCount++;

                if (node.unlocks == null || node.unlocks.Length != 1 || node.unlocks[0] == null)
                {
                    Debug.LogError($"[Phase2BValidation] FAIL: {id} does not unlock exactly one non-null part.");
                    allPass = false;
                }

                if (node.prerequisites == null || node.prerequisites.Length == 0)
                {
                    Debug.LogError($"[Phase2BValidation] FAIL: {id} has no prerequisites — should never be free.");
                    allPass = false;
                }
            }

            var supersonicNode = AssetDatabase.LoadAssetAtPath<TechNode>($"{TechDir}/TN_2B_drone_propulsion_jet_supersonic.asset");
            var subsonicNode = AssetDatabase.LoadAssetAtPath<TechNode>($"{TechDir}/TN_2B_drone_propulsion_jet_subsonic.asset");
            bool chainOk = supersonicNode != null && subsonicNode != null
                && supersonicNode.prerequisites != null && supersonicNode.prerequisites.Length == 1
                && supersonicNode.prerequisites[0] == subsonicNode;
            Debug.Log($"[Phase2BValidation] Propulsion progression chain (Supersonic Jet requires Subsonic Jet): {(chainOk ? "PASS" : "FAIL")}");
            allPass &= chainOk;

            Debug.Log($"[Phase2BValidation] Checked {checkedCount}/{nodeIds.Length} expected Phase 2B drone tech nodes. " +
                (allPass ? "ALL PASS" : "ONE OR MORE FAILURES ABOVE"));

            if (!allPass)
                Debug.LogError("[Phase2BValidation] Drone breadth tech wiring validation FAILED.");
        }

        [MenuItem("Vanquish/Phase 2B/Validate Altitude & Landing Math (Headless)")]
        public static void ValidateAltitudeAndLandingMath()
        {
            bool allPass = true;

            // AbsoluteMSL ignores ground height entirely.
            float msl = AltitudeController.ComputeTargetWorldAltitude(AltitudeMode.AbsoluteMSL, 120f, groundHeightMeters: 40f);
            bool mslOk = Mathf.Approximately(msl, 120f);
            Debug.Log($"[Phase2BValidation] AbsoluteMSL target (desired=120, ground=40) = {msl:F1}. {(mslOk ? "PASS" : "FAIL")}");
            allPass &= mslOk;

            // RelativeAGL adds ground height.
            float agl = AltitudeController.ComputeTargetWorldAltitude(AltitudeMode.RelativeAGL, 50f, groundHeightMeters: 40f);
            bool aglOk = Mathf.Approximately(agl, 90f);
            Debug.Log($"[Phase2BValidation] RelativeAGL target (desired=50, ground=40) = {agl:F1}. {(aglOk ? "PASS" : "FAIL")}");
            allPass &= aglOk;

            // Climb-rate clamp: a huge height error should still only command
            // maxClimbRateMetersPerSecond, not an unbounded vertical speed.
            float bigErrorAccel = AltitudeController.ComputeVerticalAccel(currentWorldY: 0f, currentVerticalSpeed: 0f,
                targetWorldY: 500f, maxClimbRateMetersPerSecond: 10f, verticalAccelGain: 4f);
            float smallErrorAccel = AltitudeController.ComputeVerticalAccel(currentWorldY: 0f, currentVerticalSpeed: 0f,
                targetWorldY: 500f, maxClimbRateMetersPerSecond: 20f, verticalAccelGain: 4f);
            bool climbClamped = Mathf.Approximately(bigErrorAccel, 10f * 4f) && smallErrorAccel > bigErrorAccel;
            Debug.Log($"[Phase2BValidation] Climb-rate clamp: accel@maxClimb10={bigErrorAccel:F1}, " +
                $"accel@maxClimb20={smallErrorAccel:F1} (should be larger). {(climbClamped ? "PASS" : "FAIL")}");
            allPass &= climbClamped;

            // At the target with zero vertical speed, commanded acceleration should be ~0.
            float settledAccel = AltitudeController.ComputeVerticalAccel(currentWorldY: 90f, currentVerticalSpeed: 0f,
                targetWorldY: 90f, maxClimbRateMetersPerSecond: 10f, verticalAccelGain: 4f);
            bool settledOk = Mathf.Approximately(settledAccel, 0f);
            Debug.Log($"[Phase2BValidation] Settled-at-target accel = {settledAccel:F2}. {(settledOk ? "PASS" : "FAIL")}");
            allPass &= settledOk;

            // Vertical cliff detection.
            var cliffCheck = new TerrainCollisionCheck { WillCollide = true, DistanceToImpact = 50f, SurfaceNormal = Vector3.right };
            var slopeCheck = new TerrainCollisionCheck { WillCollide = true, DistanceToImpact = 50f, SurfaceNormal = Vector3.up };
            bool cliffOk = cliffCheck.IsVerticalCliff() && !slopeCheck.IsVerticalCliff();
            Debug.Log($"[Phase2BValidation] Cliff detection: vertical-normal-surface IsVerticalCliff={cliffCheck.IsVerticalCliff()}, " +
                $"flat-surface IsVerticalCliff={slopeCheck.IsVerticalCliff()}. {(cliffOk ? "PASS" : "FAIL")}");
            allPass &= cliffOk;

            // Required climb rate formula.
            float requiredClimb = TerrainCollisionChecker.RequiredClimbRate(forwardSpeed: 40f, heightDelta: 20f, detectionDistance: 100f);
            bool requiredClimbOk = Mathf.Approximately(requiredClimb, 8f); // 40 * 20 / 100
            Debug.Log($"[Phase2BValidation] Required climb rate (v=40, dh=20, d=100) = {requiredClimb:F1} m/s. " +
                $"{(requiredClimbOk ? "PASS" : "FAIL")}");
            allPass &= requiredClimbOk;

            // Landing validation: safe on flat grass at low speed.
            bool safeOnGrass = LandingValidator.CanLandSafely(new Vector3(1f, -2f, 1f), Vector3.up,
                LandingSurfaceType.FlatGrassOrSoil, maxVerticalSpeedMetersPerSecond: 4f, maxHorizontalSpeedMetersPerSecond: 6f,
                out string grassReason);
            Debug.Log($"[Phase2BValidation] Safe landing on flat grass, low speed: {safeOnGrass} ({grassReason}). " +
                $"{(safeOnGrass ? "PASS" : "FAIL")}");
            allPass &= safeOnGrass;

            // Landing validation: never safe on water regardless of speed/slope.
            bool unsafeOnWater = !LandingValidator.CanLandSafely(Vector3.zero, Vector3.up, LandingSurfaceType.WaterOrMarsh,
                maxVerticalSpeedMetersPerSecond: 100f, maxHorizontalSpeedMetersPerSecond: 100f, out string waterReason);
            Debug.Log($"[Phase2BValidation] Water/Marsh always unsafe: {unsafeOnWater} ({waterReason}). {(unsafeOnWater ? "PASS" : "FAIL")}");
            allPass &= unsafeOnWater;

            // Landing validation: excessive sink rate fails even on the most forgiving surface.
            bool unsafeSinkRate = !LandingValidator.CanLandSafely(new Vector3(0f, -20f, 0f), Vector3.up,
                LandingSurfaceType.PavedRunwayOrHelipad, maxVerticalSpeedMetersPerSecond: 4f, maxHorizontalSpeedMetersPerSecond: 6f,
                out string sinkReason);
            Debug.Log($"[Phase2BValidation] Excessive sink rate rejected on paved runway: {unsafeSinkRate} ({sinkReason}). " +
                $"{(unsafeSinkRate ? "PASS" : "FAIL")}");
            allPass &= unsafeSinkRate;

            // Landing validation: steep slope fails on rock (5-degree max) even at safe speed.
            Vector3 steepNormal = Quaternion.Euler(20f, 0f, 0f) * Vector3.up;
            bool unsafeSlope = !LandingValidator.CanLandSafely(new Vector3(1f, -1f, 0f), steepNormal,
                LandingSurfaceType.UnevenOrRock, maxVerticalSpeedMetersPerSecond: 4f, maxHorizontalSpeedMetersPerSecond: 6f,
                out string slopeReason);
            Debug.Log($"[Phase2BValidation] Steep 20\u00b0 slope rejected on rock (max 5\u00b0): {unsafeSlope} ({slopeReason}). " +
                $"{(unsafeSlope ? "PASS" : "FAIL")}");
            allPass &= unsafeSlope;

            Debug.Log(allPass
                ? "[Phase2BValidation] Altitude & landing math check: ALL PASS"
                : "[Phase2BValidation] Altitude & landing math check: ONE OR MORE FAILURES ABOVE");

            if (!allPass)
                Debug.LogError("[Phase2BValidation] Altitude & landing math validation FAILED.");
        }

        private static bool CheckAirframe(string path, DroneAirframeClass expectedClass, bool expectRotors)
        {
            var airframe = AssetDatabase.LoadAssetAtPath<DroneAirframeDefinition>(path);
            if (airframe == null)
            {
                Debug.LogError($"[Phase2BValidation] FAIL: missing airframe asset {path}.");
                return false;
            }
            bool ok = airframe.airframeClass == expectedClass
                && (expectRotors ? airframe.rotorCount > 0 : airframe.rotorCount == 0)
                && airframe.maxTakeOffMassKg > 0f;
            if (!ok)
            {
                Debug.LogError($"[Phase2BValidation] FAIL: {path} misconfigured (class={airframe.airframeClass}, " +
                    $"rotorCount={airframe.rotorCount}, maxTakeOffMassKg={airframe.maxTakeOffMassKg}).");
            }
            return ok;
        }

        private static bool CheckPropulsion(string path, bool expectForwardFlight)
        {
            var propulsion = AssetDatabase.LoadAssetAtPath<PropulsionDefinition>(path);
            if (propulsion == null)
            {
                Debug.LogError($"[Phase2BValidation] FAIL: missing propulsion asset {path}.");
                return false;
            }
            bool ok = propulsion.requiresForwardFlight == expectForwardFlight;
            if (!ok)
            {
                Debug.LogError($"[Phase2BValidation] FAIL: {path} requiresForwardFlight=" +
                    $"{propulsion.requiresForwardFlight}, expected {expectForwardFlight}.");
            }
            return ok;
        }

        private static bool CheckAsset<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                Debug.LogError($"[Phase2BValidation] FAIL: missing asset {path}.");
            return asset != null;
        }

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                Debug.LogError($"[Phase2BValidation] Could not load asset at {path}");
            return asset;
        }
    }
}
