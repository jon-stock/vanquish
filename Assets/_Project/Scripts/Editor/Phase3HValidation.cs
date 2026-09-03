using System.Linq;
using UnityEditor;
using UnityEngine;
using Vanquish.Combat;
using Vanquish.Core;
using Vanquish.Data.Drones;
using Vanquish.Data.Missiles;
using Vanquish.Data.Shared;
using Vanquish.Data.TechTree;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Headless checks for the planform-preset pass: the 3 curated Airframe+Wing
    /// planform presets exist and are internally consistent, their merged TechNodes
    /// unlock both parts together with a sane prerequisite chain, their real-world-
    /// referenced visual dimensions follow the expected relative size ordering
    /// (Fighter smallest, Stealth flying-wing largest — matching Fury/Gambit/X-47B in
    /// reality), and VehicleFactory/DroneVisualBuilder can actually build a visual for
    /// each without throwing. Same pattern as Phase3BValidation/Phase2BValidation.
    /// </summary>
    public static class Phase3HValidation
    {
        private const string DronesDir = "Assets/_Project/Data/Drones";
        private const string TechDir = "Assets/_Project/Data/TechTree";

        [MenuItem("Vanquish/Phase 3H/Validate Planform Presets (Headless)")]
        public static void ValidateAll()
        {
            bool allPass = true;
            allPass &= ValidatePlanformPresetsExistAndConsistent();
            allPass &= ValidateSizeOrdering();
            allPass &= ValidateTechNodeWiring();
            allPass &= ValidateFlyingWingKiteLiftCurve();
            allPass &= ValidatePlanformVisualsBuildWithoutThrowing();
            allPass &= ValidateMissileMountScaleIsBelievable();

            Debug.Log(allPass
                ? "[Phase3HValidation] All planform-preset checks PASSED."
                : "[Phase3HValidation] One or more planform-preset checks FAILED — see log above.");

            if (!allPass)
                Debug.LogError("[Phase3HValidation] Planform-preset validation FAILED.");
        }

        private static bool ValidatePlanformPresetsExistAndConsistent()
        {
            bool pass = true;
            pass &= CheckPlanform("Planform_TwinTailFighter", DroneAirframeClass.FixedWing, LiftSurfaceType.DeltaWing);
            pass &= CheckPlanform("Planform_CrankedKiteRecon", DroneAirframeClass.CcaScale, LiftSurfaceType.VariableSweepWing);
            pass &= CheckPlanform("Planform_FlyingWingStealth", DroneAirframeClass.FlyingWingStealth, LiftSurfaceType.FlyingWing);
            return pass;
        }

        private static bool CheckPlanform(string assetName, DroneAirframeClass expectedClass, LiftSurfaceType expectedWingType)
        {
            var planform = AssetDatabase.LoadAssetAtPath<DronePlanformDefinition>($"{DronesDir}/{assetName}.asset");
            if (planform == null || planform.airframe == null || planform.wing == null)
            {
                Debug.LogError($"[Phase3HValidation] FAIL: {assetName} missing or has a null airframe/wing reference.");
                return false;
            }

            bool classOk = planform.airframe.airframeClass == expectedClass;
            bool wingOk = planform.wing.liftSurfaceType == expectedWingType;
            bool consistent = DroneCompatibility.GetFlightConfiguration(planform.airframe) == DroneCompatibility.GetFlightConfiguration(planform.wing);

            bool ok = classOk && wingOk && consistent;
            Debug.Log($"[Phase3HValidation] {assetName}: airframeClass={planform.airframe.airframeClass} " +
                $"(expect {expectedClass}), wingType={planform.wing.liftSurfaceType} (expect {expectedWingType}), " +
                $"flight-configuration-consistent={consistent}. {(ok ? "PASS" : "FAIL")}");
            return ok;
        }

        /// <summary>
        /// Real-world relative scale ordering: a Fury/YFQ-44A-class fighter is the
        /// smallest of the three real reference aircraft, a Gambit-class twin-tail
        /// recon airframe is mid-sized, and an X-47B-class flying wing is the
        /// largest — the visual dimensions seeded by Phase3HPlanformSeeder should
        /// preserve that ordering, not just be "big numbers."
        /// </summary>
        private static bool ValidateSizeOrdering()
        {
            var fighter = AssetDatabase.LoadAssetAtPath<DroneAirframeDefinition>($"{DronesDir}/Airframe_FixedWing.asset");
            var recon = AssetDatabase.LoadAssetAtPath<DroneAirframeDefinition>($"{DronesDir}/Airframe_CcaScale.asset");
            var stealth = AssetDatabase.LoadAssetAtPath<DroneAirframeDefinition>($"{DronesDir}/Airframe_FlyingWingStealth.asset");

            if (fighter == null || recon == null || stealth == null)
            {
                Debug.LogError("[Phase3HValidation] FAIL: missing one or more airframe assets for size-ordering check.");
                return false;
            }

            bool spanOrderOk = fighter.wingSpanMeters < recon.wingSpanMeters && recon.wingSpanMeters < stealth.wingSpanMeters;
            bool lengthOrderOk = fighter.fuselageLengthMeters < stealth.fuselageLengthMeters &&
                                  recon.fuselageLengthMeters < stealth.fuselageLengthMeters;
            bool allPositive = fighter.wingSpanMeters > 0f && recon.wingSpanMeters > 0f && stealth.wingSpanMeters > 0f &&
                                fighter.fuselageLengthMeters > 0f && recon.fuselageLengthMeters > 0f && stealth.fuselageLengthMeters > 0f;

            bool ok = spanOrderOk && lengthOrderOk && allPositive;
            Debug.Log($"[Phase3HValidation] Wingspan: Fighter={fighter.wingSpanMeters}m, Recon={recon.wingSpanMeters}m, " +
                $"Stealth={stealth.wingSpanMeters}m (expect Fighter < Recon < Stealth, matching real Fury < Gambit-estimate < X-47B). " +
                $"{(ok ? "PASS" : "FAIL")}");
            return ok;
        }

        private static bool ValidateTechNodeWiring()
        {
            bool pass = true;

            var fighterNode = CheckMergedTechNode("TN_3H_planform_twintailfighter", "Airframe_FixedWing", "Wing_DeltaWing");
            var reconNode = CheckMergedTechNode("TN_3H_planform_crankedkiterecon", "Airframe_CcaScale", "Wing_VariableSweepWing");
            var stealthNode = CheckMergedTechNode("TN_3H_planform_flyingwingstealth", "Airframe_FlyingWingStealth", "Wing_FlyingWingKite");
            pass &= fighterNode != null && reconNode != null && stealthNode != null;

            if (pass)
            {
                bool chainOk = reconNode.prerequisites != null && reconNode.prerequisites.Contains(fighterNode) &&
                               stealthNode.prerequisites != null && stealthNode.prerequisites.Contains(reconNode);
                Debug.Log($"[Phase3HValidation] Planform progression chain (Recon requires Fighter, Stealth requires Recon): {(chainOk ? "PASS" : "FAIL")}");
                pass &= chainOk;
            }

            // The 6 retired individual nodes should no longer exist — confirms
            // RetireOldIndividualTechNodes actually ran, not just that the new ones exist.
            string[] retiredIds =
            {
                "TN_2B_drone_airframe_fixedwing", "TN_2B_drone_airframe_flyingwingstealth", "TN_2B_drone_airframe_ccascale",
                "TN_2B_drone_wing_fixedwing", "TN_2B_drone_wing_deltawing", "TN_2B_drone_wing_variablesweepwing",
            };
            bool allRetired = true;
            foreach (var id in retiredIds)
            {
                bool stillExists = AssetDatabase.LoadAssetAtPath<TechNode>($"{TechDir}/{id}.asset") != null;
                if (stillExists)
                {
                    Debug.LogError($"[Phase3HValidation] FAIL: retired node {id} still exists on disk.");
                    allRetired = false;
                }
            }
            Debug.Log($"[Phase3HValidation] Old individual airframe/wing TechNodes correctly retired: {(allRetired ? "PASS" : "FAIL")}");
            pass &= allRetired;

            return pass;
        }

        private static TechNode CheckMergedTechNode(string nodeId, string expectedAirframeAsset, string expectedWingAsset)
        {
            var node = AssetDatabase.LoadAssetAtPath<TechNode>($"{TechDir}/{nodeId}.asset");
            if (node == null)
            {
                Debug.LogError($"[Phase3HValidation] FAIL: missing tech node {nodeId}.");
                return null;
            }

            var expectedAirframe = AssetDatabase.LoadAssetAtPath<DroneAirframeDefinition>($"{DronesDir}/{expectedAirframeAsset}.asset");
            var expectedWing = AssetDatabase.LoadAssetAtPath<WingOrPropellerDefinition>($"{DronesDir}/{expectedWingAsset}.asset");

            bool unlocksBoth = node.unlocks != null && node.unlocks.Length == 2 &&
                                node.unlocks.Contains(expectedAirframe) && node.unlocks.Contains(expectedWing);
            bool hasPrereq = node.prerequisites != null && node.prerequisites.Length > 0;

            Debug.Log($"[Phase3HValidation] {nodeId} unlocks exactly {{{expectedAirframeAsset}, {expectedWingAsset}}} " +
                $"together: {unlocksBoth}, has a prerequisite: {hasPrereq}. {(unlocksBoth && hasPrereq ? "PASS" : "FAIL")}");

            return unlocksBoth && hasPrereq ? node : null;
        }

        private static bool ValidateFlyingWingKiteLiftCurve()
        {
            var wing = AssetDatabase.LoadAssetAtPath<WingOrPropellerDefinition>($"{DronesDir}/Wing_FlyingWingKite.asset");
            if (wing == null)
            {
                Debug.LogError("[Phase3HValidation] FAIL: missing Wing_FlyingWingKite.asset.");
                return false;
            }
            bool ok = wing.criticalAoADegrees > wing.referenceAoADegrees && wing.referenceAoADegrees > wing.zeroLiftAoADegrees &&
                      wing.liftSurfaceType == LiftSurfaceType.FlyingWing;
            Debug.Log($"[Phase3HValidation] Wing_FlyingWingKite lift curve valid (zeroLift={wing.zeroLiftAoADegrees}, " +
                $"reference={wing.referenceAoADegrees}, critical={wing.criticalAoADegrees}), liftSurfaceType=FlyingWing: {(ok ? "PASS" : "FAIL")}");
            return ok;
        }

        /// <summary>Reuses Phase3BValidation's "build a real visual via VehicleFactory and check it doesn't
        /// throw / produces a MainWing" pattern, one drone per planform.</summary>
        private static bool ValidatePlanformVisualsBuildWithoutThrowing()
        {
            bool pass = true;
            var parent = new GameObject("Phase3HValidation_PlanformVisualRoot");
            try
            {
                string[] planformAssets = { "Planform_TwinTailFighter", "Planform_CrankedKiteRecon", "Planform_FlyingWingStealth" };
                foreach (var planformAsset in planformAssets)
                {
                    var planform = AssetDatabase.LoadAssetAtPath<DronePlanformDefinition>($"{DronesDir}/{planformAsset}.asset");
                    if (planform == null)
                    {
                        Debug.LogError($"[Phase3HValidation] FAIL: missing {planformAsset} for visual-build check.");
                        pass = false;
                        continue;
                    }

                    DroneLoadout loadout = BuildJetStrikeDroneLoadout(planform);
                    if (loadout == null)
                        continue; // logged inside BuildJetStrikeDroneLoadout

                    GameObject preview = VehicleFactory.BuildVisualOnlyDrone(parent.transform, loadout, Team.Player);
                    bool hasMainWing = preview.GetComponentsInChildren<Transform>().Any(t => t.name == "MainWing");
                    Debug.Log($"[Phase3HValidation] {planformAsset} built a visual with a MainWing: {(hasMainWing ? "PASS" : "FAIL")}");
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

        private static bool ValidateMissileMountScaleIsBelievable()
        {
            var planform = AssetDatabase.LoadAssetAtPath<DronePlanformDefinition>($"{DronesDir}/Planform_TwinTailFighter.asset");
            var weaponBayLarge = AssetDatabase.LoadAssetAtPath<Vanquish.Data.Drones.WeaponBayDefinition>($"{DronesDir}/WeaponBay_Large.asset");
            if (planform == null || weaponBayLarge == null)
            {
                Debug.LogError("[Phase3HValidation] FAIL: missing assets for missile-mount-scale check.");
                return false;
            }

            var parent = new GameObject("Phase3HValidation_MissileScaleRoot");
            try
            {
                DroneLoadout loadout = BuildJetStrikeDroneLoadout(planform, weaponBayLarge);
                if (loadout == null)
                    return true; // logged already

                GameObject preview = VehicleFactory.BuildVisualOnlyDrone(parent.transform, loadout, Team.Player);
                Transform missileVisual = preview.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "MissileVisual");
                if (missileVisual == null)
                {
                    Debug.LogError("[Phase3HValidation] FAIL: no mounted missile visual found on Twin-Tail Fighter preview.");
                    Object.DestroyImmediate(preview);
                    return false;
                }

                // Missile "full length" scale factor is baked into missileVisual's local
                // scale (see MissileVisualBuilder.Build's `scale` param / VehicleFactory's
                // missileMountScale) — a believable AIM-120-vs-Fury-style look is roughly
                // 30-90% of the aircraft's own fuselage length, comfortably wider than the
                // exact tuned value so this doesn't become a brittle magic-number test.
                float missileScale = missileVisual.localScale.z;
                float nominalMissileFullLength = 2f; // matches DroneVisualBuilder.ComputeMissileMountScale's own constant
                float missileLength = missileScale * nominalMissileFullLength;
                float fuselageLength = planform.airframe.fuselageLengthMeters;
                float ratio = missileLength / fuselageLength;

                bool believable = ratio > 0.25f && ratio < 1.0f;
                Debug.Log($"[Phase3HValidation] Twin-Tail Fighter mounted missile length ~{missileLength:F1}m vs " +
                    $"fuselage {fuselageLength:F1}m (ratio {ratio:P0}, expect 25%-100%, AIM-120-vs-Fury-like): {(believable ? "PASS" : "FAIL")}");

                Object.DestroyImmediate(preview);
                return believable;
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        private static DroneLoadout BuildJetStrikeDroneLoadout(DronePlanformDefinition planform, Vanquish.Data.Drones.WeaponBayDefinition weaponBayOverride = null)
        {
            var propulsion = AssetDatabase.LoadAssetAtPath<PropulsionDefinition>($"{DronesDir}/Propulsion_Jet_Subsonic.asset");
            var engine = AssetDatabase.LoadAssetAtPath<DroneEngineDefinition>($"{DronesDir}/Engine_Jet_Subsonic.asset");
            var fuel = AssetDatabase.LoadAssetAtPath<FuelDefinition>("Assets/_Project/Data/Shared/Fuel_JetFuel_Basic.asset");
            var hull = AssetDatabase.LoadAssetAtPath<HullMaterialDefinition>($"{DronesDir}/Hull_CompositePlastic.asset");
            var weaponBay = weaponBayOverride ?? AssetDatabase.LoadAssetAtPath<Vanquish.Data.Drones.WeaponBayDefinition>($"{DronesDir}/WeaponBay_Large.asset");
            var sensor = AssetDatabase.LoadAssetAtPath<SensorSuiteDefinition>($"{DronesDir}/Sensor_Basic.asset");

            var missileAirframe = AssetDatabase.LoadAssetAtPath<MissileAirframeDefinition>("Assets/_Project/Data/Missiles/Airframe_Basic.asset");
            var missileEngine = AssetDatabase.LoadAssetAtPath<MissileEngineDefinition>("Assets/_Project/Data/Missiles/Engine_SolidRocket_Basic.asset");
            var missilePayload = AssetDatabase.LoadAssetAtPath<MissilePayloadDefinition>("Assets/_Project/Data/Missiles/Payload_HEFrag_Small.asset");
            var missileFuel = AssetDatabase.LoadAssetAtPath<FuelDefinition>("Assets/_Project/Data/Shared/Fuel_Solid_Basic.asset");
            var missileSeeker = AssetDatabase.LoadAssetAtPath<SeekerDefinition>("Assets/_Project/Data/Missiles/Seeker_IR_Basic.asset");

            if (propulsion == null || engine == null || fuel == null || hull == null || weaponBay == null || sensor == null ||
                missileAirframe == null || missileEngine == null || missilePayload == null || missileFuel == null || missileSeeker == null)
            {
                Debug.LogWarning("[Phase3HValidation] Skipping a check — one or more seeded data assets are missing.");
                return null;
            }

            return new DroneLoadout
            {
                designName = "Phase3HValidation",
                propulsion = propulsion,
                airframe = planform.airframe,
                wingOrPropeller = planform.wing,
                hullMaterial = hull,
                engine = engine,
                fuel = fuel,
                weaponBay = weaponBay,
                sensorSuite = sensor,
                ammoCount = 2,
                missileLoadout = new MissileLoadout
                {
                    designName = "Phase3HValidation Missile",
                    airframe = missileAirframe,
                    engine = missileEngine,
                    payload = missilePayload,
                    fuel = missileFuel,
                    seeker = missileSeeker,
                },
            };
        }
    }
}
