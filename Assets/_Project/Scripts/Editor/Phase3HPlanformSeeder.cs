using System.IO;
using UnityEditor;
using UnityEngine;
using Vanquish.Data;
using Vanquish.Data.Drones;
using Vanquish.Data.TechTree;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Planform-preset pass: merges Airframe+Wing into curated, real-world-referenced
    /// "Planform" presets (see DronePlanformDefinition's own doc comment for why),
    /// one per reference silhouette the user supplied:
    ///   - Flying-Wing Stealth (X-47B-class): Airframe_FlyingWingStealth (retuned to
    ///     real X-47B-scaled dimensions) + a new dedicated Wing_FlyingWingKite asset
    ///     (LiftSurfaceType.FlyingWing — a real cranked/double-delta mesh, not a
    ///     plain triangle).
    ///   - Twin-Tail Fighter (Fury/YFQ-44A/"Brontanax"-class CCA): Airframe_FixedWing
    ///     (retuned to the real disclosed Fury dimensions, 20ft/17ft) + the existing
    ///     Wing_DeltaWing asset (reused, not replaced).
    ///   - Cranked-Kite Recon (Gambit-class): Airframe_CcaScale (retuned to an
    ///     estimated between-Fury-and-X-47B scale — Gambit's exact dimensions aren't
    ///     public) + the existing Wing_VariableSweepWing asset (reused).
    ///
    /// Retires the six individual per-airframe/per-wing TechNodes the previous
    /// fixed-wing-flight-model-rework pass created (TN_2B_drone_airframe_fixedwing/
    /// flyingwingstealth/ccascale, TN_2B_drone_wing_fixedwing/deltawing/
    /// variablesweepwing) in favor of three merged TechNodes — one per planform,
    /// each unlocking its airframe+wing pair together as a single research purchase,
    /// matching "an aircraft's fuselage and wing are one integrated design, not two
    /// separate purchases." Wing_FixedWing.asset (the plain straight wing) becomes
    /// unused by any of the three curated planforms and is deleted along with its
    /// TechNode, rather than left as orphaned dead content.
    /// </summary>
    public static class Phase3HPlanformSeeder
    {
        private const string DronesDir = "Assets/_Project/Data/Drones";
        private const string TechTreeDir = "Assets/_Project/Data/TechTree";

        [MenuItem("Vanquish/Phase 3H/Seed Planform Presets")]
        public static void SeedAll()
        {
            RetuneAirframeDimensions();
            SeedFlyingWingKiteWing();
            SeedPlanformPresets();
            RetireOldIndividualTechNodes();
            SeedPlanformTechNodes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase3HPlanformSeeder] Seeded 3 planform presets (Flying-Wing Stealth / Twin-Tail Fighter / " +
                "Cranked-Kite Recon), retuned their airframes' real-world visual dimensions, and wired 3 merged " +
                "TechNodes under Assets/_Project/Data/TechTree/. Re-run Vanquish/Phase 1/Build Workshop Scene to " +
                "pick these up.");
        }

        /// <summary>
        /// Real-world-referenced visual dimensions (DroneAirframeDefinition.
        /// wingSpanMeters/fuselageLengthMeters — visual only, does not touch mass/
        /// drag/MTOW/hardpoint stats, which stay within this game's existing Tier
        /// balance envelope). Fury's are the real disclosed dimensions (20ft/17ft);
        /// X-47B's are the real dimensions scaled by ~0.74x (still unmistakably the
        /// largest of the three, matching reality, without making the in-game
        /// aircraft absurdly large relative to existing ~600-1200m arenas); Gambit's
        /// are an estimate (General Atomics hasn't published exact figures) sized
        /// between the other two, since Gambit is marketed as a similarly-attritable
        /// CCA-class twin-tail airframe, not a full-size UCAV like the X-47B.
        /// </summary>
        [MenuItem("Vanquish/Phase 3H/Retune Airframe Visual Dimensions")]
        public static void RetuneAirframeDimensions()
        {
            SetDimensions("Airframe_FixedWing", wingSpanMeters: 5.2f, fuselageLengthMeters: 6.1f); // real Fury/YFQ-44A: 17ft/20ft
            SetDimensions("Airframe_CcaScale", wingSpanMeters: 9.5f, fuselageLengthMeters: 8f); // Gambit-class estimate
            SetDimensions("Airframe_FlyingWingStealth", wingSpanMeters: 14f, fuselageLengthMeters: 9f); // X-47B real 18.92m/11.63m, scaled ~0.74x

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase3HPlanformSeeder] Retuned Airframe_FixedWing/CcaScale/FlyingWingStealth visual dimensions.");
        }

        private static void SetDimensions(string assetName, float wingSpanMeters, float fuselageLengthMeters)
        {
            var airframe = AssetDatabase.LoadAssetAtPath<DroneAirframeDefinition>($"{DronesDir}/{assetName}.asset");
            if (airframe == null)
            {
                Debug.LogError($"[Phase3HPlanformSeeder] Could not load {assetName} — run Vanquish/Phase 2B/Seed Drone Airframe Variants first.");
                return;
            }
            airframe.wingSpanMeters = wingSpanMeters;
            airframe.fuselageLengthMeters = fuselageLengthMeters;
            EditorUtility.SetDirty(airframe);
        }

        [MenuItem("Vanquish/Phase 3H/Seed Flying-Wing Kite Wing Asset")]
        public static void SeedFlyingWingKiteWing()
        {
            EnsureDir(DronesDir);

            // Broad, low-aspect-ratio, high-criticalAoA (vortex-lift-like, same
            // rationale as DeltaWing's own high critical angle from the fixed-wing
            // flight-model rework) — the X-47B-class planform is the biggest/
            // broadest of the three, so it gets the most generous lift/stall margin.
            CreateOrReplace<WingOrPropellerDefinition>($"{DronesDir}/Wing_FlyingWingKite.asset", w =>
            {
                w.id = "drone_wing_flyingwingkite";
                w.displayName = "Flying-Wing Kite Planform";
                w.category = PartCategory.DroneWingOrPropeller;
                w.tier = TechTier.Tier3_Stealth;
                w.researchCost = 0; // unlocked as part of the merged planform TechNode below, not separately
                w.buildCost = 170;
                w.massKg = 7f;
                w.liftSurfaceType = LiftSurfaceType.FlyingWing;
                w.liftCoefficient = 1.35f;
                w.dragCoefficient = 0.032f;
                w.turnRateDegreesPerSecond = 70f;
                w.cruiseEfficiencyMultiplier = 1.3f;
                w.zeroLiftAoADegrees = -2f;
                w.referenceAoADegrees = 6f;
                w.criticalAoADegrees = 24f;
                w.inducedDragFactor = 0.016f;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase3HPlanformSeeder] Seeded Wing_FlyingWingKite.");
        }

        [MenuItem("Vanquish/Phase 3H/Seed Planform Preset Assets")]
        public static void SeedPlanformPresets()
        {
            EnsureDir(DronesDir);

            var fixedWingAirframe = Load<DroneAirframeDefinition>("Airframe_FixedWing");
            var ccaScaleAirframe = Load<DroneAirframeDefinition>("Airframe_CcaScale");
            var flyingWingAirframe = Load<DroneAirframeDefinition>("Airframe_FlyingWingStealth");
            var deltaWing = Load<WingOrPropellerDefinition>("Wing_DeltaWing");
            var variableSweepWing = Load<WingOrPropellerDefinition>("Wing_VariableSweepWing");
            var flyingWingKite = Load<WingOrPropellerDefinition>("Wing_FlyingWingKite");

            if (fixedWingAirframe == null || ccaScaleAirframe == null || flyingWingAirframe == null ||
                deltaWing == null || variableSweepWing == null || flyingWingKite == null)
            {
                Debug.LogError("[Phase3HPlanformSeeder] Missing one or more source assets — run the Phase 2B " +
                    "airframe/wing seeders and Vanquish/Phase 3H/Seed Flying-Wing Kite Wing Asset first.");
                return;
            }

            CreateOrReplace<DronePlanformDefinition>($"{DronesDir}/Planform_TwinTailFighter.asset", p =>
            {
                p.id = "drone_planform_twintailfighter";
                p.displayName = "Twin-Tail Fighter Planform";
                p.description = "A slender, chined single-body fighter-class CCA airframe with a clipped-delta " +
                    "wing and a canted twin tail — small and cheap enough to be genuinely attritable.";
                p.airframe = fixedWingAirframe;
                p.wing = deltaWing;
            });

            CreateOrReplace<DronePlanformDefinition>($"{DronesDir}/Planform_CrankedKiteRecon.asset", p =>
            {
                p.id = "drone_planform_crankedkiterecon";
                p.displayName = "Cranked-Kite Recon Planform";
                p.description = "A broader twin-tail blended-wing-body airframe between the fighter and the " +
                    "flying wing in scale — a modular recon/strike CCA hull with a swept cranked wing.";
                p.airframe = ccaScaleAirframe;
                p.wing = variableSweepWing;
            });

            CreateOrReplace<DronePlanformDefinition>($"{DronesDir}/Planform_FlyingWingStealth.asset", p =>
            {
                p.id = "drone_planform_flyingwingstealth";
                p.displayName = "Flying-Wing Stealth Planform";
                p.description = "The largest of the three: a fully tailless, low-observable flying wing with a " +
                    "broad cranked-kite planform and internal weapon bays — no separate fuselage or tail at all.";
                p.airframe = flyingWingAirframe;
                p.wing = flyingWingKite;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase3HPlanformSeeder] Seeded 3 DronePlanformDefinition presets under Assets/_Project/Data/Drones/.");
        }

        /// <summary>
        /// Deletes the six individual TechNodes the fixed-wing-flight-model-rework
        /// pass created for these same airframes/wings — each planform is now unlocked
        /// as a single merged purchase (see SeedPlanformTechNodes) instead of two
        /// separate ones. Also deletes Wing_FixedWing.asset + its TechNode: the plain
        /// straight wing isn't used by any of the three curated planforms and would
        /// otherwise be dead, unreachable content (nothing in the Workshop can select
        /// it once Fixed-Wing mode only shows the merged Planform dropdown).
        /// </summary>
        [MenuItem("Vanquish/Phase 3H/Retire Old Individual Airframe-Wing Tech Nodes")]
        public static void RetireOldIndividualTechNodes()
        {
            string[] staleNodeIds =
            {
                "TN_2B_drone_airframe_fixedwing",
                "TN_2B_drone_airframe_flyingwingstealth",
                "TN_2B_drone_airframe_ccascale",
                "TN_2B_drone_wing_fixedwing",
                "TN_2B_drone_wing_deltawing",
                "TN_2B_drone_wing_variablesweepwing",
            };

            foreach (var nodeId in staleNodeIds)
            {
                string path = $"{TechTreeDir}/{nodeId}.asset";
                if (AssetDatabase.LoadAssetAtPath<TechNode>(path) != null)
                    AssetDatabase.DeleteAsset(path);
            }

            string staleWingPath = $"{DronesDir}/Wing_FixedWing.asset";
            if (AssetDatabase.LoadAssetAtPath<WingOrPropellerDefinition>(staleWingPath) != null)
                AssetDatabase.DeleteAsset(staleWingPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase3HPlanformSeeder] Retired 6 old individual airframe/wing TechNodes and the now-unused Wing_FixedWing asset.");
        }

        [MenuItem("Vanquish/Phase 3H/Seed Planform Tech Nodes")]
        public static void SeedPlanformTechNodes()
        {
            EnsureDir(TechTreeDir);

            var tnDroneBasics = AssetDatabase.LoadAssetAtPath<TechNode>($"{TechTreeDir}/TN_06_DroneBasics.asset");
            if (tnDroneBasics == null)
            {
                Debug.LogError("[Phase3HPlanformSeeder] Missing TN_06_DroneBasics — run Vanquish/Phase 1/Seed Tier-0 Data first.");
                return;
            }

            var fighterAirframe = Load<DroneAirframeDefinition>("Airframe_FixedWing");
            var fighterWing = Load<WingOrPropellerDefinition>("Wing_DeltaWing");
            var reconAirframe = Load<DroneAirframeDefinition>("Airframe_CcaScale");
            var reconWing = Load<WingOrPropellerDefinition>("Wing_VariableSweepWing");
            var stealthAirframe = Load<DroneAirframeDefinition>("Airframe_FlyingWingStealth");
            var stealthWing = Load<WingOrPropellerDefinition>("Wing_FlyingWingKite");

            // Twin-Tail Fighter branches directly off the Tier-0 drone basics node
            // (Tier 1, an alternative early airframe philosophy alongside the
            // multirotor line, not an upgrade of it). Cranked-Kite Recon upgrades
            // from the fighter; Flying-Wing Stealth (the largest/most advanced of the
            // three) upgrades from the recon planform — matching their real-world
            // relative scale/complexity ordering.
            var tnFighter = CreatePlanformTechNode("TN_3H_planform_twintailfighter", "Twin-Tail Fighter Planform",
                TechTier.Tier1_Guided, 260, new TechNode[] { tnDroneBasics }, fighterAirframe, fighterWing);
            var tnRecon = CreatePlanformTechNode("TN_3H_planform_crankedkiterecon", "Cranked-Kite Recon Planform",
                TechTier.Tier2_Advanced, 420, new[] { tnFighter }, reconAirframe, reconWing);
            CreatePlanformTechNode("TN_3H_planform_flyingwingstealth", "Flying-Wing Stealth Planform",
                TechTier.Tier3_Stealth, 620, new[] { tnRecon }, stealthAirframe, stealthWing);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase3HPlanformSeeder] Seeded 3 merged planform TechNodes under Assets/_Project/Data/TechTree/.");
        }

        private static TechNode CreatePlanformTechNode(string nodeId, string displayName, TechTier tier, int researchCost,
            TechNode[] prerequisites, DroneAirframeDefinition airframe, WingOrPropellerDefinition wing)
        {
            if (airframe == null || wing == null)
            {
                Debug.LogError($"[Phase3HPlanformSeeder] Cannot create {nodeId} — missing airframe or wing asset.");
                return null;
            }

            return CreateOrReplace<TechNode>($"{TechTreeDir}/{nodeId}.asset", n =>
            {
                n.id = nodeId;
                n.displayName = displayName;
                n.tier = tier;
                n.researchCost = researchCost;
                n.prerequisites = prerequisites ?? new TechNode[0];
                n.unlocks = new PartDefinition[] { airframe, wing };
            });
        }

        private static T Load<T>(string assetName) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>($"{DronesDir}/{assetName}.asset");
            if (asset == null)
                Debug.LogError($"[Phase3HPlanformSeeder] Could not load {assetName}.");
            return asset;
        }

        private static T CreateOrReplace<T>(string path, System.Action<T> configure) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                configure(existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            configure(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureDir(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
                string leaf = Path.GetFileName(path);
                if (!AssetDatabase.IsValidFolder(parent))
                    EnsureDir(parent);
                AssetDatabase.CreateFolder(parent, leaf);
            }
        }
    }
}
