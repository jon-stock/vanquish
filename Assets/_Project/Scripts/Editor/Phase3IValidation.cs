using System.Linq;
using UnityEditor;
using UnityEngine;
using Vanquish.Data.Drones;
using Vanquish.Data.TechTree;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Headless checks for the Propulsion+Engine merge: the 4 curated propulsion
    /// packages exist and pair the expected propulsion/engine assets, the 3 merged
    /// ICE/Jet TechNodes unlock both parts together with a sane prerequisite chain,
    /// and the 6 old individually-unlockable propulsion/engine TechNodes are gone.
    /// Same pattern as Phase3HValidation.
    /// </summary>
    public static class Phase3IValidation
    {
        private const string DronesDir = "Assets/_Project/Data/Drones";
        private const string TechDir = "Assets/_Project/Data/TechTree";

        [MenuItem("Vanquish/Phase 3I/Validate Propulsion Packages (Headless)")]
        public static void ValidateAll()
        {
            bool allPass = true;
            allPass &= CheckPackage("Package_Electric", "Propulsion_Electric_Basic", "Engine_Electric_Basic");
            allPass &= CheckPackage("Package_InternalCombustion", "Propulsion_ICE_Basic", "Engine_ICE_Basic");
            allPass &= CheckPackage("Package_SubsonicJet", "Propulsion_Jet_Subsonic", "Engine_Jet_Subsonic");
            allPass &= CheckPackage("Package_SupersonicJet", "Propulsion_Jet_Supersonic", "Engine_Jet_Supersonic");
            allPass &= ValidateTechNodeWiring();

            Debug.Log(allPass
                ? "[Phase3IValidation] All propulsion-package checks PASSED."
                : "[Phase3IValidation] One or more propulsion-package checks FAILED — see log above.");
            if (!allPass)
                Debug.LogError("[Phase3IValidation] Propulsion-package validation FAILED.");
        }

        private static bool CheckPackage(string packageAsset, string expectedPropulsion, string expectedEngine)
        {
            var package = AssetDatabase.LoadAssetAtPath<DronePropulsionPackageDefinition>($"{DronesDir}/{packageAsset}.asset");
            if (package == null || package.propulsion == null || package.engine == null)
            {
                Debug.LogError($"[Phase3IValidation] FAIL: {packageAsset} missing or has a null propulsion/engine reference.");
                return false;
            }

            bool propulsionOk = package.propulsion.name == expectedPropulsion;
            bool engineOk = package.engine.name == expectedEngine;
            bool consistent = DroneCompatibility.GetFlightConfiguration(package.propulsion) == DroneCompatibility.GetFlightConfiguration(package.engine);

            bool ok = propulsionOk && engineOk && consistent;
            Debug.Log($"[Phase3IValidation] {packageAsset}: propulsion={package.propulsion.name} (expect {expectedPropulsion}), " +
                $"engine={package.engine.name} (expect {expectedEngine}), flight-configuration-consistent={consistent}. {(ok ? "PASS" : "FAIL")}");
            return ok;
        }

        private static bool ValidateTechNodeWiring()
        {
            bool pass = true;

            var tnIce = CheckMergedTechNode("TN_3I_package_ice", "Propulsion_ICE_Basic", "Engine_ICE_Basic");
            var tnSubsonic = CheckMergedTechNode("TN_3I_package_jetsubsonic", "Propulsion_Jet_Subsonic", "Engine_Jet_Subsonic");
            var tnSupersonic = CheckMergedTechNode("TN_3I_package_jetsupersonic", "Propulsion_Jet_Supersonic", "Engine_Jet_Supersonic");
            pass &= tnIce != null && tnSubsonic != null && tnSupersonic != null;

            if (pass)
            {
                bool chainOk = tnSupersonic.prerequisites != null && tnSupersonic.prerequisites.Contains(tnSubsonic);
                Debug.Log($"[Phase3IValidation] Propulsion package progression chain (Supersonic Jet requires Subsonic Jet): {(chainOk ? "PASS" : "FAIL")}");
                pass &= chainOk;
            }

            string[] retiredIds =
            {
                "TN_2B_drone_propulsion_ice_basic", "TN_2B_drone_engine_ice_basic",
                "TN_2B_drone_propulsion_jet_subsonic", "TN_2B_drone_engine_jet_subsonic",
                "TN_2B_drone_propulsion_jet_supersonic", "TN_2B_drone_engine_jet_supersonic",
            };
            bool allRetired = true;
            foreach (var id in retiredIds)
            {
                if (AssetDatabase.LoadAssetAtPath<TechNode>($"{TechDir}/{id}.asset") != null)
                {
                    Debug.LogError($"[Phase3IValidation] FAIL: retired node {id} still exists on disk.");
                    allRetired = false;
                }
            }
            Debug.Log($"[Phase3IValidation] Old individual propulsion/engine TechNodes correctly retired: {(allRetired ? "PASS" : "FAIL")}");
            pass &= allRetired;

            return pass;
        }

        private static TechNode CheckMergedTechNode(string nodeId, string expectedPropulsionAsset, string expectedEngineAsset)
        {
            var node = AssetDatabase.LoadAssetAtPath<TechNode>($"{TechDir}/{nodeId}.asset");
            if (node == null)
            {
                Debug.LogError($"[Phase3IValidation] FAIL: missing tech node {nodeId}.");
                return null;
            }

            var expectedPropulsion = AssetDatabase.LoadAssetAtPath<PropulsionDefinition>($"{DronesDir}/{expectedPropulsionAsset}.asset");
            var expectedEngine = AssetDatabase.LoadAssetAtPath<DroneEngineDefinition>($"{DronesDir}/{expectedEngineAsset}.asset");

            bool unlocksBoth = node.unlocks != null && node.unlocks.Length == 2 &&
                                node.unlocks.Contains(expectedPropulsion) && node.unlocks.Contains(expectedEngine);
            bool hasPrereq = node.prerequisites != null && node.prerequisites.Length > 0;

            Debug.Log($"[Phase3IValidation] {nodeId} unlocks exactly {{{expectedPropulsionAsset}, {expectedEngineAsset}}} " +
                $"together: {unlocksBoth}, has a prerequisite: {hasPrereq}. {(unlocksBoth && hasPrereq ? "PASS" : "FAIL")}");

            return unlocksBoth && hasPrereq ? node : null;
        }
    }
}
