using System.IO;
using UnityEditor;
using UnityEngine;
using Vanquish.Data;
using Vanquish.Data.Support;
using Vanquish.Data.TechTree;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Seeds Phase 2C's one new data asset — a datalink network part enabling
    /// mid-course guidance updates (see DatalinkMidCourseGuidance) — plus the
    /// TechNode gating it. Everything else in Phase 2C (Proportional Navigation,
    /// probabilistic detection, jamming, countermeasure decoys) is pure runtime
    /// behavior built on data that Phase 1/2A already seeded (SeekerDefinition,
    /// JammingDefinition, CountermeasureDefinition), so this is a much smaller
    /// seeder than 2A/2B's — most of 2C's "content" is code, not assets.
    ///
    /// The drone-side countermeasure picker slot (Phase 2C — see
    /// WorkshopController.droneCountermeasureOptions) deliberately reuses the exact
    /// same CountermeasureDefinition assets 2A already seeded for the missile slot
    /// (Countermeasure_FlareChaffDispenser/RcsShaping under Assets/_Project/Data/Missiles/)
    /// rather than duplicating them — decoy/stealth equipment is equally valid on
    /// either loadout, and PlayerProgress.IsPartUnlocked works correctly against a
    /// single shared asset referenced from two different option arrays (unlocking it
    /// once unlocks it for both the missile and drone pickers).
    /// </summary>
    public static class Phase2CGuidanceDepthSeeder
    {
        private const string SupportDir = "Assets/_Project/Data/Support";
        private const string TechTreeDir = "Assets/_Project/Data/TechTree";

        [MenuItem("Vanquish/Phase 2C/Seed Datalink Network")]
        public static void SeedDatalinkNetwork()
        {
            EnsureDir(SupportDir);

            CreateOrReplace<DatalinkNetworkDefinition>($"{SupportDir}/Datalink_MidCourseRelay.asset", d =>
            {
                d.id = "support_datalink_midcourserelay";
                d.displayName = "Mid-Course Relay Datalink";
                d.category = PartCategory.SupportDatalink;
                d.tier = TechTier.Tier2_Advanced;
                d.researchCost = 180;
                d.buildCost = 120;
                d.massKg = 1.5f;
                d.rangeMeters = 8000f;
                d.jamResistance = 0.4f;
                d.supportsMidCourseUpdates = true;
                d.supportsSeekerHandoff = true;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase2CGuidanceDepthSeeder] Seeded Datalink_MidCourseRelay under " +
                "Assets/_Project/Data/Support/. Not yet wired into the tech tree — run " +
                "Vanquish/Phase 2C/Seed Datalink Tech Node next.");
        }

        [MenuItem("Vanquish/Phase 2C/Seed Datalink Tech Node")]
        public static void SeedDatalinkTechNode()
        {
            EnsureDir(TechTreeDir);

            var datalink = AssetDatabase.LoadAssetAtPath<DatalinkNetworkDefinition>($"{SupportDir}/Datalink_MidCourseRelay.asset");
            if (datalink == null)
            {
                Debug.LogError("[Phase2CGuidanceDepthSeeder] Could not load Datalink_MidCourseRelay — run " +
                    "Vanquish/Phase 2C/Seed Datalink Network first.");
                return;
            }

            // No dedicated Phase 1 "support"/datalink base tech node exists (this is a
            // new category), so gate it behind the missile engine node directly —
            // same "no perfect-fit base node, use the closest existing one" approach
            // 2A/2B used for their own new categories (countermeasures/jamming gated
            // behind the missile airframe node, drone countermeasure behind... this is
            // its own first new-category node here).
            var tnMissileEngine = AssetDatabase.LoadAssetAtPath<TechNode>($"{TechTreeDir}/TN_02_MissileEngine.asset");
            if (tnMissileEngine == null)
            {
                Debug.LogError("[Phase2CGuidanceDepthSeeder] Missing TN_02_MissileEngine — run " +
                    "Vanquish/Phase 1/Seed Tier-0 Data first.");
                return;
            }

            string nodeId = $"TN_2C_{datalink.id}";
            CreateOrReplace<TechNode>($"{TechTreeDir}/{nodeId}.asset", n =>
            {
                n.id = nodeId;
                n.displayName = datalink.displayName;
                n.tier = datalink.tier;
                n.researchCost = datalink.researchCost;
                n.prerequisites = new[] { tnMissileEngine };
                n.unlocks = new PartDefinition[] { datalink };
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase2CGuidanceDepthSeeder] Seeded TN_2C_support_datalink_midcourserelay. Re-run " +
                "Vanquish/Phase 1/Build Workshop Scene to pick it up in the tech tree and Datalink picker row.");
        }

        [MenuItem("Vanquish/Phase 2C/Seed All Guidance Depth Data")]
        public static void SeedAll()
        {
            SeedDatalinkNetwork();
            SeedDatalinkTechNode();
            Debug.Log("[Phase2CGuidanceDepthSeeder] All Phase 2C data seeded.");
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
