using UnityEditor;
using UnityEngine;
using Vanquish.Data;
using Vanquish.Data.Missiles;
using Vanquish.Data.TechTree;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Headless sanity checks for Phase 2A part-breadth changes that don't need a
    /// scene/Play mode — just loads seeded assets and exercises DesignStatsCalculator
    /// directly, logging results for a human (or a batchmode CI log grep) to check.
    /// Run via `Unity.exe -batchmode -quit -executeMethod
    /// Vanquish.EditorTools.Phase2AValidation.ValidateTier0MissileMtow`.
    /// </summary>
    public static class Phase2AValidation
    {
        private const string MissilesDir = "Assets/_Project/Data/Missiles";
        private const string SharedDir = "Assets/_Project/Data/Shared";

        [MenuItem("Vanquish/Phase 2A/Validate Tier-0 Missile MTOW (Headless)")]
        public static void ValidateTier0MissileMtow()
        {
            var loadout = new MissileLoadout
            {
                designName = "Validation Basic Missile",
                airframe = Load<MissileAirframeDefinition>($"{MissilesDir}/Airframe_Basic.asset"),
                engine = Load<MissileEngineDefinition>($"{MissilesDir}/Engine_SolidRocket_Basic.asset"),
                seeker = Load<SeekerDefinition>($"{MissilesDir}/Seeker_IR_Basic.asset"),
                payload = Load<MissilePayloadDefinition>($"{MissilesDir}/Payload_HEFrag_Small.asset"),
                fuel = Load<Vanquish.Data.Shared.FuelDefinition>($"{SharedDir}/Fuel_Solid_Basic.asset"),
                fuelFillFraction = 1f,
            };

            if (!loadout.IsComplete)
            {
                Debug.LogError("[Phase2AValidation] FAILED: Tier-0 missile loadout incomplete — one or more " +
                    "seeded assets failed to load. Run Vanquish/Phase 1/Seed Tier-0 Data first.");
                return;
            }

            var stats = DesignStatsCalculator.Calculate(loadout);
            bool pass = Mathf.Approximately(stats.massKg, 30f) && stats.maxTakeOffMassKg > 0f && stats.isWithinMtow;

            Debug.Log($"[Phase2AValidation] Tier-0 missile @ full fuel: massKg={stats.massKg:F1}, " +
                $"maxTakeOffMassKg={stats.maxTakeOffMassKg:F1}, isWithinMtow={stats.isWithinMtow}, " +
                $"fuelMassKg={stats.fuelMassKg:F1}. {(pass ? "PASS" : "FAIL")}");

            // Also confirm the fuel-fill slider actually changes fuel mass and total mass.
            loadout.fuelFillFraction = 0f;
            var emptyStats = DesignStatsCalculator.Calculate(loadout);
            bool sliderWorks = emptyStats.fuelMassKg < stats.fuelMassKg && emptyStats.massKg < stats.massKg;
            Debug.Log($"[Phase2AValidation] Tier-0 missile @ empty fuel: massKg={emptyStats.massKg:F1}, " +
                $"fuelMassKg={emptyStats.fuelMassKg:F1}. Fuel slider affects mass: " +
                $"{(sliderWorks ? "PASS" : "FAIL")}");

            if (!pass || !sliderWorks)
                Debug.LogError("[Phase2AValidation] One or more checks FAILED — see log lines above.");
        }

        /// <summary>
        /// Headless check that Phase2AMissileBreadthSeeder.SeedTechTreeNodes produced a
        /// well-formed tech graph: every one of the 18 Phase 2A part-breadth TechNodes
        /// exists, unlocks exactly one part, and has at least one non-null prerequisite
        /// (nothing should be immediately available with zero research). Also spot-checks
        /// one multi-step progression chain (engine tree) resolves to the expected node.
        /// Run via `Unity.exe -batchmode -quit -executeMethod
        /// Vanquish.EditorTools.Phase2AValidation.ValidateMissileBreadthTechWiring`.
        /// </summary>
        [MenuItem("Vanquish/Phase 2A/Validate Missile Breadth Tech Wiring (Headless)")]
        public static void ValidateMissileBreadthTechWiring()
        {
            const string TechDir = "Assets/_Project/Data/TechTree";
            string[] nodeIds =
            {
                "TN_2A_missile_payload_grenade", "TN_2A_missile_payload_shapedcharge",
                "TN_2A_missile_payload_kinetic", "TN_2A_missile_payload_cluster",
                "TN_2A_missile_engine_liquid_basic", "TN_2A_missile_engine_ramjet_basic",
                "TN_2A_missile_engine_scramjet_basic",
                "TN_2A_missile_seeker_wire_saclos", "TN_2A_missile_seeker_laser",
                "TN_2A_missile_seeker_optical_tv", "TN_2A_missile_seeker_sarh",
                "TN_2A_missile_seeker_arh", "TN_2A_missile_seeker_imaging_ir",
                "TN_2A_missile_seeker_multispectral",
                "TN_2A_missile_countermeasure_flarechaff", "TN_2A_missile_countermeasure_rcsshaping",
                "TN_2A_missile_jamming_ecmpod", "TN_2A_missile_jamming_eccmsuite",
            };

            bool allPass = true;
            int checkedCount = 0;

            foreach (var id in nodeIds)
            {
                var node = AssetDatabase.LoadAssetAtPath<TechNode>($"{TechDir}/{id}.asset");
                if (node == null)
                {
                    Debug.LogError($"[Phase2AValidation] FAIL: missing tech node {id}. Run " +
                        "Vanquish/Phase 2A/Seed Missile Breadth Tech Nodes (after the four variant seeders).");
                    allPass = false;
                    continue;
                }

                checkedCount++;

                if (node.unlocks == null || node.unlocks.Length != 1 || node.unlocks[0] == null)
                {
                    Debug.LogError($"[Phase2AValidation] FAIL: {id} does not unlock exactly one non-null part.");
                    allPass = false;
                }

                if (node.prerequisites == null || node.prerequisites.Length == 0)
                {
                    Debug.LogError($"[Phase2AValidation] FAIL: {id} has no prerequisites — should never be free.");
                    allPass = false;
                }
                else
                {
                    foreach (var prereq in node.prerequisites)
                    {
                        if (prereq == null)
                        {
                            Debug.LogError($"[Phase2AValidation] FAIL: {id} has a null prerequisite reference.");
                            allPass = false;
                        }
                    }
                }
            }

            var scramjetNode = AssetDatabase.LoadAssetAtPath<TechNode>($"{TechDir}/TN_2A_missile_engine_scramjet_basic.asset");
            var ramjetNode = AssetDatabase.LoadAssetAtPath<TechNode>($"{TechDir}/TN_2A_missile_engine_ramjet_basic.asset");
            bool chainOk = scramjetNode != null && ramjetNode != null
                && scramjetNode.prerequisites != null && scramjetNode.prerequisites.Length == 1
                && scramjetNode.prerequisites[0] == ramjetNode;
            Debug.Log($"[Phase2AValidation] Engine progression chain (Scramjet requires Ramjet): {(chainOk ? "PASS" : "FAIL")}");
            allPass &= chainOk;

            Debug.Log($"[Phase2AValidation] Checked {checkedCount}/{nodeIds.Length} expected Phase 2A tech nodes. " +
                (allPass ? "ALL PASS" : "ONE OR MORE FAILURES ABOVE"));

            if (!allPass)
                Debug.LogError("[Phase2AValidation] Missile breadth tech wiring validation FAILED.");
        }

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                Debug.LogError($"[Phase2AValidation] Could not load asset at {path}");
            return asset;
        }
    }
}
