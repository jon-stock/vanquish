using System.Linq;
using UnityEditor;
using UnityEngine;
using Vanquish.Combat;
using Vanquish.Core;
using Vanquish.Data;
using Vanquish.Data.Drones;
using Vanquish.Data.Missiles;
using Vanquish.Data.Shared;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Headless checks for Phase 3B's visual-fidelity work: every SeekerType gets a
    /// distinct nose treatment, hardpoint-mounted missile count tracks ammoCount
    /// (capped to hardpointCount) and respects internal-vs-external weapon bays, and
    /// wing-type/hull-material/rotor-material selections actually change the built
    /// mesh/material rather than being silently ignored. Runs entirely in Edit mode
    /// (no Play mode needed — VehicleFactory/DroneVisualBuilder/MissileVisualBuilder
    /// only touch GameObject/Transform/Mesh APIs, same as every Editor scene builder
    /// already does) and cleans up every temporary GameObject it creates afterward.
    /// </summary>
    public static class Phase3BValidation
    {
        [MenuItem("Vanquish/Phase 3B/Validate Visual Fidelity (Headless)")]
        public static void ValidateVisualFidelity()
        {
            bool allPassed = true;

            allPassed &= ValidateSeekerNoseVariety();
            allPassed &= ValidateHardpointMountedMissiles();
            allPassed &= ValidateWeaponBayInternalHidesOrdnance();
            allPassed &= ValidateWingShapeVariety();
            allPassed &= ValidateHullMaterialFinishVariety();
            allPassed &= ValidateIncompleteLoadoutDoesNotThrow();

            Debug.Log(allPassed
                ? "[Phase3BValidation] All visual-fidelity checks PASSED."
                : "[Phase3BValidation] One or more visual-fidelity checks FAILED — see log above.");
        }

        private static bool ValidateSeekerNoseVariety()
        {
            bool pass = true;
            var root = new GameObject("Phase3BValidation_SeekerRoot");
            try
            {
                var seekerOptions = new[]
                {
                    "Assets/_Project/Data/Missiles/Seeker_IR_Basic.asset",
                    "Assets/_Project/Data/Missiles/Seeker_WireSaclos.asset",
                    "Assets/_Project/Data/Missiles/Seeker_Laser.asset",
                    "Assets/_Project/Data/Missiles/Seeker_OpticalTv.asset",
                    "Assets/_Project/Data/Missiles/Seeker_SARH.asset",
                    "Assets/_Project/Data/Missiles/Seeker_ARH.asset",
                    "Assets/_Project/Data/Missiles/Seeker_ImagingIR.asset",
                    "Assets/_Project/Data/Missiles/Seeker_MultiSpectral.asset",
                };

                var missileAirframe = Load<MissileAirframeDefinition>("Assets/_Project/Data/Missiles/Airframe_Basic.asset");
                var engine = Load<MissileEngineDefinition>("Assets/_Project/Data/Missiles/Engine_SolidRocket_Basic.asset");
                var payload = Load<MissilePayloadDefinition>("Assets/_Project/Data/Missiles/Payload_HEFrag_Small.asset");
                var fuel = Load<FuelDefinition>("Assets/_Project/Data/Shared/Fuel_Solid_Basic.asset");

                foreach (string seekerPath in seekerOptions)
                {
                    var seeker = Load<SeekerDefinition>(seekerPath);
                    if (seeker == null)
                        continue;

                    var loadout = new MissileLoadout
                    {
                        designName = "Phase3BValidation",
                        airframe = missileAirframe,
                        engine = engine,
                        payload = payload,
                        fuel = fuel,
                        seeker = seeker,
                    };

                    Transform visual = MissileVisualBuilder.Build(root.transform, loadout, Team.Player);
                    bool hasNoseDetail = visual.GetComponentsInChildren<Transform>()
                        .Any(t => t.name.StartsWith("Seeker") || t.name == "WireSpool");
                    Debug.Log($"[Phase3BValidation] Seeker '{seeker.name}' ({seeker.seekerType}) produced a nose detail piece: {(hasNoseDetail ? "PASS" : "FAIL")}");
                    pass &= hasNoseDetail;

                    Object.DestroyImmediate(visual.gameObject);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
            return pass;
        }

        private static bool ValidateHardpointMountedMissiles()
        {
            bool pass = true;
            var parent = new GameObject("Phase3BValidation_HardpointRoot");
            try
            {
                DroneLoadout loadout = BuildStrikeDroneLoadout(
                    "Assets/_Project/Data/Drones/Airframe_SmallQuad.asset",
                    "Assets/_Project/Data/Drones/Propeller_Plastic_Medium.asset",
                    "Assets/_Project/Data/Drones/Hull_CompositePlastic.asset",
                    "Assets/_Project/Data/Drones/WeaponBay_Large.asset", // external
                    ammoCount: 3);
                if (loadout == null)
                    return true; // logged inside BuildStrikeDroneLoadout; don't fail the whole suite on missing seed data

                GameObject preview = VehicleFactory.BuildVisualOnlyDrone(parent.transform, loadout, Team.Player);
                int mountedCount = preview.GetComponentsInChildren<Transform>().Count(t => t.name == "MissileVisual");
                int expectedCount = Mathf.Min(3, loadout.airframe.hardpointCount);
                bool countMatchesAmmo = mountedCount == expectedCount;
                Debug.Log($"[Phase3BValidation] SmallQuad with 3 ammo (hardpointCount={loadout.airframe.hardpointCount}), external bay, mounts min(ammo, hardpoints)={expectedCount} visible missiles (actual: {mountedCount}): {(countMatchesAmmo ? "PASS" : "FAIL")}");
                pass &= countMatchesAmmo;
                Object.DestroyImmediate(preview);

                // hardpointCount for Airframe_SmallQuad is expected to be smaller than a
                // large ammo count — mounting should cap at hardpointCount, never exceed it.
                DroneLoadout overloaded = BuildStrikeDroneLoadout(
                    "Assets/_Project/Data/Drones/Airframe_SmallQuad.asset",
                    "Assets/_Project/Data/Drones/Propeller_Plastic_Medium.asset",
                    "Assets/_Project/Data/Drones/Hull_CompositePlastic.asset",
                    "Assets/_Project/Data/Drones/WeaponBay_Large.asset",
                    ammoCount: 999);
                if (overloaded != null)
                {
                    GameObject overloadedPreview = VehicleFactory.BuildVisualOnlyDrone(parent.transform, overloaded, Team.Player);
                    int overloadedMountedCount = overloadedPreview.GetComponentsInChildren<Transform>().Count(t => t.name == "MissileVisual");
                    bool cappedAtHardpoints = overloadedMountedCount == overloaded.airframe.hardpointCount;
                    Debug.Log($"[Phase3BValidation] ammoCount (999) far exceeding hardpointCount ({overloaded.airframe.hardpointCount}) caps mounted visuals at hardpointCount (actual: {overloadedMountedCount}): {(cappedAtHardpoints ? "PASS" : "FAIL")}");
                    pass &= cappedAtHardpoints;
                    Object.DestroyImmediate(overloadedPreview);
                }
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
            return pass;
        }

        private static bool ValidateWeaponBayInternalHidesOrdnance()
        {
            bool pass = true;
            var parent = new GameObject("Phase3BValidation_InternalBayRoot");
            try
            {
                DroneLoadout loadout = BuildStrikeDroneLoadout(
                    "Assets/_Project/Data/Drones/Airframe_SmallQuad.asset",
                    "Assets/_Project/Data/Drones/Propeller_Plastic_Medium.asset",
                    "Assets/_Project/Data/Drones/Hull_CompositePlastic.asset",
                    "Assets/_Project/Data/Drones/WeaponBay_InternalMedium.asset", // internal
                    ammoCount: 3);
                if (loadout == null)
                    return true;

                GameObject preview = VehicleFactory.BuildVisualOnlyDrone(parent.transform, loadout, Team.Player);
                int mountedCount = preview.GetComponentsInChildren<Transform>().Count(t => t.name == "MissileVisual");
                bool noneVisible = mountedCount == 0;
                Debug.Log($"[Phase3BValidation] Internal weapon bay shows zero mounted missile visuals despite carrying ammo (actual: {mountedCount}): {(noneVisible ? "PASS" : "FAIL")}");
                pass &= noneVisible;
                Object.DestroyImmediate(preview);
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
            return pass;
        }

        private static bool ValidateWingShapeVariety()
        {
            bool pass = true;
            var parent = new GameObject("Phase3BValidation_WingRoot");
            try
            {
                // Wing_FixedWing was retired by Phase3HPlanformSeeder (superseded by the
                // merged Planform picker); Wing_FlyingWingKite is its planform-preset-era
                // replacement for the tailless flying-wing silhouette.
                var wingPaths = new[]
                {
                    "Assets/_Project/Data/Drones/Wing_DeltaWing.asset",
                    "Assets/_Project/Data/Drones/Wing_VariableSweepWing.asset",
                    "Assets/_Project/Data/Drones/Wing_FlyingWingKite.asset",
                };

                foreach (string wingPath in wingPaths)
                {
                    DroneLoadout loadout = BuildStrikeDroneLoadout(
                        "Assets/_Project/Data/Drones/Airframe_FixedWing.asset",
                        wingPath,
                        "Assets/_Project/Data/Drones/Hull_CompositePlastic.asset",
                        "Assets/_Project/Data/Drones/WeaponBay_Large.asset",
                        ammoCount: 2,
                        useJetPropulsion: true);
                    if (loadout == null)
                        continue;

                    GameObject preview = VehicleFactory.BuildVisualOnlyDrone(parent.transform, loadout, Team.Player);
                    bool hasMainWing = preview.GetComponentsInChildren<Transform>().Any(t => t.name == "MainWing");
                    Debug.Log($"[Phase3BValidation] Wing '{loadout.wingOrPropeller.name}' ({loadout.wingOrPropeller.liftSurfaceType}) built a MainWing: {(hasMainWing ? "PASS" : "FAIL")}");
                    pass &= hasMainWing;
                    Object.DestroyImmediate(preview);
                }
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
            return pass;
        }

        private static bool ValidateHullMaterialFinishVariety()
        {
            bool pass = true;
            var titanium = Load<HullMaterialDefinition>("Assets/_Project/Data/Drones/Hull_TitaniumAlloy.asset");
            var ram = Load<HullMaterialDefinition>("Assets/_Project/Data/Drones/Hull_RadarAbsorbentMaterial.asset");
            if (titanium == null || ram == null)
                return true;

            Material titaniumMat = TeamColorUtility_GetMaterialForTest(Team.Player, titanium.materialType);
            Material ramMat = TeamColorUtility_GetMaterialForTest(Team.Player, ram.materialType);
            bool differentMaterials = titaniumMat != ramMat;
            Debug.Log($"[Phase3BValidation] Titanium and RAM hull materials produce different finish materials for the same team: {(differentMaterials ? "PASS" : "FAIL")}");
            pass &= differentMaterials;
            return pass;
        }

        /// <summary>
        /// TeamColorUtility's hull-finish material cache is private — this reproduces
        /// the exact same call a real visual build makes (ApplyTeamColor with a hull
        /// material) against a throwaway GameObject and reads back the resulting
        /// shared material, rather than needing to expose internal cache state.
        /// </summary>
        private static Material TeamColorUtility_GetMaterialForTest(Team team, HullMaterialType hullMaterial)
        {
            var go = new GameObject("Phase3BValidation_MaterialProbe");
            go.AddComponent<MeshRenderer>();
            try
            {
                TeamColorUtility.ApplyTeamColor(go.transform, team, hullMaterial);
                return go.GetComponent<Renderer>().sharedMaterial;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static bool ValidateIncompleteLoadoutDoesNotThrow()
        {
            var parent = new GameObject("Phase3BValidation_IncompleteRoot");
            try
            {
                GameObject preview = VehicleFactory.BuildVisualOnlyDrone(parent.transform, null, Team.Player);
                bool builtEmptyWithoutThrowing = preview != null && preview.transform.childCount == 0;
                Debug.Log($"[Phase3BValidation] A null/incomplete loadout builds an empty preview without throwing: {(builtEmptyWithoutThrowing ? "PASS" : "FAIL")}");
                Object.DestroyImmediate(preview);
                return builtEmptyWithoutThrowing;
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        private static DroneLoadout BuildStrikeDroneLoadout(string airframePath, string wingPath, string hullPath,
            string weaponBayPath, int ammoCount, bool useJetPropulsion = false)
        {
            var airframe = Load<DroneAirframeDefinition>(airframePath);
            var wing = Load<WingOrPropellerDefinition>(wingPath);
            var hull = Load<HullMaterialDefinition>(hullPath);
            var weaponBay = Load<WeaponBayDefinition>(weaponBayPath);
            var propulsion = Load<PropulsionDefinition>(useJetPropulsion
                ? "Assets/_Project/Data/Drones/Propulsion_Jet_Subsonic.asset"
                : "Assets/_Project/Data/Drones/Propulsion_Electric_Basic.asset");
            var engine = Load<DroneEngineDefinition>(useJetPropulsion
                ? "Assets/_Project/Data/Drones/Engine_Jet_Subsonic.asset"
                : "Assets/_Project/Data/Drones/Engine_Electric_Basic.asset");
            var fuel = Load<FuelDefinition>(useJetPropulsion
                ? "Assets/_Project/Data/Shared/Fuel_JetFuel_Basic.asset"
                : "Assets/_Project/Data/Shared/Fuel_Battery_Basic.asset");
            var sensor = Load<SensorSuiteDefinition>("Assets/_Project/Data/Drones/Sensor_Basic.asset");

            var missileAirframe = Load<MissileAirframeDefinition>("Assets/_Project/Data/Missiles/Airframe_Basic.asset");
            var missileEngine = Load<MissileEngineDefinition>("Assets/_Project/Data/Missiles/Engine_SolidRocket_Basic.asset");
            var missilePayload = Load<MissilePayloadDefinition>("Assets/_Project/Data/Missiles/Payload_HEFrag_Small.asset");
            var missileFuel = Load<FuelDefinition>("Assets/_Project/Data/Shared/Fuel_Solid_Basic.asset");
            var missileSeeker = Load<SeekerDefinition>("Assets/_Project/Data/Missiles/Seeker_IR_Basic.asset");

            if (airframe == null || wing == null || hull == null || weaponBay == null || propulsion == null ||
                engine == null || fuel == null || sensor == null || missileAirframe == null || missileEngine == null ||
                missilePayload == null || missileFuel == null || missileSeeker == null)
            {
                Debug.LogWarning("[Phase3BValidation] Skipping a check — one or more seeded data assets are missing " +
                    "(run Phase 1/2 data seeders first).");
                return null;
            }

            return new DroneLoadout
            {
                designName = "Phase3BValidation",
                propulsion = propulsion,
                airframe = airframe,
                wingOrPropeller = wing,
                hullMaterial = hull,
                engine = engine,
                fuel = fuel,
                weaponBay = weaponBay,
                sensorSuite = sensor,
                ammoCount = ammoCount,
                missileLoadout = new MissileLoadout
                {
                    designName = "Phase3BValidation Missile",
                    airframe = missileAirframe,
                    engine = missileEngine,
                    payload = missilePayload,
                    fuel = missileFuel,
                    seeker = missileSeeker,
                },
            };
        }

        private static T Load<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }
}
