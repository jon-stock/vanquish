using System.IO;
using UnityEditor;
using UnityEngine;
using Vanquish.Data;
using Vanquish.Data.Missiles;
using Vanquish.Data.Shared;
using Vanquish.Data.Support;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Seeds Phase 2D's one new data asset — a Tier-0 SAM site BaseDefenseDefinition —
    /// so InstallationFactory.SpawnBaseDefense/SamSiteAI have something real to spawn.
    /// No BaseDefenseDefinition asset (or any Support/ definition at all) existed
    /// before this; every other Support/ definition (LaunchPlatformDefinition,
    /// RadarInstallationDefinition) remains unseeded/unconsumed — out of scope for
    /// this archetype item, tracked separately under Phase 2F.
    /// </summary>
    public static class Phase2DSamSiteSeeder
    {
        private const string SupportDir = "Assets/_Project/Data/Support";

        [MenuItem("Vanquish/Phase 2D/Seed SAM Site Definition")]
        public static void SeedSamSiteDefinition()
        {
            EnsureDir(SupportDir);

            // Reuses the exact same Tier-0 missile parts as Phase1CombatSceneBuilder's
            // "Basic Missile" (Airframe_Basic/Engine_SolidRocket_Basic/Seeker_IR_Basic/
            // Payload_HEFrag_Small/Fuel_Solid_Basic) rather than seeding a new dedicated
            // "SAM missile" part set — this archetype item is about the site/AI/spawner
            // existing and behaving correctly, not about new missile part breadth.
            var missileLoadout = new MissileLoadout
            {
                designName = "SAM Site Missile",
                airframe = Load<MissileAirframeDefinition>("Assets/_Project/Data/Missiles/Airframe_Basic.asset"),
                engine = Load<MissileEngineDefinition>("Assets/_Project/Data/Missiles/Engine_SolidRocket_Basic.asset"),
                seeker = Load<SeekerDefinition>("Assets/_Project/Data/Missiles/Seeker_IR_Basic.asset"),
                payload = Load<MissilePayloadDefinition>("Assets/_Project/Data/Missiles/Payload_HEFrag_Small.asset"),
                fuel = Load<FuelDefinition>("Assets/_Project/Data/Shared/Fuel_Solid_Basic.asset"),
            };

            if (!missileLoadout.IsComplete)
            {
                Debug.LogError("[Phase2DSamSiteSeeder] Could not load one or more Tier-0 missile parts — " +
                    "run Vanquish/Phase 1/Seed Tier-0 Data first.");
                return;
            }

            CreateOrReplace<BaseDefenseDefinition>($"{SupportDir}/BaseDefense_SamSite_Basic.asset", d =>
            {
                d.id = "support_basedefense_samsite_basic";
                d.displayName = "Basic SAM Site";
                d.category = PartCategory.SupportBaseDefense;
                d.tier = TechTier.Tier0_Improvised;
                d.researchCost = 0;
                d.buildCost = 0;
                d.massKg = 0f; // static installation, not assembled into a mass-budgeted design
                d.defenseType = BaseDefenseType.SamSite;
                // Long engagement range and high rate of fire relative to a drone's
                // typical 250-400m engage range / 2.5s cooldown — per this item's own
                // "long engagement range, high rate of fire" description.
                d.engagementRangeMeters = 1500f;
                d.rateOfFirePerSecond = 1f; // one shot/second — WeaponController.fireCooldownSeconds = 1s
                d.interceptProbability = 0.8f; // not yet consumed by any runtime component — data for a future refinement
                d.health = 200f; // tougher than a Tier-0 drone's health, a harder target to kill outright
                d.missileLoadout = missileLoadout;
                d.ammoCount = 20;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase2DSamSiteSeeder] Seeded BaseDefense_SamSite_Basic under " +
                "Assets/_Project/Data/Support/. Not yet wired into any tech tree/Workshop placement flow " +
                "(that's Phase 2F's job) — currently only consumed by InstallationFactory/CombatTestSceneBuilder.");
        }

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                Debug.LogError($"[Phase2DSamSiteSeeder] Could not load asset at {path}");
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
