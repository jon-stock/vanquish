using UnityEditor;
using UnityEngine;
using Vanquish.Data;
using Vanquish.Data.Drones;
using Vanquish.Data.TechTree;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Depth pass: merges Propulsion+Engine into curated "Propulsion Package"
    /// presets (see DronePropulsionPackageDefinition's own doc comment for why —
    /// direct user feedback: "propulsion and engine are the same: one can go").
    ///
    /// Electric (Tier 0) keeps its existing two-node unlock path (TN_06_DroneBasics
    /// unlocks the starter airframe alongside Propulsion_Electric_Basic;
    /// TN_07_DronePower unlocks Engine_Electric_Basic alongside the starter
    /// battery fuel) — those two nodes are already bundled with other, unrelated
    /// Tier-0 parts, so retiring them isn't worth the churn to the starting tech
    /// tree; the package simply treats "both halves unlocked" as its unlock
    /// condition regardless of which node(s) that came from (see
    /// WorkshopController.IsPropulsionPackageUnlocked). ICE/Jet Subsonic/Jet
    /// Supersonic each previously had two INDEPENDENT dedicated TechNodes (one for
    /// the PropulsionDefinition, one for the DroneEngineDefinition) that unlocked
    /// nothing else — those are retired here in favor of one merged node per tier,
    /// matching "a propulsion system and its engine are one integrated purchase."
    /// </summary>
    public static class Phase3IPropulsionMergeSeeder
    {
        private const string DronesDir = "Assets/_Project/Data/Drones";
        private const string TechTreeDir = "Assets/_Project/Data/TechTree";

        [MenuItem("Vanquish/Phase 3I/Seed Propulsion Packages")]
        public static void SeedAll()
        {
            SeedPackagePresets();
            RetireOldIndividualTechNodes();
            SeedPackageTechNodes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase3IPropulsionMergeSeeder] Seeded 4 propulsion packages (Electric/ICE/Subsonic Jet/" +
                "Supersonic Jet), retired the 3 pairs of individually-unlockable ICE/Jet propulsion+engine " +
                "TechNodes in favor of 3 merged package nodes, and wired the tech tree. Re-run " +
                "Vanquish/Phase 1/Build Workshop Scene to pick these up.");
        }

        [MenuItem("Vanquish/Phase 3I/Seed Propulsion Package Preset Assets")]
        public static void SeedPackagePresets()
        {
            var electricPropulsion = Load<PropulsionDefinition>("Propulsion_Electric_Basic");
            var electricEngine = Load<DroneEngineDefinition>("Engine_Electric_Basic");
            var icePropulsion = Load<PropulsionDefinition>("Propulsion_ICE_Basic");
            var iceEngine = Load<DroneEngineDefinition>("Engine_ICE_Basic");
            var jetSubsonicPropulsion = Load<PropulsionDefinition>("Propulsion_Jet_Subsonic");
            var jetSubsonicEngine = Load<DroneEngineDefinition>("Engine_Jet_Subsonic");
            var jetSupersonicPropulsion = Load<PropulsionDefinition>("Propulsion_Jet_Supersonic");
            var jetSupersonicEngine = Load<DroneEngineDefinition>("Engine_Jet_Supersonic");

            if (electricPropulsion == null || electricEngine == null || icePropulsion == null || iceEngine == null ||
                jetSubsonicPropulsion == null || jetSubsonicEngine == null || jetSupersonicPropulsion == null || jetSupersonicEngine == null)
            {
                Debug.LogError("[Phase3IPropulsionMergeSeeder] Missing one or more source propulsion/engine assets — " +
                    "run Vanquish/Phase 1/Seed Tier-0 Data and Vanquish/Phase 2B/Seed Drone Propulsion Engine Fuel Variants first.");
                return;
            }

            CreateOrReplace<DronePropulsionPackageDefinition>($"{DronesDir}/Package_Electric.asset", p =>
            {
                p.id = "drone_propulsion_package_electric";
                p.displayName = "Electric Propulsion Package";
                p.description = "Omnidirectional multirotor propulsion — quiet, low IR signature, no fuel to run dry, but the slowest of the four.";
                p.propulsion = electricPropulsion;
                p.engine = electricEngine;
            });

            CreateOrReplace<DronePropulsionPackageDefinition>($"{DronesDir}/Package_InternalCombustion.asset", p =>
            {
                p.id = "drone_propulsion_package_ice";
                p.displayName = "Internal Combustion Package";
                p.description = "Petrol/diesel piston propulsion — much longer endurance per kg of fuel than electric, at a noise/IR signature cost.";
                p.propulsion = icePropulsion;
                p.engine = iceEngine;
            });

            CreateOrReplace<DronePropulsionPackageDefinition>($"{DronesDir}/Package_SubsonicJet.asset", p =>
            {
                p.id = "drone_propulsion_package_jet_subsonic";
                p.displayName = "Subsonic Jet Package";
                p.description = "Fixed-wing turbofan propulsion — real cruise speed and altitude ceiling, but needs forward airspeed to fly at all.";
                p.propulsion = jetSubsonicPropulsion;
                p.engine = jetSubsonicEngine;
            });

            CreateOrReplace<DronePropulsionPackageDefinition>($"{DronesDir}/Package_SupersonicJet.asset", p =>
            {
                p.id = "drone_propulsion_package_jet_supersonic";
                p.displayName = "Supersonic Jet Package";
                p.description = "Afterburning turbojet propulsion — the fastest package by far, at the highest fuel consumption and IR signature.";
                p.propulsion = jetSupersonicPropulsion;
                p.engine = jetSupersonicEngine;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase3IPropulsionMergeSeeder] Seeded 4 DronePropulsionPackageDefinition presets under Assets/_Project/Data/Drones/.");
        }

        [MenuItem("Vanquish/Phase 3I/Retire Old Individual Propulsion-Engine Tech Nodes")]
        public static void RetireOldIndividualTechNodes()
        {
            string[] staleNodeIds =
            {
                "TN_2B_drone_propulsion_ice_basic",
                "TN_2B_drone_engine_ice_basic",
                "TN_2B_drone_propulsion_jet_subsonic",
                "TN_2B_drone_engine_jet_subsonic",
                "TN_2B_drone_propulsion_jet_supersonic",
                "TN_2B_drone_engine_jet_supersonic",
            };

            foreach (var nodeId in staleNodeIds)
            {
                string path = $"{TechTreeDir}/{nodeId}.asset";
                if (AssetDatabase.LoadAssetAtPath<TechNode>(path) != null)
                    AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase3IPropulsionMergeSeeder] Retired 6 old individual propulsion/engine TechNodes.");
        }

        [MenuItem("Vanquish/Phase 3I/Seed Propulsion Package Tech Nodes")]
        public static void SeedPackageTechNodes()
        {
            var tnDroneBasics = AssetDatabase.LoadAssetAtPath<TechNode>($"{TechTreeDir}/TN_06_DroneBasics.asset");
            if (tnDroneBasics == null)
            {
                Debug.LogError("[Phase3IPropulsionMergeSeeder] Missing TN_06_DroneBasics — run Vanquish/Phase 1/Seed Tier-0 Data first.");
                return;
            }

            var icePropulsion = Load<PropulsionDefinition>("Propulsion_ICE_Basic");
            var iceEngine = Load<DroneEngineDefinition>("Engine_ICE_Basic");
            var jetSubsonicPropulsion = Load<PropulsionDefinition>("Propulsion_Jet_Subsonic");
            var jetSubsonicEngine = Load<DroneEngineDefinition>("Engine_Jet_Subsonic");
            var jetSupersonicPropulsion = Load<PropulsionDefinition>("Propulsion_Jet_Supersonic");
            var jetSupersonicEngine = Load<DroneEngineDefinition>("Engine_Jet_Supersonic");

            // ICE branches directly off the Tier-0 drone basics node (an alternative
            // early propulsion philosophy, not an upgrade of electric); Subsonic Jet
            // also branches directly off it; Supersonic Jet upgrades from Subsonic Jet
            // — matching the prerequisite shape the two retired individual propulsion
            // nodes already had.
            var tnIce = CreatePackageTechNode("TN_3I_package_ice", "Internal Combustion Package",
                TechTier.Tier1_Guided, 130, new[] { tnDroneBasics }, icePropulsion, iceEngine);
            var tnSubsonicJet = CreatePackageTechNode("TN_3I_package_jetsubsonic", "Subsonic Jet Package",
                TechTier.Tier2_Advanced, 180, new[] { tnDroneBasics }, jetSubsonicPropulsion, jetSubsonicEngine);
            CreatePackageTechNode("TN_3I_package_jetsupersonic", "Supersonic Jet Package",
                TechTier.Tier4_Hypersonic, 340, new[] { tnSubsonicJet }, jetSupersonicPropulsion, jetSupersonicEngine);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase3IPropulsionMergeSeeder] Seeded 3 merged propulsion package TechNodes under Assets/_Project/Data/TechTree/.");
        }

        private static TechNode CreatePackageTechNode(string nodeId, string displayName, TechTier tier, int researchCost,
            TechNode[] prerequisites, PropulsionDefinition propulsion, DroneEngineDefinition engine)
        {
            if (propulsion == null || engine == null)
            {
                Debug.LogError($"[Phase3IPropulsionMergeSeeder] Cannot create {nodeId} — missing propulsion or engine asset.");
                return null;
            }

            return CreateOrReplace<TechNode>($"{TechTreeDir}/{nodeId}.asset", n =>
            {
                n.id = nodeId;
                n.displayName = displayName;
                n.tier = tier;
                n.researchCost = researchCost;
                n.prerequisites = prerequisites ?? new TechNode[0];
                n.unlocks = new PartDefinition[] { propulsion, engine };
            });
        }

        private static T Load<T>(string assetName) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>($"{DronesDir}/{assetName}.asset");
            if (asset == null)
                Debug.LogError($"[Phase3IPropulsionMergeSeeder] Could not load {assetName}.");
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
    }
}
