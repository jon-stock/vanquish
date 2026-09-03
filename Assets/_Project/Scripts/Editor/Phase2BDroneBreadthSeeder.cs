using System.IO;
using UnityEditor;
using UnityEngine;
using Vanquish.Data;
using Vanquish.Data.Drones;
using Vanquish.Data.Shared;
using Vanquish.Data.TechTree;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Seeds Phase 2B drone part breadth (airframe classes, rotor material/size
    /// combinations, wing types, hull materials, jet/ICE propulsion & fuel, weapon bay
    /// variants) as real ScriptableObject assets under Assets/_Project/Data/Drones/ (and
    /// Assets/_Project/Data/Shared/ for fuel, matching Phase1DataSeeder's convention),
    /// alongside (not replacing) the Tier-0 catalog Phase1DataSeeder creates. Idempotent
    /// like the Phase 1/2A seeders: re-running overwrites the same asset paths rather
    /// than duplicating them.
    ///
    /// Data-only, same convention as Phase2AMissileBreadthSeeder: these assets are not
    /// wired into any TechNode or into WorkshopController's part picker. Phase 2B's
    /// PLAN.md checklist (unlike 2A's) does not itself require Workshop picker wiring —
    /// its exit criteria only asks that every PartCategory.Drone* value has real assets
    /// and that a fixed-wing jet drone / electric quadcopter both exist and fly per
    /// their own propulsion model, both satisfied by this seeder plus the
    /// VehicleFactory/DesignStatsCalculator changes made alongside it. Wiring these into
    /// TechNodes/a Workshop drone picker (mirroring 2A's missile picker, whose
    /// infrastructure — WorkshopController.BuildPartSlotRow&lt;T&gt; — already generalizes
    /// to any PartDefinition) is a natural follow-up, not required by this sub-milestone.
    /// </summary>
    public static class Phase2BDroneBreadthSeeder
    {
        private const string DronesDir = "Assets/_Project/Data/Drones";
        private const string SharedDir = "Assets/_Project/Data/Shared";
        private const string TechTreeDir = "Assets/_Project/Data/TechTree";

        [MenuItem("Vanquish/Phase 2B/Seed Drone Airframe Variants")]
        public static void SeedAirframeVariants()
        {
            EnsureDir(DronesDir);

            // Hexacopter: quadcopter->hexacopter upgrade path. More rotors mounted means
            // paying their individual mass multiple times (heavier structuralMassKg and
            // rotor count than SmallQuad), but more hardpoint/internal-bay headroom — a
            // genuine mass-vs-capacity trade-off, not a strict upgrade.
            CreateOrReplace<DroneAirframeDefinition>($"{DronesDir}/Airframe_SmallHexa.asset", a =>
            {
                a.id = "drone_airframe_smallhexa";
                a.displayName = "Small Hexacopter Airframe";
                a.category = PartCategory.DroneAirframe;
                a.tier = TechTier.Tier1_Guided;
                a.researchCost = 90;
                a.buildCost = 60;
                a.massKg = 3f;
                a.airframeClass = DroneAirframeClass.Hexacopter;
                a.dragCoefficient = 0.2f;
                a.structuralMassKg = 10f;
                a.hardpointCount = 4;
                a.internalBayCount = 0;
                a.baseRadarCrossSection = 0.4f;
                a.rotorCount = 6;
                a.maxTakeOffMassKg = 260f;
            });

            // Fixed-Wing: the enum value already existed but had no asset. Pairs with
            // jet/ICE propulsion's requiresForwardFlight=true — no rotors (rotorCount=0),
            // gets DroneVisualBuilder's fuselage+wings silhouette instead of the
            // multirotor mesh.
            CreateOrReplace<DroneAirframeDefinition>($"{DronesDir}/Airframe_FixedWing.asset", a =>
            {
                a.id = "drone_airframe_fixedwing";
                a.displayName = "Fixed-Wing Airframe";
                a.category = PartCategory.DroneAirframe;
                a.tier = TechTier.Tier1_Guided;
                a.researchCost = 100;
                a.buildCost = 70;
                a.massKg = 4f;
                a.airframeClass = DroneAirframeClass.FixedWing;
                a.dragCoefficient = 0.06f;
                a.structuralMassKg = 12f;
                a.hardpointCount = 2;
                a.internalBayCount = 0;
                a.baseRadarCrossSection = 0.5f;
                a.rotorCount = 0;
                a.maxTakeOffMassKg = 300f;
            });

            // Flying-Wing Stealth: low-RCS silhouette, internal bays only (no external
            // hardpoints exposing RCS) — pairs with RAM hull material for the full
            // stealth stack. Tier 3, matching the design doc's stealth-era tech tier.
            CreateOrReplace<DroneAirframeDefinition>($"{DronesDir}/Airframe_FlyingWingStealth.asset", a =>
            {
                a.id = "drone_airframe_flyingwingstealth";
                a.displayName = "Flying-Wing Stealth Airframe";
                a.category = PartCategory.DroneAirframe;
                a.tier = TechTier.Tier3_Stealth;
                a.researchCost = 240;
                a.buildCost = 160;
                a.massKg = 6f;
                a.airframeClass = DroneAirframeClass.FlyingWingStealth;
                a.dragCoefficient = 0.04f;
                a.structuralMassKg = 18f;
                a.hardpointCount = 2;
                a.internalBayCount = 2;
                a.baseRadarCrossSection = 0.08f;
                a.rotorCount = 0;
                a.maxTakeOffMassKg = 380f;
            });

            // CCA-Scale: the largest, most expensive airframe — cutting-edge Collaborative
            // Combat Aircraft tier from the design doc's concept summary. Tier 4, highest
            // hardpoint count and MTOW headroom of any drone airframe.
            CreateOrReplace<DroneAirframeDefinition>($"{DronesDir}/Airframe_CcaScale.asset", a =>
            {
                a.id = "drone_airframe_ccascale";
                a.displayName = "CCA-Scale Airframe";
                a.category = PartCategory.DroneAirframe;
                a.tier = TechTier.Tier4_Hypersonic;
                a.researchCost = 400;
                a.buildCost = 280;
                a.massKg = 12f;
                a.airframeClass = DroneAirframeClass.CcaScale;
                a.dragCoefficient = 0.05f;
                a.structuralMassKg = 40f;
                a.hardpointCount = 6;
                a.internalBayCount = 3;
                a.baseRadarCrossSection = 0.15f;
                a.rotorCount = 0;
                a.maxTakeOffMassKg = 800f;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase2BDroneBreadthSeeder] Seeded Small Hexacopter, Fixed-Wing, Flying-Wing Stealth, and " +
                "CCA-Scale airframe variants under Assets/_Project/Data/Drones/ (Airframe_SmallQuad from " +
                "Phase1DataSeeder covers Small Quad). Not yet wired into the tech tree or Workshop picker.");
        }

        [MenuItem("Vanquish/Phase 2B/Seed Drone Rotor Variants (Material x Size)")]
        public static void SeedRotorVariants()
        {
            EnsureDir(DronesDir);

            // Every RotorMaterial x RotorSize combination gets its own asset, per
            // PLAN.md's explicit ask. Size scales liftCoefficient/mass/drag up
            // independent of material; material trades mass vs. durability
            // (structuralIntegrity) independent of size — "small carbon fibre" and
            // "large plastic" are both valid builds for different purposes.
            SeedRotor(RotorMaterial.Plastic, RotorSize.Small, mass: 0.6f, lift: 0.8f, drag: 0.04f, durability: 0.5f, cost: 20, build: 12);
            SeedRotor(RotorMaterial.Plastic, RotorSize.Medium, mass: 1f, lift: 1f, drag: 0.05f, durability: 0.5f, cost: 30, build: 18);
            SeedRotor(RotorMaterial.Plastic, RotorSize.Large, mass: 1.6f, lift: 1.3f, drag: 0.07f, durability: 0.45f, cost: 45, build: 26);

            SeedRotor(RotorMaterial.CarbonFiber, RotorSize.Small, mass: 0.4f, lift: 0.8f, drag: 0.035f, durability: 0.4f, cost: 55, build: 35);
            SeedRotor(RotorMaterial.CarbonFiber, RotorSize.Medium, mass: 0.65f, lift: 1f, drag: 0.045f, durability: 0.4f, cost: 75, build: 48);
            SeedRotor(RotorMaterial.CarbonFiber, RotorSize.Large, mass: 1.05f, lift: 1.3f, drag: 0.06f, durability: 0.35f, cost: 100, build: 65);

            SeedRotor(RotorMaterial.Metal, RotorSize.Small, mass: 0.9f, lift: 0.8f, drag: 0.05f, durability: 0.85f, cost: 40, build: 25);
            SeedRotor(RotorMaterial.Metal, RotorSize.Medium, mass: 1.5f, lift: 1f, drag: 0.065f, durability: 0.85f, cost: 55, build: 34);
            SeedRotor(RotorMaterial.Metal, RotorSize.Large, mass: 2.4f, lift: 1.3f, drag: 0.09f, durability: 0.8f, cost: 80, build: 50);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase2BDroneBreadthSeeder] Seeded all 9 RotorMaterial x RotorSize combinations under " +
                "Assets/_Project/Data/Drones/ (Propeller_Basic from Phase1DataSeeder covers Plastic/Medium). " +
                "Not yet wired into the tech tree or Workshop picker.");
        }

        private static void SeedRotor(RotorMaterial material, RotorSize size, float mass, float lift, float drag,
            float durability, int cost, int build)
        {
            string assetName = $"Propeller_{material}_{size}";
            CreateOrReplace<WingOrPropellerDefinition>($"{DronesDir}/{assetName}.asset", w =>
            {
                w.id = $"drone_propeller_{material.ToString().ToLowerInvariant()}_{size.ToString().ToLowerInvariant()}";
                w.displayName = $"{size} {(material == RotorMaterial.CarbonFiber ? "Carbon Fiber" : material.ToString())} Propeller Set";
                w.category = PartCategory.DroneWingOrPropeller;
                w.tier = TechTier.Tier0_Improvised;
                w.researchCost = cost;
                w.buildCost = build;
                w.massKg = mass;
                w.liftSurfaceType = LiftSurfaceType.Propeller;
                w.liftCoefficient = lift;
                w.dragCoefficient = drag;
                w.turnRateDegreesPerSecond = 180f;
                w.cruiseEfficiencyMultiplier = 1f;
                w.rotorMaterial = material;
                w.rotorSize = size;
                w.structuralIntegrity = durability;
            });
        }

        /// <summary>
        /// Depth pass (direct user feedback: "there's no radar option, so the sensor
        /// stat is always stuck as the limiting factor for missiles"): only two
        /// sensor suites ever existed (Sensor_Basic, 1500m; Sensor_Scout, 4000m —
        /// both Tier 0), and the strike drone's own sensor was hardcoded to always
        /// use Sensor_Basic regardless of what was unlocked (see
        /// WorkshopController.TryBuildDroneLoadout, now fixed alongside this). Since
        /// seeded missile seekers reach 1500-9000m, a 1500m sensor made a strike
        /// drone's own detection the bottleneck against almost every seeker tier —
        /// these two proper radar upgrades (Tier2/Tier4) give a real choice that can
        /// actually out-range mid/high-tier seekers instead of always losing that race.
        /// </summary>
        [MenuItem("Vanquish/Phase 2B/Seed Drone Sensor Variants")]
        public static void SeedSensorVariants()
        {
            EnsureDir(DronesDir);

            CreateOrReplace<SensorSuiteDefinition>($"{DronesDir}/Sensor_Radar_Advanced.asset", s =>
            {
                s.id = "drone_sensor_radar_advanced";
                s.displayName = "Advanced Pulse-Doppler Radar";
                s.category = PartCategory.DroneSensorSuite;
                s.tier = TechTier.Tier2_Advanced;
                s.researchCost = 170;
                s.buildCost = 110;
                s.massKg = 2.5f;
                s.radarRangeMeters = 6000f;
                s.radarFieldOfViewDegrees = 120f;
                s.eoIrRangeMeters = 3500f;
                s.eoIrFieldOfViewDegrees = 60f;
                s.esmRangeMeters = 2500f;
                s.sharesContactsWithTeam = false;
                s.datalinkRelayDelaySeconds = 0f;
            });

            CreateOrReplace<SensorSuiteDefinition>($"{DronesDir}/Sensor_Radar_LongRange.asset", s =>
            {
                s.id = "drone_sensor_radar_longrange";
                s.displayName = "Long-Range AESA Radar";
                s.category = PartCategory.DroneSensorSuite;
                s.tier = TechTier.Tier4_Hypersonic;
                s.researchCost = 320;
                s.buildCost = 220;
                s.massKg = 4f;
                // Comfortably out-ranges even the longest-reaching seeded seeker
                // (Seeker_MultiSpectral, 9000m) — a real reason to pick this over
                // always being outranged by your own missiles.
                s.radarRangeMeters = 10000f;
                s.radarFieldOfViewDegrees = 140f;
                s.eoIrRangeMeters = 5000f;
                s.eoIrFieldOfViewDegrees = 60f;
                s.esmRangeMeters = 4000f;
                s.sharesContactsWithTeam = false;
                s.datalinkRelayDelaySeconds = 0f;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase2BDroneBreadthSeeder] Seeded Advanced Pulse-Doppler Radar and Long-Range AESA Radar " +
                "sensor variants under Assets/_Project/Data/Drones/ (Sensor_Basic from Phase1DataSeeder covers " +
                "the Tier-0 baseline). Not yet wired into the tech tree or Workshop picker — see class comment.");
        }

        [MenuItem("Vanquish/Phase 2B/Seed Drone Wing Type Variants")]
        public static void SeedWingTypeVariants()
        {
            EnsureDir(DronesDir);

            // FixedWing (plain straight wing) was retired by Phase3HPlanformSeeder,
            // which deletes Wing_FixedWing.asset entirely — the planform-preset pass
            // curates exactly one wing per airframe class instead of a free
            // cross-product, and the straight wing wasn't chosen for any of the three
            // curated planforms. No longer seeded here; do not re-add without also
            // updating Phase3HPlanformSeeder's retirement logic.

            // DeltaWing: less low-speed lift than a straight wing but far more
            // maneuverable and lower drag at speed — the classic speed/agility vs.
            // low-speed-handling trade. Fixed-wing flight-model rework: a real delta
            // planform's vortex lift lets it fly to a much higher angle of attack
            // before conventionally stalling than a straight wing (criticalAoADegrees
            // 28 vs. FixedWing's 15) — this is what "far more maneuverable" now
            // concretely means under the real AoA model, not just a turnRate number.
            CreateOrReplace<WingOrPropellerDefinition>($"{DronesDir}/Wing_DeltaWing.asset", w =>
            {
                w.id = "drone_wing_deltawing";
                w.displayName = "Delta Wing";
                w.category = PartCategory.DroneWingOrPropeller;
                w.tier = TechTier.Tier2_Advanced;
                w.researchCost = 150;
                w.buildCost = 95;
                w.massKg = 3.5f;
                w.liftSurfaceType = LiftSurfaceType.DeltaWing;
                w.liftCoefficient = 1.1f;
                w.dragCoefficient = 0.035f;
                w.turnRateDegreesPerSecond = 110f;
                w.cruiseEfficiencyMultiplier = 1.1f;
                w.zeroLiftAoADegrees = -1f;
                w.referenceAoADegrees = 8f;
                w.criticalAoADegrees = 28f;
                w.inducedDragFactor = 0.015f;
            });

            // VariableSweepWing: mechanically adjusts sweep angle in flight (Phase 1
            // simplification: modeled as a single averaged "best of both worlds" stat
            // block rather than a real in-flight sweep state machine) — best
            // maneuverability AND good cruise efficiency, at the highest mass/cost of
            // the three, matching the real-world complexity/cost of swing-wing designs.
            // Fixed-wing flight-model rework: splits the difference between FixedWing's
            // forgiving-but-limited stall margin and DeltaWing's huge one, and pays the
            // least induced drag of the three (the "good cruise efficiency" half of its
            // description) rather than only DeltaWing beating it on drag.
            CreateOrReplace<WingOrPropellerDefinition>($"{DronesDir}/Wing_VariableSweepWing.asset", w =>
            {
                w.id = "drone_wing_variablesweepwing";
                w.displayName = "Variable-Sweep Wing";
                w.category = PartCategory.DroneWingOrPropeller;
                w.tier = TechTier.Tier3_Stealth;
                w.researchCost = 230;
                w.buildCost = 150;
                w.massKg = 5f;
                w.liftSurfaceType = LiftSurfaceType.VariableSweepWing;
                w.liftCoefficient = 1.25f;
                w.dragCoefficient = 0.03f;
                w.turnRateDegreesPerSecond = 130f;
                w.cruiseEfficiencyMultiplier = 1.25f;
                w.zeroLiftAoADegrees = -2f;
                w.referenceAoADegrees = 6f;
                w.criticalAoADegrees = 20f;
                w.inducedDragFactor = 0.012f;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase2BDroneBreadthSeeder] Seeded Fixed Wing, Delta Wing, and Variable-Sweep Wing " +
                "variants under Assets/_Project/Data/Drones/. Not yet wired into the tech tree or Workshop picker.");
        }

        [MenuItem("Vanquish/Phase 2B/Seed Drone Hull Material Variants")]
        public static void SeedHullMaterialVariants()
        {
            EnsureDir(DronesDir);

            // Aluminum Alloy: lighter than composite plastic with meaningfully better
            // armor, at a moderate cost premium — a solid all-around mid-tier upgrade.
            CreateOrReplace<HullMaterialDefinition>($"{DronesDir}/Hull_AluminumAlloy.asset", h =>
            {
                h.id = "drone_hull_aluminumalloy";
                h.displayName = "Aluminum Alloy Hull";
                h.category = PartCategory.DroneHullMaterial;
                h.tier = TechTier.Tier1_Guided;
                h.researchCost = 80;
                h.buildCost = 50;
                h.massKg = 1.5f;
                h.materialType = HullMaterialType.AluminumAlloy;
                h.armorRating = 12f;
                h.densityFactor = 1.4f;
                h.radarCrossSectionMultiplier = 1f;
                h.maxTemperatureCelsius = 250f;
            });

            // Carbon Fiber: lightest hull material — mass vs. structural trade, not a
            // strict upgrade over Aluminum/Composite Plastic (lower armor than Aluminum
            // despite the cost premium).
            CreateOrReplace<HullMaterialDefinition>($"{DronesDir}/Hull_CarbonFiber.asset", h =>
            {
                h.id = "drone_hull_carbonfiber";
                h.displayName = "Carbon Fiber Hull";
                h.category = PartCategory.DroneHullMaterial;
                h.tier = TechTier.Tier2_Advanced;
                h.researchCost = 140;
                h.buildCost = 90;
                h.massKg = 0.7f;
                h.materialType = HullMaterialType.CarbonFiber;
                h.armorRating = 8f;
                h.densityFactor = 0.6f;
                h.radarCrossSectionMultiplier = 0.9f;
                h.maxTemperatureCelsius = 200f;
            });

            // Radar-Absorbent Material: the stealth hull — meaningfully cuts RCS at a
            // mass/cost premium and no armor upside, matching the design doc's
            // "Radar Absorbent Material" high-tier material callout.
            CreateOrReplace<HullMaterialDefinition>($"{DronesDir}/Hull_RadarAbsorbentMaterial.asset", h =>
            {
                h.id = "drone_hull_radarabsorbentmaterial";
                h.displayName = "Radar-Absorbent Material Hull";
                h.category = PartCategory.DroneHullMaterial;
                h.tier = TechTier.Tier3_Stealth;
                h.researchCost = 220;
                h.buildCost = 140;
                h.massKg = 1.8f;
                h.materialType = HullMaterialType.RadarAbsorbentMaterial;
                h.armorRating = 6f;
                h.densityFactor = 1.1f;
                h.radarCrossSectionMultiplier = 0.35f;
                h.maxTemperatureCelsius = 180f;
            });

            // Titanium Alloy: heaviest and priciest, but the highest armor rating and by
            // far the highest max operating temperature — the hypersonic/CCA-tier hull,
            // matching the design doc's "Titanium alloys" high-tier material callout.
            CreateOrReplace<HullMaterialDefinition>($"{DronesDir}/Hull_TitaniumAlloy.asset", h =>
            {
                h.id = "drone_hull_titaniumalloy";
                h.displayName = "Titanium Alloy Hull";
                h.category = PartCategory.DroneHullMaterial;
                h.tier = TechTier.Tier4_Hypersonic;
                h.researchCost = 320;
                h.buildCost = 220;
                h.massKg = 2.6f;
                h.materialType = HullMaterialType.TitaniumAlloy;
                h.armorRating = 20f;
                h.densityFactor = 1.6f;
                h.radarCrossSectionMultiplier = 1f;
                h.maxTemperatureCelsius = 650f;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase2BDroneBreadthSeeder] Seeded Aluminum Alloy, Carbon Fiber, Radar-Absorbent Material, " +
                "and Titanium Alloy hull variants under Assets/_Project/Data/Drones/ (Hull_CompositePlastic from " +
                "Phase1DataSeeder covers Composite Plastic). Not yet wired into the tech tree or Workshop picker.");
        }

        [MenuItem("Vanquish/Phase 2B/Seed Drone Propulsion, Engine & Fuel Variants")]
        public static void SeedPropulsionEngineFuelVariants()
        {
            EnsureDir(DronesDir);
            EnsureDir(SharedDir);

            // ---- Internal Combustion (Petrol/Diesel): high endurance, distinct
            // thermal/acoustic signature vs. electric, fixed dry engine mass — per the
            // design doc's Drone Propulsion & Fuel Spectrum section. Data-only
            // simplification: modeled as a single ICE propulsion/engine pair fuelled by
            // Petrol; a Diesel fuel variant is also seeded (higher energy density/mass,
            // lower volatility) so both fuel types the design doc calls out exist as
            // real assets, even though only one ICE engine asset exists so far. Kept
            // multirotor-compatible (requiresForwardFlight=false) — a gas-quadcopter is
            // a real high-endurance FPV pattern, not sci-fi.
            var propulsionIce = CreateOrReplace<PropulsionDefinition>($"{DronesDir}/Propulsion_ICE_Basic.asset", p =>
            {
                p.id = "drone_propulsion_ice_basic";
                p.displayName = "Basic ICE Propulsion";
                p.category = PartCategory.DronePropulsion;
                p.tier = TechTier.Tier1_Guided;
                p.researchCost = 110;
                p.buildCost = 70;
                p.massKg = 5f;
                p.propulsionType = PropulsionType.InternalCombustion;
                p.maxSpeedMetersPerSecond = 35f;
                p.accelerationMetersPerSecondSquared = 5f;
                p.acousticSignature = 0.8f;
                p.infraredSignature = 0.9f;
                p.requiresForwardFlight = false;
            });

            CreateOrReplace<DroneEngineDefinition>($"{DronesDir}/Engine_ICE_Basic.asset", e =>
            {
                e.id = "drone_engine_ice_basic";
                e.displayName = "Basic ICE Engine";
                e.category = PartCategory.DroneEngine;
                e.tier = TechTier.Tier1_Guided;
                e.researchCost = 110;
                e.buildCost = 70;
                e.massKg = 6f;
                e.powerOutput = 2200f;
                e.consumptionRatePerSecond = 3f;
                e.infraredSignature = 1.2f;
                e.reliability = 0.85f;
            });

            CreateOrReplace<FuelDefinition>($"{SharedDir}/Fuel_Petrol_Basic.asset", f =>
            {
                f.id = "fuel_petrol_basic";
                f.displayName = "Petrol";
                f.category = PartCategory.DroneFuel;
                f.tier = TechTier.Tier1_Guided;
                f.researchCost = 60;
                f.buildCost = 35;
                f.massKg = 0f;
                f.fuelType = FuelType.Petrol;
                f.energyDensityMjPerKg = 44f;
                f.capacityKg = 8f;
                f.volatility = 0.6f;
            });

            CreateOrReplace<FuelDefinition>($"{SharedDir}/Fuel_Diesel_Basic.asset", f =>
            {
                f.id = "fuel_diesel_basic";
                f.displayName = "Diesel";
                f.category = PartCategory.DroneFuel;
                f.tier = TechTier.Tier1_Guided;
                f.researchCost = 65;
                f.buildCost = 40;
                f.massKg = 0f;
                f.fuelType = FuelType.Diesel;
                f.energyDensityMjPerKg = 46f;
                f.capacityKg = 9f;
                f.volatility = 0.4f;
            });

            // ---- Subsonic Jet: fixed-wing-style forward flight, jet fuel. ----
            CreateOrReplace<PropulsionDefinition>($"{DronesDir}/Propulsion_Jet_Subsonic.asset", p =>
            {
                p.id = "drone_propulsion_jet_subsonic";
                p.displayName = "Subsonic Jet Propulsion";
                p.category = PartCategory.DronePropulsion;
                p.tier = TechTier.Tier2_Advanced;
                p.researchCost = 170;
                p.buildCost = 110;
                p.massKg = 6f;
                p.propulsionType = PropulsionType.SubsonicJet;
                p.maxSpeedMetersPerSecond = 230f;
                p.accelerationMetersPerSecondSquared = 12f;
                p.acousticSignature = 0.6f;
                p.infraredSignature = 1.4f;
                p.requiresForwardFlight = true;
            });

            CreateOrReplace<DroneEngineDefinition>($"{DronesDir}/Engine_Jet_Subsonic.asset", e =>
            {
                e.id = "drone_engine_jet_subsonic";
                e.displayName = "Subsonic Turbofan";
                e.category = PartCategory.DroneEngine;
                e.tier = TechTier.Tier2_Advanced;
                e.researchCost = 170;
                e.buildCost = 110;
                e.massKg = 9f;
                // Fix (direct user feedback: "max speed is low for supersonic engine",
                // same underlying issue here too): top speed in this sim is whatever
                // speed makes thrust == quadratic drag, not a directly-set number —
                // 3200N against a ~0.095-0.072 combined airframe+wing dragCoefficient
                // range only reached ~180-210 m/s, nowhere near a genuine subsonic-jet
                // cruise speed. Raised so equilibrium lands around 250-290 m/s
                // (still comfortably below Mach 1, ~343 m/s, matching "Subsonic").
                e.powerOutput = 6000f;
                e.consumptionRatePerSecond = 4f;
                e.infraredSignature = 1.8f;
                e.reliability = 0.9f;
                // Fixed-wing flight-model rework: engines had no field tying them to a
                // flight model at all before this — see DroneEngineDefinition.requiresForwardFlight's
                // own doc comment for why that was a real compatibility gap.
                e.requiresForwardFlight = true;
            });

            // ---- Supersonic Jet: highest tier drone propulsion. ----
            CreateOrReplace<PropulsionDefinition>($"{DronesDir}/Propulsion_Jet_Supersonic.asset", p =>
            {
                p.id = "drone_propulsion_jet_supersonic";
                p.displayName = "Supersonic Jet Propulsion";
                p.category = PartCategory.DronePropulsion;
                p.tier = TechTier.Tier4_Hypersonic;
                p.researchCost = 340;
                p.buildCost = 230;
                p.massKg = 10f;
                p.propulsionType = PropulsionType.SupersonicJet;
                p.maxSpeedMetersPerSecond = 620f;
                p.accelerationMetersPerSecondSquared = 20f;
                p.acousticSignature = 0.9f;
                p.infraredSignature = 3f;
                p.requiresForwardFlight = true;
            });

            CreateOrReplace<DroneEngineDefinition>($"{DronesDir}/Engine_Jet_Supersonic.asset", e =>
            {
                e.id = "drone_engine_jet_supersonic";
                e.displayName = "Supersonic Afterburning Turbojet";
                e.category = PartCategory.DroneEngine;
                e.tier = TechTier.Tier4_Hypersonic;
                e.researchCost = 340;
                e.buildCost = 230;
                e.massKg = 14f;
                // Fix (direct user feedback: "max speed is low (300m/s) for supersonic
                // engine"): equilibrium speed (thrust == quadratic drag) with the old
                // 6500N only reached ~260-300 m/s regardless of the seeded
                // maxSpeedMetersPerSecond=620 stat (that stat has never actually been
                // enforced anywhere — see the Propulsion+Engine merge's own notes on
                // dead fields). Raised to reach a genuinely supersonic equilibrium
                // (~460-530 m/s, comfortably above Mach 1) across every planform's
                // drag range instead of stalling out just shy of the sound barrier.
                e.powerOutput = 20000f;
                e.consumptionRatePerSecond = 10f;
                e.infraredSignature = 3.5f;
                e.reliability = 0.88f;
                e.requiresForwardFlight = true;
            });

            CreateOrReplace<FuelDefinition>($"{SharedDir}/Fuel_JetFuel_Basic.asset", f =>
            {
                f.id = "fuel_jetfuel_basic";
                f.displayName = "Jet Fuel";
                f.category = PartCategory.DroneFuel;
                f.tier = TechTier.Tier2_Advanced;
                f.researchCost = 90;
                f.buildCost = 55;
                f.massKg = 0f;
                f.fuelType = FuelType.JetFuel;
                f.energyDensityMjPerKg = 43f;
                f.capacityKg = 12f;
                f.volatility = 0.5f;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Phase2BDroneBreadthSeeder] Seeded ICE (Petrol/Diesel), Subsonic Jet, and Supersonic Jet " +
                "propulsion/engine/fuel variants under Assets/_Project/Data/Drones/ and Assets/_Project/Data/Shared/ " +
                $"(Propulsion_Electric_Basic/Engine_Electric_Basic/Fuel_Battery_Basic from Phase1DataSeeder cover " +
                $"Electric). Not yet wired into the tech tree or Workshop picker. ICE propulsion id={propulsionIce.id}.");
        }

        [MenuItem("Vanquish/Phase 2B/Seed Drone Weapon Bay Variants")]
        public static void SeedWeaponBayVariants()
        {
            EnsureDir(DronesDir);

            // Large external bay: more capacity/munition count than the Tier-0 Small
            // bay, but still external (adds exposed RCS once stealth RCS accounting
            // consumes isInternal).
            CreateOrReplace<WeaponBayDefinition>($"{DronesDir}/WeaponBay_Large.asset", w =>
            {
                w.id = "drone_weaponbay_large";
                w.displayName = "Large External Weapon Bay";
                w.category = PartCategory.DroneWeaponBay;
                w.tier = TechTier.Tier1_Guided;
                w.researchCost = 100;
                w.buildCost = 65;
                w.massKg = 2.5f;
                w.payloadCapacityKg = 60f;
                w.maxMunitionCount = 8;
                w.internalCapacity = 0; // purely external
                w.isInternal = false;
                w.cycleTimeSeconds = 2.5f;
            });

            // Internal bay: lower capacity than the Large external bay for its tier, but
            // isInternal=true — munitions carried here don't add to the airframe's
            // exposed RCS, the whole point of pairing this with a stealth airframe/hull.
            CreateOrReplace<WeaponBayDefinition>($"{DronesDir}/WeaponBay_InternalMedium.asset", w =>
            {
                w.id = "drone_weaponbay_internalmedium";
                w.displayName = "Internal Medium Weapon Bay";
                w.category = PartCategory.DroneWeaponBay;
                w.tier = TechTier.Tier3_Stealth;
                w.researchCost = 200;
                w.buildCost = 130;
                w.massKg = 3f;
                w.payloadCapacityKg = 40f;
                // Depth pass (direct user feedback: "internal bay should always be used
                // first, then pylons after — affecting RCS"): a mixed bay rather than
                // purely internal — the first 4 rounds ride hidden with zero RCS
                // contribution, exactly like before; a design that actually carries more
                // than that spills the extra 2 onto external pylons (visibly mounted,
                // adding RCS), instead of the old "isInternal=true means the ENTIRE
                // loadout is invisible/free no matter how much ammo" behavior.
                w.maxMunitionCount = 6;
                w.internalCapacity = 4;
                w.isInternal = true;
                w.cycleTimeSeconds = 3f;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase2BDroneBreadthSeeder] Seeded Large External and Internal Medium weapon bay variants " +
                "under Assets/_Project/Data/Drones/ (WeaponBay_Small from Phase1DataSeeder covers the Tier-0 " +
                "external bay). Not yet wired into the tech tree or Workshop picker.");
        }

        [MenuItem("Vanquish/Phase 2B/Seed All Drone Breadth")]
        public static void SeedAll()
        {
            SeedAirframeVariants();
            SeedRotorVariants();
            SeedWingTypeVariants();
            SeedHullMaterialVariants();
            SeedPropulsionEngineFuelVariants();
            SeedWeaponBayVariants();
            Debug.Log("[Phase2BDroneBreadthSeeder] All Phase 2B drone breadth categories seeded.");
        }

        /// <summary>
        /// Wires every Phase 2B drone-breadth part asset (seeded by the six "Seed Drone
        /// ..." menu items above) behind its own TechNode, so WorkshopController's part
        /// picker (extended to drone slots alongside 2A's missile picker) has something
        /// unlockable to show. Requires Phase1DataSeeder's Tier-0 drone nodes
        /// (TN_06-TN_09) and all six Phase 2B variant seeders to have already run.
        /// Idempotent, like the rest of this file and Phase2AMissileBreadthSeeder's
        /// equivalent method.
        /// </summary>
        [MenuItem("Vanquish/Phase 2B/Seed Drone Breadth Tech Nodes")]
        public static void SeedTechTreeNodes()
        {
            EnsureDir(TechTreeDir);

            var tnDroneBasics = LoadNode("TN_06_DroneBasics");       // droneAirframe, dronePropulsion
            var tnDronePower = LoadNode("TN_07_DronePower");         // droneEngine, droneFuel
            var tnDroneStructure = LoadNode("TN_08_DroneStructure"); // droneWingOrPropeller, droneHullMaterial
            var tnDroneSystems = LoadNode("TN_09_DroneSystems");     // droneWeaponBay, sensorBasic

            if (tnDroneBasics == null || tnDronePower == null || tnDroneStructure == null || tnDroneSystems == null)
            {
                Debug.LogError("[Phase2BDroneBreadthSeeder] Missing Phase 1 drone tech nodes (TN_06-TN_09) — run " +
                    "Vanquish/Phase 1/Seed Tier-0 Data first.");
                return;
            }

            // ---- Airframes: Hexacopter branches directly off the Tier-0 drone basics
            // node. Fixed-Wing/FlyingWingStealth/CcaScale individually unlockable
            // TechNodes were retired by Phase3HPlanformSeeder — those three airframes
            // are now unlocked only as part of a merged Planform preset (airframe+wing
            // together, see TN_3H_planform_* nodes) rather than standalone. ----
            CreatePartTechNode(LoadPart<DroneAirframeDefinition>("Airframe_SmallHexa"), tnDroneBasics);

            // ---- Rotors: all 9 Material x Size combinations branch directly off the
            // Tier-0 drone structure node — they're alternative Tier-0 choices (material/
            // size trade-offs), not a tiered progression, matching their seeded
            // Tier0_Improvised tier. ----
            foreach (RotorMaterial material in System.Enum.GetValues(typeof(RotorMaterial)))
            {
                foreach (RotorSize size in System.Enum.GetValues(typeof(RotorSize)))
                {
                    CreatePartTechNode(LoadPart<WingOrPropellerDefinition>($"Propeller_{material}_{size}"), tnDroneStructure);
                }
            }

            // ---- Wing types: Fixed Wing (and its TechNode) was retired by
            // Phase3HPlanformSeeder; Delta Wing/Variable-Sweep Wing are no longer
            // individually unlockable either — both are now only unlocked as part of a
            // merged Planform preset (see TN_3H_planform_* nodes). Neither is seeded
            // standalone here anymore. ----

            // ---- Hull materials: Aluminum Alloy -> Carbon Fiber -> RAM -> Titanium,
            // matching their increasing seeded tiers (1, 2, 3, 4). ----
            var tnAluminum = CreatePartTechNode(LoadPart<HullMaterialDefinition>("Hull_AluminumAlloy"), tnDroneStructure);
            var tnCarbonFiber = CreatePartTechNode(LoadPart<HullMaterialDefinition>("Hull_CarbonFiber"), tnAluminum);
            var tnRam = CreatePartTechNode(LoadPart<HullMaterialDefinition>("Hull_RadarAbsorbentMaterial"), tnCarbonFiber);
            CreatePartTechNode(LoadPart<HullMaterialDefinition>("Hull_TitaniumAlloy"), tnRam);

            // ---- Fuel: Petrol/Diesel/Jet Fuel branch off the Tier-0 drone power node. ----
            // Note: the individual ICE/Jet-Subsonic/Jet-Supersonic Propulsion+Engine
            // TechNodes that used to live here were retired by
            // Phase3IPropulsionMergeSeeder in favor of 3 merged TN_3I_package_* nodes
            // (see its own doc comment) — do not re-add them here, or re-running this
            // method will silently resurrect the retired individual unlock paths.
            CreatePartTechNode(LoadPart<FuelDefinition>("Fuel_Petrol_Basic", SharedDir), tnDronePower);
            CreatePartTechNode(LoadPart<FuelDefinition>("Fuel_Diesel_Basic", SharedDir), tnDronePower);
            CreatePartTechNode(LoadPart<FuelDefinition>("Fuel_JetFuel_Basic", SharedDir), tnDronePower);

            // ---- Weapon bays: Large (external) branches off the Tier-0 drone systems
            // node; Internal Medium upgrades from Large, matching their seeded tiers
            // (1, 3). ----
            var tnLargeBay = CreatePartTechNode(LoadPart<WeaponBayDefinition>("WeaponBay_Large"), tnDroneSystems);
            CreatePartTechNode(LoadPart<WeaponBayDefinition>("WeaponBay_InternalMedium"), tnLargeBay);

            // ---- Sensors: Advanced Radar branches off the Tier-0 drone systems node;
            // Long-Range AESA upgrades from Advanced Radar. ----
            var tnAdvancedRadar = CreatePartTechNode(LoadPart<SensorSuiteDefinition>("Sensor_Radar_Advanced"), tnDroneSystems);
            CreatePartTechNode(LoadPart<SensorSuiteDefinition>("Sensor_Radar_LongRange"), tnAdvancedRadar);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase2BDroneBreadthSeeder] Seeded 21 TechNodes (TN_2B_*) gating the Phase 2B drone " +
                "breadth variants under Assets/_Project/Data/TechTree/ (10 fewer than the original 31 — the " +
                "individual Fixed-Wing/Flying-Wing Stealth/CCA-Scale airframe and Fixed/Delta/Variable-Sweep " +
                "Wing TechNodes were retired by Phase3HPlanformSeeder in favor of 3 merged Planform TechNodes, " +
                "and the individual ICE/Jet-Subsonic/Jet-Supersonic Propulsion+Engine TechNodes were retired by " +
                "Phase3IPropulsionMergeSeeder in favor of 3 merged Propulsion Package TechNodes; +2 new Sensor " +
                "TechNodes this pass). " +
                "Re-run Vanquish/Phase 1/Build Workshop Scene to pick these up in WorkshopController's tech " +
                "tree list and drone part picker.");
        }

        private static TechNode LoadNode(string assetName)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TechNode>($"{TechTreeDir}/{assetName}.asset");
            if (asset == null)
                Debug.LogError($"[Phase2BDroneBreadthSeeder] Could not load tech node {assetName}.");
            return asset;
        }

        private static T LoadPart<T>(string assetName, string dir = DronesDir) where T : PartDefinition
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>($"{dir}/{assetName}.asset");
            if (asset == null)
                Debug.LogError($"[Phase2BDroneBreadthSeeder] Could not load part {assetName} — run the relevant " +
                    "'Seed Drone ... Variants' menu item first.");
            return asset;
        }

        /// <summary>Creates (or updates) a TechNode named "TN_2B_&lt;part.id&gt;" that unlocks exactly one part.</summary>
        private static TechNode CreatePartTechNode(PartDefinition part, params TechNode[] prerequisites)
        {
            if (part == null)
                return null;

            string nodeId = $"TN_2B_{part.id}";
            return CreateOrReplace<TechNode>($"{TechTreeDir}/{nodeId}.asset", n =>
            {
                n.id = nodeId;
                n.displayName = part.displayName;
                n.tier = part.tier;
                n.researchCost = part.researchCost;
                n.prerequisites = prerequisites ?? new TechNode[0];
                n.unlocks = new PartDefinition[] { part };
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
