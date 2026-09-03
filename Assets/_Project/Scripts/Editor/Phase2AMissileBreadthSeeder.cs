using System.IO;
using UnityEditor;
using UnityEngine;
using Vanquish.Data;
using Vanquish.Data.Missiles;
using Vanquish.Data.TechTree;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Seeds Phase 2A missile part breadth (payloads, engines, seekers, countermeasures,
    /// jamming) as real ScriptableObject assets under Assets/_Project/Data/Missiles/,
    /// alongside (not replacing) the Tier-0 catalog Phase1DataSeeder creates. Idempotent
    /// like Phase1DataSeeder: re-running overwrites the same asset paths rather than
    /// duplicating them.
    ///
    /// The four "Seed Missile ... Variants" menu items above are data-only (part
    /// assets, no TechNode). <see cref="SeedTechTreeNodes"/> is the follow-up step that
    /// wires each of those assets behind its own TechNode so a player can actually
    /// research and pick them via WorkshopController's multi-option part picker — run
    /// the four variant seeders first, then this one, then re-run
    /// Phase1WorkshopSceneBuilder to pick up the new nodes/options in the Workshop scene.
    /// </summary>
    public static class Phase2AMissileBreadthSeeder
    {
        private const string MissilesDir = "Assets/_Project/Data/Missiles";
        private const string TechTreeDir = "Assets/_Project/Data/TechTree";

        /// <summary>
        /// Depth pass (direct user feedback: "some things are just too heavy to ever
        /// be on a missile"): until now exactly one MissileAirframeDefinition existed
        /// in the whole game (Phase1DataSeeder's Airframe_Basic, 40kg MTOW) — tuned
        /// around the Tier-0 reference loadout (~30kg), but every heavier Tier1-4
        /// engine/seeker/payload/module this file seeds above still had to fit inside
        /// that same 40kg ceiling. A handful of legal higher-tier combinations
        /// (e.g. Scramjet + Cluster + Multi-Spectral seeker, ~46kg with zero optional
        /// modules) mathematically could not fit under any circumstances — the
        /// Workshop's "Enter Combat" gate would simply refuse them forever, with no
        /// bigger airframe a player could ever pick instead. Three more tiers here,
        /// each meaningfully bigger/heavier/pricier (and progressively less
        /// maneuverable/stealthy — a bigger airframe is a bigger radar target) than
        /// the last, mirroring the drone airframe tier progression's own shape.
        /// </summary>
        [MenuItem("Vanquish/Phase 2A/Seed Missile Airframe Variants")]
        public static void SeedAirframeVariants()
        {
            EnsureDir(MissilesDir);

            CreateOrReplace<MissileAirframeDefinition>($"{MissilesDir}/Airframe_Interceptor.asset", a =>
            {
                a.id = "missile_airframe_interceptor";
                a.displayName = "Interceptor Airframe";
                a.category = PartCategory.MissileAirframe;
                a.tier = TechTier.Tier1_Guided;
                a.researchCost = 110;
                a.buildCost = 70;
                a.massKg = 3f;
                a.dragCoefficient = 0.07f;
                a.structuralMassKg = 10f;
                a.maxGForce = 30f;
                a.baseRadarCrossSection = 0.06f;
                a.maxTemperatureCelsius = 300f;
                a.maxTakeOffMassKg = 55f;
            });

            CreateOrReplace<MissileAirframeDefinition>($"{MissilesDir}/Airframe_HeavyStrike.asset", a =>
            {
                a.id = "missile_airframe_heavystrike";
                a.displayName = "Heavy Strike Airframe";
                a.category = PartCategory.MissileAirframe;
                a.tier = TechTier.Tier2_Advanced;
                a.researchCost = 190;
                a.buildCost = 130;
                a.massKg = 5f;
                a.dragCoefficient = 0.09f;
                a.structuralMassKg = 16f;
                // Bigger/heavier airframe, genuinely less maneuverable ceiling than the
                // Interceptor — carrying more mass isn't a strict upgrade.
                a.maxGForce = 22f;
                a.baseRadarCrossSection = 0.11f;
                a.maxTemperatureCelsius = 450f;
                a.maxTakeOffMassKg = 78f;
            });

            CreateOrReplace<MissileAirframeDefinition>($"{MissilesDir}/Airframe_Hypersonic.asset", a =>
            {
                a.id = "missile_airframe_hypersonic";
                a.displayName = "Hypersonic Airframe";
                a.category = PartCategory.MissileAirframe;
                a.tier = TechTier.Tier4_Hypersonic;
                a.researchCost = 320;
                a.buildCost = 220;
                a.massKg = 6f;
                a.dragCoefficient = 0.05f; // slender, built for hypersonic cruise
                a.structuralMassKg = 20f;
                a.maxGForce = 18f; // least maneuverable of the four — pure speed/reach
                a.baseRadarCrossSection = 0.04f;
                a.maxTemperatureCelsius = 1600f; // needs to survive scramjet-class heating
                a.maxTakeOffMassKg = 95f;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase2AMissileBreadthSeeder] Seeded Interceptor, Heavy Strike, and Hypersonic missile " +
                "airframe variants under Assets/_Project/Data/Missiles/ (Airframe_Basic from Phase1DataSeeder " +
                "covers the Tier-0 baseline). Not yet wired into the tech tree or Workshop picker — see class comment.");
        }

        [MenuItem("Vanquish/Phase 2A/Seed Missile Payload Variants")]
        public static void SeedPayloadVariants()
        {
            EnsureDir(MissilesDir);

            // Shaped Charge: armor-piercing HEAT warhead. Efficient mass-to-penetration
            // ratio (focused charge vs. Frag's dispersed blast), but needs a direct hit
            // — no proximity fuse, small blast radius, minimal splash.
            CreateOrReplace<MissilePayloadDefinition>($"{MissilesDir}/Payload_ShapedCharge.asset", p =>
            {
                p.id = "missile_payload_shapedcharge";
                p.displayName = "Shaped Charge Warhead";
                p.category = PartCategory.MissilePayload;
                p.tier = TechTier.Tier1_Guided;
                p.researchCost = 90;
                p.buildCost = 60;
                p.massKg = 4f;
                p.payloadType = PayloadType.ShapedCharge;
                p.warheadMassKg = 4f;
                p.blastRadiusMeters = 3f;
                p.directDamage = 90f;
                p.splashDamage = 5f;
                p.requiresProximityFuse = false;
            });

            // Kinetic: no explosive fill at all — a dense penetrator relying on impact
            // energy. Heavy for its size (tungsten/DU rod), no blast/splash, needs a
            // direct hit.
            CreateOrReplace<MissilePayloadDefinition>($"{MissilesDir}/Payload_Kinetic.asset", p =>
            {
                p.id = "missile_payload_kinetic";
                p.displayName = "Kinetic Penetrator";
                p.category = PartCategory.MissilePayload;
                p.tier = TechTier.Tier1_Guided;
                p.researchCost = 90;
                p.buildCost = 70;
                p.massKg = 6f;
                p.payloadType = PayloadType.Kinetic;
                p.warheadMassKg = 0f;
                p.blastRadiusMeters = 0f;
                p.directDamage = 70f;
                p.splashDamage = 0f;
                p.requiresProximityFuse = false;
            });

            // Cluster: submunition dispenser, airburst over a wide area. Heaviest
            // payload (casing + multiple submunitions), lower single-target damage
            // than a dedicated warhead but much wider splash coverage — a genuine
            // area-denial trade-off, not a strict upgrade.
            CreateOrReplace<MissilePayloadDefinition>($"{MissilesDir}/Payload_Cluster.asset", p =>
            {
                p.id = "missile_payload_cluster";
                p.displayName = "Cluster Submunition Dispenser";
                p.category = PartCategory.MissilePayload;
                p.tier = TechTier.Tier2_Advanced;
                p.researchCost = 150;
                p.buildCost = 90;
                p.massKg = 8f;
                p.payloadType = PayloadType.Cluster;
                p.warheadMassKg = 10f;
                p.blastRadiusMeters = 20f;
                p.directDamage = 20f;
                p.splashDamage = 45f;
                p.requiresProximityFuse = true;
            });

            // Grenade: cheapest, lightest, weakest — the "grenade-drop drones" Tier 0
            // munition referenced in the design doc's improvised tier. Contact-fused,
            // not proximity-fused (it's a dumb impact grenade, not a guided munition).
            CreateOrReplace<MissilePayloadDefinition>($"{MissilesDir}/Payload_Grenade.asset", p =>
            {
                p.id = "missile_payload_grenade";
                p.displayName = "Improvised Grenade";
                p.category = PartCategory.MissilePayload;
                p.tier = TechTier.Tier0_Improvised;
                p.researchCost = 20;
                p.buildCost = 10;
                p.massKg = 1f;
                p.payloadType = PayloadType.Grenade;
                p.warheadMassKg = 1f;
                p.blastRadiusMeters = 4f;
                p.directDamage = 25f;
                p.splashDamage = 10f;
                p.requiresProximityFuse = false;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase2AMissileBreadthSeeder] Seeded Shaped Charge, Kinetic, Cluster, and Grenade payload " +
                "variants under Assets/_Project/Data/Missiles/ (Payload_HEFrag_Small from Phase1DataSeeder covers " +
                "HE-Frag). Not yet wired into the tech tree or Workshop picker — see class comment.");
        }

        [MenuItem("Vanquish/Phase 2A/Seed Missile Engine Variants")]
        public static void SeedEngineVariants()
        {
            EnsureDir(MissilesDir);

            // Liquid Rocket: throttleable, better specific impulse than solid propellant
            // so it burns much longer for similar mass, but the tankage/turbopump/valve
            // plumbing needed to handle liquid propellant makes the engine itself heavier
            // and pricier than the Tier 0 solid motor. Lower peak thrust, longer burn —
            // a genuine trade, not a strict upgrade.
            CreateOrReplace<MissileEngineDefinition>($"{MissilesDir}/Engine_LiquidRocket.asset", e =>
            {
                e.id = "missile_engine_liquid_basic";
                e.displayName = "Liquid Rocket Engine";
                e.category = PartCategory.MissileEngine;
                e.tier = TechTier.Tier1_Guided;
                e.researchCost = 100;
                e.buildCost = 70;
                e.massKg = 8f;
                e.propulsionType = PropulsionType.LiquidRocket;
                e.thrustNewtons = 2800f;
                e.burnTimeSeconds = 14f;
                e.maxSpeedMetersPerSecond = 320f;
                e.infraredSignature = 1.3f;
                e.maneuverabilityMultiplier = 1f;
            });

            // Ramjet: air-breathing, so it doesn't need to carry its own oxidizer — lighter
            // than the rocket motors above for a given burn time, and burns far longer
            // (sustained cruise rather than a short boost). Data-only simplification for
            // now: real ramjets can't produce thrust below roughly Mach 0.5-1 (need
            // ram-air compression to work at all); that airspeed-gated ignition behavior
            // isn't modeled yet — see the Phase 2C guidance/sensor work for where
            // propulsion-model nuance like this is expected to land.
            CreateOrReplace<MissileEngineDefinition>($"{MissilesDir}/Engine_Ramjet.asset", e =>
            {
                e.id = "missile_engine_ramjet_basic";
                e.displayName = "Ramjet Engine";
                e.category = PartCategory.MissileEngine;
                e.tier = TechTier.Tier2_Advanced;
                e.researchCost = 160;
                e.buildCost = 100;
                e.massKg = 6f;
                e.propulsionType = PropulsionType.Ramjet;
                e.thrustNewtons = 2200f;
                e.burnTimeSeconds = 25f;
                e.maxSpeedMetersPerSecond = 680f;
                e.infraredSignature = 2.5f;
                // Optimized for sustained straight-line cruise, not hard corrections —
                // see MissileEngineDefinition.maneuverabilityMultiplier.
                e.maneuverabilityMultiplier = 0.85f;
            });

            // Scramjet: hypersonic-capable supersonic-combustion ramjet. Tier 4 exotic
            // tech (per the design doc's "Scramjet components" high-tier material
            // callout) — heaviest and most expensive engine, but by far the highest top
            // speed. Same airspeed-gated-ignition caveat as the ramjet above applies,
            // more so.
            CreateOrReplace<MissileEngineDefinition>($"{MissilesDir}/Engine_Scramjet.asset", e =>
            {
                e.id = "missile_engine_scramjet_basic";
                e.displayName = "Scramjet Engine";
                e.category = PartCategory.MissileEngine;
                e.tier = TechTier.Tier4_Hypersonic;
                e.researchCost = 300;
                e.buildCost = 200;
                e.massKg = 10f;
                e.propulsionType = PropulsionType.Scramjet;
                e.thrustNewtons = 4000f;
                e.burnTimeSeconds = 18f;
                e.maxSpeedMetersPerSecond = 1700f;
                e.infraredSignature = 4f;
                // The least agile of the four — built for raw hypersonic speed, not
                // maneuvering — see MissileEngineDefinition.maneuverabilityMultiplier.
                e.maneuverabilityMultiplier = 0.65f;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase2AMissileBreadthSeeder] Seeded Liquid Rocket, Ramjet, and Scramjet engine variants " +
                "under Assets/_Project/Data/Missiles/ (Engine_SolidRocket_Basic from Phase1DataSeeder covers Solid " +
                "Rocket). Not yet wired into the tech tree or Workshop picker — see class comment.");
        }

        [MenuItem("Vanquish/Phase 2A/Seed Missile Seeker Variants")]
        public static void SeedSeekerVariants()
        {
            EnsureDir(MissilesDir);

            // Wire/SACLOS: operator-guided over a physical wire or command line-of-sight
            // link rather than an onboard "seeker" in the sensing sense. Immune to
            // RF/IR jamming and countermeasures almost entirely (there's no emission to
            // jam or signature to spoof), but short range and needs the launch platform
            // to maintain LOS/guidance for the whole flight — a real Cold War-era
            // trade-off, matching the design doc's "Cold War hardware" starting tier.
            CreateOrReplace<SeekerDefinition>($"{MissilesDir}/Seeker_WireSaclos.asset", s =>
            {
                s.id = "missile_seeker_wire_saclos";
                s.displayName = "Wire/SACLOS Guidance";
                s.category = PartCategory.MissileSeeker;
                s.tier = TechTier.Tier0_Improvised;
                s.researchCost = 30;
                s.buildCost = 20;
                s.massKg = 1.5f;
                s.seekerType = SeekerType.WireOrDatalinkGuided;
                s.detectionRangeMeters = 1500f;
                s.fieldOfViewDegrees = 20f;
                s.reacquisitionTimeSeconds = 0.5f;
                s.jamResistance = 0.95f;
                s.countermeasureSusceptibility = 0.05f;
            });

            // Laser-guided: beam-riding/semi-active laser homing on a designator spot.
            // Narrow detection cone (has to see the laser spot), hard to jam
            // electronically, but smoke/laser-warning countermeasures can disrupt the
            // designation.
            CreateOrReplace<SeekerDefinition>($"{MissilesDir}/Seeker_Laser.asset", s =>
            {
                s.id = "missile_seeker_laser";
                s.displayName = "Laser-Guided Seeker";
                s.category = PartCategory.MissileSeeker;
                s.tier = TechTier.Tier1_Guided;
                s.researchCost = 100;
                s.buildCost = 65;
                s.massKg = 2.5f;
                s.seekerType = SeekerType.Laser;
                s.detectionRangeMeters = 3500f;
                s.fieldOfViewDegrees = 15f;
                s.reacquisitionTimeSeconds = 0.8f;
                s.jamResistance = 0.7f;
                s.countermeasureSusceptibility = 0.3f;
            });

            // Optical/TV: operator-guided via a video feed rather than passive homing.
            // No RF/IR emissions to jam, but the human operator can be visually confused
            // by smoke/decoys.
            CreateOrReplace<SeekerDefinition>($"{MissilesDir}/Seeker_OpticalTv.asset", s =>
            {
                s.id = "missile_seeker_optical_tv";
                s.displayName = "Optical/TV Seeker";
                s.category = PartCategory.MissileSeeker;
                s.tier = TechTier.Tier1_Guided;
                s.researchCost = 95;
                s.buildCost = 60;
                s.massKg = 2.2f;
                s.seekerType = SeekerType.Optical;
                s.detectionRangeMeters = 2500f;
                s.fieldOfViewDegrees = 25f;
                s.reacquisitionTimeSeconds = 1.2f;
                s.jamResistance = 0.9f;
                s.countermeasureSusceptibility = 0.35f;
            });

            // SARH (Semi-Active Radar Homing): homes on radar energy reflected off the
            // target from the launching platform's own illuminating radar — needs that
            // illumination maintained for the whole flight (classic early Cold War
            // radar-guided missile, e.g. early AIM-7 Sparrow-style). Decent range,
            // moderate jam resistance, and chaff can spoof it since it has to pick the
            // real target's return out of clutter.
            CreateOrReplace<SeekerDefinition>($"{MissilesDir}/Seeker_SARH.asset", s =>
            {
                s.id = "missile_seeker_sarh";
                s.displayName = "Semi-Active Radar Homing (SARH)";
                s.category = PartCategory.MissileSeeker;
                s.tier = TechTier.Tier1_Guided;
                s.researchCost = 110;
                s.buildCost = 75;
                s.massKg = 3f;
                s.seekerType = SeekerType.SemiActiveRadar;
                s.detectionRangeMeters = 5000f;
                s.fieldOfViewDegrees = 20f;
                s.reacquisitionTimeSeconds = 1.5f;
                s.jamResistance = 0.4f;
                s.countermeasureSusceptibility = 0.5f;
            });

            // ARH (Active Radar Homing): carries its own radar transmitter/receiver —
            // true fire-and-forget, longest range of the radar seekers, but the classic
            // target for chaff/ECM (needs ECCM upgrades, deferred to 2C) and is the
            // heaviest seeker here.
            CreateOrReplace<SeekerDefinition>($"{MissilesDir}/Seeker_ARH.asset", s =>
            {
                s.id = "missile_seeker_arh";
                s.displayName = "Active Radar Homing (ARH)";
                s.category = PartCategory.MissileSeeker;
                s.tier = TechTier.Tier2_Advanced;
                s.researchCost = 180;
                s.buildCost = 120;
                s.massKg = 4f;
                s.seekerType = SeekerType.ActiveRadar;
                s.detectionRangeMeters = 8000f;
                s.fieldOfViewDegrees = 25f;
                s.reacquisitionTimeSeconds = 1f;
                s.jamResistance = 0.3f;
                s.countermeasureSusceptibility = 0.55f;
            });

            // Imaging IR: upgrade over the Tier 0 basic-reticle Infrared seeker — builds
            // an actual thermal image of the target rather than tracking a single hot
            // point, so it can tell a flare from the real target much better (much lower
            // countermeasureSusceptibility than Seeker_IR_Basic) and sees further.
            CreateOrReplace<SeekerDefinition>($"{MissilesDir}/Seeker_ImagingIR.asset", s =>
            {
                s.id = "missile_seeker_imaging_ir";
                s.displayName = "Imaging Infrared Seeker";
                s.category = PartCategory.MissileSeeker;
                s.tier = TechTier.Tier2_Advanced;
                s.researchCost = 160;
                s.buildCost = 100;
                s.massKg = 2.8f;
                s.seekerType = SeekerType.ImagingInfrared;
                s.detectionRangeMeters = 4500f;
                s.fieldOfViewDegrees = 20f;
                s.reacquisitionTimeSeconds = 0.6f;
                s.jamResistance = 0.6f;
                s.countermeasureSusceptibility = 0.2f;
            });

            // Multi-spectral: fuses IR + radar (and optionally optical) sensing so
            // decoys/jamming effective against only one spectrum don't break lock —
            // the best all-around seeker, heaviest and most expensive, Tier 3
            // stealth/ECCM-era tech.
            CreateOrReplace<SeekerDefinition>($"{MissilesDir}/Seeker_MultiSpectral.asset", s =>
            {
                s.id = "missile_seeker_multispectral";
                s.displayName = "Multi-Spectral Seeker";
                s.category = PartCategory.MissileSeeker;
                s.tier = TechTier.Tier3_Stealth;
                s.researchCost = 260;
                s.buildCost = 160;
                s.massKg = 5f;
                s.seekerType = SeekerType.MultiSpectral;
                s.detectionRangeMeters = 9000f;
                s.fieldOfViewDegrees = 30f;
                s.reacquisitionTimeSeconds = 0.4f;
                s.jamResistance = 0.8f;
                s.countermeasureSusceptibility = 0.1f;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase2AMissileBreadthSeeder] Seeded Wire/SACLOS, Laser, Optical/TV, SARH, ARH, Imaging IR, " +
                "and Multi-Spectral seeker variants under Assets/_Project/Data/Missiles/ (Seeker_IR_Basic from " +
                "Phase1DataSeeder covers basic Infrared). Not yet wired into the tech tree or Workshop picker — " +
                "see class comment.");
        }

        [MenuItem("Vanquish/Phase 2A/Seed Missile Countermeasure & Jamming Variants")]
        public static void SeedCountermeasureAndJammingVariants()
        {
            EnsureDir(MissilesDir);

            // Flare/Chaff Dispenser: active decoys. Doesn't change RCS/IR signature
            // (multipliers stay at 1), just carries charges that can spoof an incoming
            // seeker lock with some probability per use.
            CreateOrReplace<CountermeasureDefinition>($"{MissilesDir}/Countermeasure_FlareChaffDispenser.asset", c =>
            {
                c.id = "missile_countermeasure_flarechaff";
                c.displayName = "Flare/Chaff Dispenser";
                c.category = PartCategory.MissileCountermeasure;
                c.tier = TechTier.Tier1_Guided;
                c.researchCost = 80;
                c.buildCost = 50;
                c.massKg = 1.5f;
                c.radarCrossSectionMultiplier = 1f;
                c.infraredSignatureMultiplier = 1f;
                c.maxGForceBonus = 0f;
                c.decoyCharges = 4;
                c.decoySuccessChance = 0.35f;
            });

            // RCS-Shaping Package: passive stealth shaping/coating, not an active decoy
            // (no charges). Meaningfully cuts radar cross-section (and a little IR via
            // shaped exhaust routing) at a mass/cost premium — Tier 3 stealth-era tech.
            CreateOrReplace<CountermeasureDefinition>($"{MissilesDir}/Countermeasure_RcsShaping.asset", c =>
            {
                c.id = "missile_countermeasure_rcsshaping";
                c.displayName = "RCS-Shaping Package";
                c.category = PartCategory.MissileCountermeasure;
                c.tier = TechTier.Tier3_Stealth;
                c.researchCost = 220;
                c.buildCost = 140;
                c.massKg = 3f;
                c.radarCrossSectionMultiplier = 0.4f;
                c.infraredSignatureMultiplier = 0.9f;
                c.maxGForceBonus = 0f;
                c.decoyCharges = 0;
                c.decoySuccessChance = 0f;
            });

            // ECM Jamming Pod: degrades nearby enemy seeker lock quality (pure ECM, no
            // counter-jamming of its own). Runtime consumption of jammingStrength/
            // jammingRangeMeters against DetectionSensor is 2C work (see 2A's technical
            // notes) — this is the data/asset side only.
            CreateOrReplace<JammingDefinition>($"{MissilesDir}/Jamming_EcmPod.asset", j =>
            {
                j.id = "missile_jamming_ecmpod";
                j.displayName = "ECM Jamming Pod";
                j.category = PartCategory.MissileJamming;
                j.tier = TechTier.Tier2_Advanced;
                j.researchCost = 170;
                j.buildCost = 110;
                j.massKg = 3.5f;
                j.jammingStrength = 0.6f;
                j.jammingRangeMeters = 1500f;
                j.counterJammingStrength = 0f;
                j.powerDrawWatts = 250f;
            });

            // ECCM Suite: the counter-jamming half — resists being jammed rather than
            // jamming others. Lighter and cheaper than the ECM pod (no transmitter
            // needed, just filtering/frequency-hopping receiver hardware).
            CreateOrReplace<JammingDefinition>($"{MissilesDir}/Jamming_EccmSuite.asset", j =>
            {
                j.id = "missile_jamming_eccmsuite";
                j.displayName = "ECCM Suite";
                j.category = PartCategory.MissileJamming;
                j.tier = TechTier.Tier2_Advanced;
                j.researchCost = 150;
                j.buildCost = 95;
                j.massKg = 2.5f;
                j.jammingStrength = 0f;
                j.jammingRangeMeters = 0f;
                j.counterJammingStrength = 0.5f;
                j.powerDrawWatts = 150f;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase2AMissileBreadthSeeder] Seeded Flare/Chaff Dispenser and RCS-Shaping Package " +
                "(CountermeasureDefinition) and ECM Jamming Pod and ECCM Suite (JammingDefinition) under " +
                "Assets/_Project/Data/Missiles/. Not yet wired into the tech tree or Workshop picker — see class " +
                "comment. Runtime consumption of these stats (jamming affecting DetectionSensor, decoys breaking " +
                "an active lock) is 2C work, not this seeder.");
        }

        /// <summary>
        /// Wires every Phase 2A missile-breadth part asset (seeded by the four
        /// "Seed Missile ... Variants" menu items above) behind its own TechNode, so
        /// WorkshopController's part picker has something unlockable to show. Requires
        /// Phase1DataSeeder's Tier-0 nodes (TN_01-TN_04) and all four variant seeders to
        /// have already run. Idempotent, like the rest of this file.
        ///
        /// Progression is chained within each part category (e.g. Engine_Scramjet
        /// requires Engine_Ramjet requires the Tier-0 solid rocket node) rather than
        /// every variant gating on the same single Tier-0 node, so higher-tier variants
        /// represent genuine research progression instead of all being immediately
        /// available together.
        /// </summary>
        [MenuItem("Vanquish/Phase 2A/Seed Missile Breadth Tech Nodes")]
        public static void SeedTechTreeNodes()
        {
            EnsureDir(TechTreeDir);

            var tnMissileAirframe = LoadNode("TN_01_MissileAirframe");
            var tnMissileEngine = LoadNode("TN_02_MissileEngine");
            var tnMissileSeeker = LoadNode("TN_03_MissileSeeker");
            var tnMissilePayload = LoadNode("TN_04_MissilePayload");

            if (tnMissileAirframe == null || tnMissileEngine == null || tnMissileSeeker == null || tnMissilePayload == null)
            {
                Debug.LogError("[Phase2AMissileBreadthSeeder] Missing Phase 1 tech nodes (TN_01-TN_04) — run " +
                    "Vanquish/Phase 1/Seed Tier-0 Data first.");
                return;
            }

            // ---- Airframes: linear Interceptor -> Heavy Strike -> Hypersonic
            // progression off the Tier-0 basic airframe node — each tier trades
            // maneuverability/stealth for a bigger MTOW budget, not a strict
            // upgrade. ----
            var tnInterceptor = CreatePartTechNode(LoadPart<MissileAirframeDefinition>("Airframe_Interceptor"), tnMissileAirframe);
            var tnHeavyStrike = CreatePartTechNode(LoadPart<MissileAirframeDefinition>("Airframe_HeavyStrike"), tnInterceptor);
            CreatePartTechNode(LoadPart<MissileAirframeDefinition>("Airframe_Hypersonic"), tnHeavyStrike);

            // ---- Payloads: Grenade/Shaped Charge/Kinetic branch off the Tier-0
            // HE-Frag node directly; Cluster is a further upgrade off Shaped Charge. ----
            var tnGrenade = CreatePartTechNode(LoadPart<MissilePayloadDefinition>("Payload_Grenade"), tnMissilePayload);
            var tnShapedCharge = CreatePartTechNode(LoadPart<MissilePayloadDefinition>("Payload_ShapedCharge"), tnMissilePayload);
            CreatePartTechNode(LoadPart<MissilePayloadDefinition>("Payload_Kinetic"), tnMissilePayload);
            CreatePartTechNode(LoadPart<MissilePayloadDefinition>("Payload_Cluster"), tnShapedCharge);

            // ---- Engines: linear Solid Rocket -> Liquid Rocket -> Ramjet -> Scramjet
            // progression, matching the design doc's Cold War -> hypersonic spectrum. ----
            var tnLiquidRocket = CreatePartTechNode(LoadPart<MissileEngineDefinition>("Engine_LiquidRocket"), tnMissileEngine);
            var tnRamjet = CreatePartTechNode(LoadPart<MissileEngineDefinition>("Engine_Ramjet"), tnLiquidRocket);
            CreatePartTechNode(LoadPart<MissileEngineDefinition>("Engine_Scramjet"), tnRamjet);

            // ---- Seekers: Wire/SACLOS, Laser, Optical/TV, and SARH all branch directly
            // off the Tier-0 basic-IR node (they're alternative Tier 0/1 guidance
            // philosophies, not upgrades of each other); ARH upgrades from SARH (both
            // radar-illumination based); Imaging IR upgrades from the base IR seeker;
            // Multi-Spectral fuses ARH + Imaging IR, so it requires both. ----
            CreatePartTechNode(LoadPart<SeekerDefinition>("Seeker_WireSaclos"), tnMissileSeeker);
            CreatePartTechNode(LoadPart<SeekerDefinition>("Seeker_Laser"), tnMissileSeeker);
            CreatePartTechNode(LoadPart<SeekerDefinition>("Seeker_OpticalTv"), tnMissileSeeker);
            var tnSarh = CreatePartTechNode(LoadPart<SeekerDefinition>("Seeker_SARH"), tnMissileSeeker);
            var tnArh = CreatePartTechNode(LoadPart<SeekerDefinition>("Seeker_ARH"), tnSarh);
            var tnImagingIr = CreatePartTechNode(LoadPart<SeekerDefinition>("Seeker_ImagingIR"), tnMissileSeeker);
            CreatePartTechNode(LoadPart<SeekerDefinition>("Seeker_MultiSpectral"), tnArh, tnImagingIr);

            // ---- Countermeasures & jamming: no Phase 1 node of their own category
            // exists yet (these are new PartCategory values), so the entry-level items
            // gate behind the base missile airframe node; RCS-Shaping upgrades from the
            // Flare/Chaff Dispenser (active decoys -> passive stealth shaping). ----
            var tnFlareChaff = CreatePartTechNode(LoadPart<CountermeasureDefinition>("Countermeasure_FlareChaffDispenser"), tnMissileAirframe);
            CreatePartTechNode(LoadPart<CountermeasureDefinition>("Countermeasure_RcsShaping"), tnFlareChaff);
            CreatePartTechNode(LoadPart<JammingDefinition>("Jamming_EcmPod"), tnMissileAirframe);
            CreatePartTechNode(LoadPart<JammingDefinition>("Jamming_EccmSuite"), tnMissileAirframe);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase2AMissileBreadthSeeder] Seeded 21 TechNodes (TN_2A_*) gating the Phase 2A missile " +
                "breadth variants under Assets/_Project/Data/TechTree/. Re-run Vanquish/Phase 1/Build Workshop " +
                "Scene to pick these up in WorkshopController's tech tree list and part picker.");
        }

        private static TechNode LoadNode(string assetName)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TechNode>($"{TechTreeDir}/{assetName}.asset");
            if (asset == null)
                Debug.LogError($"[Phase2AMissileBreadthSeeder] Could not load tech node {assetName}.");
            return asset;
        }

        private static T LoadPart<T>(string assetName) where T : PartDefinition
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>($"{MissilesDir}/{assetName}.asset");
            if (asset == null)
                Debug.LogError($"[Phase2AMissileBreadthSeeder] Could not load part {assetName} — run the relevant " +
                    "'Seed Missile ... Variants' menu item first.");
            return asset;
        }

        /// <summary>Creates (or updates) a TechNode named "TN_2A_&lt;part.id&gt;" that unlocks exactly one part.</summary>
        private static TechNode CreatePartTechNode(PartDefinition part, params TechNode[] prerequisites)
        {
            if (part == null)
                return null;

            string nodeId = $"TN_2A_{part.id}";
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
