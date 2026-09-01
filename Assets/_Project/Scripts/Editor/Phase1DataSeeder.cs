using System.IO;
using UnityEditor;
using UnityEngine;
using Vanquish.Data;
using Vanquish.Data.Drones;
using Vanquish.Data.Missiles;
using Vanquish.Data.Shared;
using Vanquish.Data.TechTree;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Creates the Phase 1 Tier-0 part catalog and its linear ~10-node tech tree as
    /// real ScriptableObject assets under Assets/_Project/Data/. Idempotent: re-running
    /// overwrites the same asset paths rather than duplicating them, so it's safe to
    /// call again after tweaking stats here.
    /// </summary>
    public static class Phase1DataSeeder
    {
        private const string MissilesDir = "Assets/_Project/Data/Missiles";
        private const string DronesDir = "Assets/_Project/Data/Drones";
        private const string SharedDir = "Assets/_Project/Data/Shared";
        private const string TechTreeDir = "Assets/_Project/Data/TechTree";

        [MenuItem("Vanquish/Phase 1/Seed Tier-0 Data")]
        public static void SeedData()
        {
            EnsureDir(MissilesDir);
            EnsureDir(DronesDir);
            EnsureDir(SharedDir);
            EnsureDir(TechTreeDir);

            // ---- Missile parts ----
            var missileAirframe = CreateOrReplace<MissileAirframeDefinition>($"{MissilesDir}/Airframe_Basic.asset", a =>
            {
                a.id = "missile_airframe_basic";
                a.displayName = "Basic Missile Airframe";
                a.category = PartCategory.MissileAirframe;
                a.tier = TechTier.Tier0_Improvised;
                a.massKg = 2f;
                a.dragCoefficient = 0.08f;
                a.structuralMassKg = 8f;
                a.maxGForce = 25f;
                a.baseRadarCrossSection = 0.05f;
                a.maxTemperatureCelsius = 200f;
                // Fully-loaded Tier 0 missile (airframe+engine+payload+seeker+full fuel)
                // is 30kg — 40kg leaves headroom for a fuller fuel slider or a slightly
                // heavier part swap without instantly tripping the MTOW check.
                a.maxTakeOffMassKg = 40f;
            });

            var missileEngine = CreateOrReplace<MissileEngineDefinition>($"{MissilesDir}/Engine_SolidRocket_Basic.asset", e =>
            {
                e.id = "missile_engine_solid_basic";
                e.displayName = "Basic Solid Rocket Engine";
                e.category = PartCategory.MissileEngine;
                e.tier = TechTier.Tier0_Improvised;
                e.massKg = 5f;
                e.propulsionType = PropulsionType.SolidRocket;
                e.thrustNewtons = 3500f;
                e.burnTimeSeconds = 6f;
                e.maxSpeedMetersPerSecond = 250f;
                e.infraredSignature = 1.5f;
            });

            var missileSeeker = CreateOrReplace<SeekerDefinition>($"{MissilesDir}/Seeker_IR_Basic.asset", s =>
            {
                s.id = "missile_seeker_ir_basic";
                s.displayName = "Basic Infrared Seeker";
                s.category = PartCategory.MissileSeeker;
                s.tier = TechTier.Tier0_Improvised;
                s.massKg = 2f;
                s.seekerType = SeekerType.Infrared;
                s.detectionRangeMeters = 2000f;
                s.fieldOfViewDegrees = 30f;
                s.reacquisitionTimeSeconds = 1f;
                s.jamResistance = 0.2f;
                s.countermeasureSusceptibility = 0.6f;
            });

            var missilePayload = CreateOrReplace<MissilePayloadDefinition>($"{MissilesDir}/Payload_HEFrag_Small.asset", p =>
            {
                p.id = "missile_payload_hefrag_small";
                p.displayName = "Small HE-Frag Warhead";
                p.category = PartCategory.MissilePayload;
                p.tier = TechTier.Tier0_Improvised;
                p.massKg = 5f;
                p.payloadType = PayloadType.HighExplosiveFragmentation;
                p.warheadMassKg = 5f;
                p.blastRadiusMeters = 8f;
                p.directDamage = 60f;
                p.splashDamage = 25f;
                p.requiresProximityFuse = true;
            });

            var missileFuel = CreateOrReplace<FuelDefinition>($"{SharedDir}/Fuel_Solid_Basic.asset", f =>
            {
                f.id = "fuel_solid_basic";
                f.displayName = "Basic Solid Propellant";
                f.category = PartCategory.MissileFuel;
                f.tier = TechTier.Tier0_Improvised;
                f.massKg = 0f;
                f.fuelType = FuelType.SolidPropellant;
                f.energyDensityMjPerKg = 6f;
                f.capacityKg = 3f;
                f.volatility = 0.5f;
            });

            // ---- Drone parts ----
            var dronePropulsion = CreateOrReplace<PropulsionDefinition>($"{DronesDir}/Propulsion_Electric_Basic.asset", p =>
            {
                p.id = "drone_propulsion_electric_basic";
                p.displayName = "Basic Electric Propulsion";
                p.category = PartCategory.DronePropulsion;
                p.tier = TechTier.Tier0_Improvised;
                p.massKg = 3f;
                p.propulsionType = PropulsionType.Electric;
                p.maxSpeedMetersPerSecond = 40f;
                p.accelerationMetersPerSecondSquared = 8f;
                p.acousticSignature = 0.3f;
                p.infraredSignature = 0.5f;
            });

            var droneAirframe = CreateOrReplace<DroneAirframeDefinition>($"{DronesDir}/Airframe_SmallQuad.asset", a =>
            {
                a.id = "drone_airframe_smallquad";
                a.displayName = "Small Quad Airframe";
                a.category = PartCategory.DroneAirframe;
                a.tier = TechTier.Tier0_Improvised;
                a.massKg = 2f;
                a.airframeClass = DroneAirframeClass.SmallQuad;
                a.dragCoefficient = 0.15f;
                a.structuralMassKg = 6f;
                a.hardpointCount = 2;
                a.internalBayCount = 0;
                a.baseRadarCrossSection = 0.3f;
                // Phase 2B fields. rotorCount=4 matches the existing procedural quadcopter
                // visual. maxTakeOffMassKg=180 gives headroom over a fully-loaded Tier-0
                // strike drone (~141kg: ~21kg of drone parts + 4x ~30kg Tier-0 missiles)
                // without breaking the already-working Phase 1 loop — same "don't trip the
                // already-working MTOW check" precedent as 2A's Airframe_Basic.
                a.rotorCount = 4;
                a.maxTakeOffMassKg = 180f;
            });

            var droneWing = CreateOrReplace<WingOrPropellerDefinition>($"{DronesDir}/Propeller_Basic.asset", w =>
            {
                w.id = "drone_propeller_basic";
                w.displayName = "Basic Propeller Set";
                w.category = PartCategory.DroneWingOrPropeller;
                w.tier = TechTier.Tier0_Improvised;
                w.massKg = 1f;
                w.liftSurfaceType = LiftSurfaceType.Propeller;
                w.liftCoefficient = 1f;
                w.dragCoefficient = 0.05f;
                w.turnRateDegreesPerSecond = 180f;
                w.cruiseEfficiencyMultiplier = 1f;
            });

            var droneHull = CreateOrReplace<HullMaterialDefinition>($"{DronesDir}/Hull_CompositePlastic.asset", h =>
            {
                h.id = "drone_hull_composite_basic";
                h.displayName = "Composite Plastic Hull";
                h.category = PartCategory.DroneHullMaterial;
                h.tier = TechTier.Tier0_Improvised;
                h.massKg = 1f;
                h.materialType = HullMaterialType.CompositePlastic;
                h.armorRating = 5f;
                h.densityFactor = 1f;
                h.radarCrossSectionMultiplier = 1f;
                h.maxTemperatureCelsius = 150f;
            });

            var droneEngine = CreateOrReplace<DroneEngineDefinition>($"{DronesDir}/Engine_Electric_Basic.asset", e =>
            {
                e.id = "drone_engine_electric_basic";
                e.displayName = "Basic Electric Motor";
                e.category = PartCategory.DroneEngine;
                e.tier = TechTier.Tier0_Improvised;
                e.massKg = 2f;
                e.powerOutput = 1200f;
                e.consumptionRatePerSecond = 2f;
                e.infraredSignature = 0.4f;
                e.reliability = 0.95f;
            });

            var droneFuel = CreateOrReplace<FuelDefinition>($"{SharedDir}/Fuel_Battery_Basic.asset", f =>
            {
                f.id = "fuel_battery_basic";
                f.displayName = "Basic Battery Pack";
                f.category = PartCategory.DroneFuel;
                f.tier = TechTier.Tier0_Improvised;
                f.massKg = 0f;
                f.fuelType = FuelType.Battery;
                f.energyDensityMjPerKg = 0.5f;
                f.capacityKg = 4f;
                f.volatility = 0.1f;
            });

            var weaponBay = CreateOrReplace<WeaponBayDefinition>($"{DronesDir}/WeaponBay_Small.asset", w =>
            {
                w.id = "drone_weaponbay_small";
                w.displayName = "Small Weapon Bay";
                w.category = PartCategory.DroneWeaponBay;
                w.tier = TechTier.Tier0_Improvised;
                w.massKg = 1f;
                w.payloadCapacityKg = 20f;
                w.maxMunitionCount = 4;
                w.isInternal = false;
                w.cycleTimeSeconds = 2f;
            });

            var sensorBasic = CreateOrReplace<SensorSuiteDefinition>($"{DronesDir}/Sensor_Basic.asset", s =>
            {
                s.id = "drone_sensor_basic";
                s.displayName = "Basic Sensor Suite";
                s.category = PartCategory.DroneSensorSuite;
                s.tier = TechTier.Tier0_Improvised;
                s.massKg = 1f;
                s.radarRangeMeters = 1500f;
                s.radarFieldOfViewDegrees = 90f;
                s.eoIrRangeMeters = 800f;
                s.eoIrFieldOfViewDegrees = 60f;
                s.esmRangeMeters = 500f;
                s.sharesContactsWithTeam = false;
                s.datalinkRelayDelaySeconds = 0f;
            });

            var sensorScout = CreateOrReplace<SensorSuiteDefinition>($"{DronesDir}/Sensor_Scout.asset", s =>
            {
                s.id = "drone_sensor_scout";
                s.displayName = "Long-Range Scout Sensor";
                s.category = PartCategory.DroneSensorSuite;
                s.tier = TechTier.Tier0_Improvised;
                s.massKg = 1f;
                s.radarRangeMeters = 4000f;
                s.radarFieldOfViewDegrees = 360f;
                s.eoIrRangeMeters = 3000f;
                s.eoIrFieldOfViewDegrees = 360f;
                s.esmRangeMeters = 2500f;
                s.sharesContactsWithTeam = true;
                s.datalinkRelayDelaySeconds = 0.2f;
            });

            // ---- Linear tech tree (10 nodes) ----
            TechNode prev = null;
            prev = CreateTechNode("TN_01_MissileAirframe", "Missile Airframes", prev, missileAirframe);
            prev = CreateTechNode("TN_02_MissileEngine", "Solid Rocket Engines", prev, missileEngine);
            prev = CreateTechNode("TN_03_MissileSeeker", "Infrared Seekers", prev, missileSeeker);
            prev = CreateTechNode("TN_04_MissilePayload", "HE-Frag Warheads", prev, missilePayload);
            prev = CreateTechNode("TN_05_MissileFuel", "Solid Propellant", prev, missileFuel);
            prev = CreateTechNode("TN_06_DroneBasics", "Drone Airframes & Propulsion", prev, droneAirframe, dronePropulsion);
            prev = CreateTechNode("TN_07_DronePower", "Drone Motors & Batteries", prev, droneEngine, droneFuel);
            prev = CreateTechNode("TN_08_DroneStructure", "Propellers & Hull Materials", prev, droneWing, droneHull);
            prev = CreateTechNode("TN_09_DroneSystems", "Weapon Bays & Sensors", prev, weaponBay, sensorBasic);
            prev = CreateTechNode("TN_10_ScoutSensor", "Long-Range Scout Sensors", prev, sensorScout);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase1DataSeeder] Tier-0 part catalog and 10-node tech tree seeded under Assets/_Project/Data/.");
        }

        private static TechNode CreateTechNode(string assetName, string displayName, TechNode prerequisite,
            params PartDefinition[] unlocks)
        {
            return CreateOrReplace<TechNode>($"{TechTreeDir}/{assetName}.asset", n =>
            {
                n.id = assetName;
                n.displayName = displayName;
                n.tier = TechTier.Tier0_Improvised;
                n.researchCost = 50;
                n.prerequisites = prerequisite != null ? new[] { prerequisite } : new TechNode[0];
                n.unlocks = unlocks;
            });
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
