namespace Vanquish.Theatre.Play
{
    /// <summary>Which tab of the Tech Tree modal (<see cref="TechTreeController"/>) a node belongs to.</summary>
    public enum TechGroup
    {
        Airframe,
        Missile,
    }

    /// <summary>
    /// A single researchable node in the theatre map's tech tree. Deliberately a
    /// flat, hardcoded placeholder catalog — the same "plain static data" pattern
    /// as <see cref="SiteBuildCatalog"/> — rather than <c>Data.TechTree.TechNode</c>
    /// ScriptableObject assets, since none of those exist in this project yet (see
    /// PLAN.md). Unlocking a node here only tracks player progress
    /// (persisted via <c>SaveData.unlockedTechNodeIds</c>); it does not yet grant
    /// any concrete <c>PartDefinition</c>/unit bonus — the same documented,
    /// deliberate POC gap as the rest of the Lab's design system.
    /// </summary>
    public readonly struct TheatreTechNode
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly int ResearchCost;
        public readonly TechGroup Group;

        /// <summary>Which column this node is drawn in — see <see cref="TheatreTechCatalog.ColumnsFor"/> for the ordered column list per <see cref="Group"/>.</summary>
        public readonly string Column;

        /// <summary>1-based depth within <see cref="Column"/> — simple techs sit at tier 1, more complicated ones deeper. Drives vertical position in the tree layout (see <see cref="TechTreeController"/>); not required to be contiguous (the evolution column's tiers deliberately skip numbers to line up roughly with the sub-component tiers they depend on).</summary>
        public readonly int Tier;

        public readonly string[] PrerequisiteIds;

        public TheatreTechNode(string id, string displayName, string description, int researchCost, TechGroup group, string column, int tier, params string[] prerequisiteIds)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            ResearchCost = researchCost;
            Group = group;
            Column = column;
            Tier = tier;
            PrerequisiteIds = prerequisiteIds ?? System.Array.Empty<string>();
        }
    }

    public static class TheatreTechCatalog
    {
        // ---- Airframe tab columns ----
        private const string ColPropeller = "Propeller & Rotor Tech";
        private const string ColBattery = "Battery & Power Systems";
        private const string ColAvionics = "Avionics & Autonomy Tech";
        private const string ColSignature = "Signature & Survivability";
        private const string ColAirframe = "Airframe Evolution";

        // ---- Missile tab columns ----
        private const string ColWarhead = "Warhead Tech";
        private const string ColGuidance = "Guidance & Seekers";
        private const string ColPropulsion = "Propulsion Tech";
        private const string ColCountermeasure = "Countermeasures";
        private const string ColMissile = "Missile Evolution";

        public static readonly string[] AirframeColumnOrder = { ColPropeller, ColBattery, ColAvionics, ColSignature, ColAirframe };
        public static readonly string[] MissileColumnOrder = { ColWarhead, ColGuidance, ColPropulsion, ColCountermeasure, ColMissile };

        /// <summary>Node IDs already unlocked in every new game/save — the baseline airframe/missile architecture and baseline sub-component parts, always available without research (see TheatreMapHarness.Build/ApplySaveData). Lets a fresh Lab immediately design a basic Quadcopter/Unguided Rocket using only baseline parts, per the tech tree's "Always Available" sub-component tier.</summary>
        public static readonly string[] DefaultUnlockedIds =
        {
            "airframe_quadcopter", "missile_tier1_unguided",
            "prop_oversized_props", "power_lipo_cells",
            "warhead_shaped_charge", "mprop_solid_rocket",
        };

        public static string[] ColumnsFor(TechGroup group) => group == TechGroup.Missile ? MissileColumnOrder : AirframeColumnOrder;

        public static readonly TheatreTechNode[] Nodes =
        {
            // =============================== AIRFRAME TAB ===============================

            // ---- Propeller & Rotor Tech (always-available sub-component upgrades) ----
            new TheatreTechNode("prop_oversized_props", "Oversized Molded Propellers", "Cheap, bulky injection-molded propellers — the baseline lift solution. Always available.", 0, TechGroup.Airframe, ColPropeller, 1),
            new TheatreTechNode("prop_carbon_blades", "Balanced Carbon Fiber Blades", "Lighter, stiffer blades for smoother, more efficient flight.", 25, TechGroup.Airframe, ColPropeller, 2, "prop_oversized_props"),
            new TheatreTechNode("prop_variable_pitch", "Variable-Pitch Propeller Hubs", "Adjustable blade pitch for finer thrust control and better range.", 45, TechGroup.Airframe, ColPropeller, 3, "prop_carbon_blades"),
            new TheatreTechNode("prop_low_rcs_rotors", "Low-RCS Composite Rotors", "Radar-attenuating rotor blades and hubs for stealthier flight.", 70, TechGroup.Airframe, ColPropeller, 4, "prop_variable_pitch"),

            // ---- Battery & Power Systems ----
            new TheatreTechNode("power_lipo_cells", "High-Capacity LiPo Cells", "Standard lithium-polymer packs — the baseline power source. Always available.", 0, TechGroup.Airframe, ColBattery, 1),
            new TheatreTechNode("power_lihv_packs", "High-Voltage LiHV Smart Packs", "Higher-voltage cells with onboard telemetry for more usable capacity.", 25, TechGroup.Airframe, ColBattery, 2, "power_lipo_cells"),
            new TheatreTechNode("power_solid_state", "Solid-State Lithium Batteries", "Denser, safer solid-state cells for longer endurance.", 50, TechGroup.Airframe, ColBattery, 3, "power_lihv_packs"),
            new TheatreTechNode("power_microturbine", "Micro-Turbine Auxiliary Power", "Small turbine generator for extended-range hybrid power.", 75, TechGroup.Airframe, ColBattery, 4, "power_solid_state"),

            // ---- Avionics & Autonomy Tech ----
            new TheatreTechNode("avionics_baro_hold", "Barometric Altitude Hold", "Pressure-sensor altitude lock for stable hovering.", 15, TechGroup.Airframe, ColAvionics, 1),
            new TheatreTechNode("avionics_encrypted_telemetry", "Dual-Band Encrypted Telemetry", "Frequency-hopping encrypted control and video link.", 30, TechGroup.Airframe, ColAvionics, 2, "avionics_baro_hold"),
            new TheatreTechNode("avionics_satcom", "Satellite Data Link (SATCOM)", "Beyond-line-of-sight control via satellite relay.", 50, TechGroup.Airframe, ColAvionics, 3, "avionics_encrypted_telemetry"),
            new TheatreTechNode("avionics_edge_ai", "Edge-AI Autonomous Pilot", "Onboard AI for terrain-following and target tracking without a pilot link.", 70, TechGroup.Airframe, ColAvionics, 4, "avionics_satcom"),
            new TheatreTechNode("avionics_swarm_mesh", "Hivemind / Swarm Mesh", "Mesh-networked coordination for multi-drone swarm tactics.", 95, TechGroup.Airframe, ColAvionics, 5, "avionics_edge_ai"),

            // ---- Signature & Survivability Tech ----
            new TheatreTechNode("sig_rap_paint", "Matte Radar-Absorbing Paint", "Radar-absorbent coating to reduce reflected signal.", 20, TechGroup.Airframe, ColSignature, 1),
            new TheatreTechNode("sig_weapons_bay", "Internal Modular Weapons Bay", "Enclosed payload bay to reduce drag and radar signature.", 35, TechGroup.Airframe, ColSignature, 2, "sig_rap_paint"),
            new TheatreTechNode("sig_flush_inlets", "Flush Chin Inlets & Serrated Edges", "Faceted, flush intakes and serrated panel edges to scatter radar returns.", 60, TechGroup.Airframe, ColSignature, 3, "sig_weapons_bay"),
            new TheatreTechNode("sig_jamming_suite", "Active Radar Jamming Suite", "Onboard active jammer to degrade enemy radar tracking.", 85, TechGroup.Airframe, ColSignature, 4, "sig_flush_inlets"),

            // ---- Airframe Evolution (architectural unlocks — each tier gates on a mix of the sub-component columns above) ----
            new TheatreTechNode("airframe_quadcopter", "Quadcopter", "Lightweight 4-rotor frame, line-of-sight RC control, unencrypted analog video link. Always available.", 0, TechGroup.Airframe, ColAirframe, 1),
            new TheatreTechNode("airframe_hexacopter", "Hexacopter", "6-motor redundant frame with increased payload capacity and GPS waypoint navigation.", 60, TechGroup.Airframe, ColAirframe, 2, "prop_carbon_blades", "power_lihv_packs"),
            new TheatreTechNode("airframe_male_hale", "MALE / HALE Fixed-Wing", "Multi-day-endurance fixed-wing airframe with EO/IR targeting, a laser designator, and light guided-munition hardpoints (Bayraktar/Predator-class).", 110, TechGroup.Airframe, ColAirframe, 4, "prop_variable_pitch", "avionics_satcom"),
            new TheatreTechNode("airframe_cca", "Collaborative Combat Aircraft (CCA)", "Transonic, low-observable jet airframe with manned-unmanned teaming, an internal weapons bay, and autonomous swarming (Anduril Fury/BAE-class).", 160, TechGroup.Airframe, ColAirframe, 6, "prop_low_rcs_rotors", "avionics_swarm_mesh", "sig_flush_inlets"),

            // =============================== MISSILE TAB ===============================

            // ---- Warhead Tech ----
            new TheatreTechNode("warhead_shaped_charge", "Shaped-Charge Warhead", "Focused explosive jet for penetrating armored targets. Always available.", 0, TechGroup.Missile, ColWarhead, 1),
            new TheatreTechNode("warhead_frag_sleeve", "Fragmentation Sleeve", "Pre-fragmented casing for wider anti-personnel/soft-target lethality.", 25, TechGroup.Missile, ColWarhead, 2, "warhead_shaped_charge"),
            new TheatreTechNode("warhead_tandem_charge", "Tandem Shaped Charge", "A precursor charge to defeat reactive armor before the main charge.", 45, TechGroup.Missile, ColWarhead, 3, "warhead_frag_sleeve"),
            new TheatreTechNode("warhead_thermobaric", "Thermobaric Payload", "Fuel-air explosive payload for fortified/enclosed targets.", 70, TechGroup.Missile, ColWarhead, 4, "warhead_tandem_charge"),

            // ---- Guidance & Seekers ----
            new TheatreTechNode("guide_laser_seeker", "Semi-Active Laser Seeker", "Homes on a reflected laser designator spot.", 20, TechGroup.Missile, ColGuidance, 1),
            new TheatreTechNode("guide_ir_seeker", "Imaging Infrared Seeker", "Thermal imaging seeker for fire-and-forget lock-on.", 35, TechGroup.Missile, ColGuidance, 2, "guide_laser_seeker"),
            new TheatreTechNode("guide_gps_ins", "GPS/INS Midcourse Guidance", "Satellite/inertial navigation for standoff midcourse flight.", 50, TechGroup.Missile, ColGuidance, 3, "guide_ir_seeker"),
            new TheatreTechNode("guide_mmw_radar", "Millimeter-Wave Radar Seeker", "All-weather active radar terminal seeker.", 75, TechGroup.Missile, ColGuidance, 4, "guide_gps_ins"),

            // ---- Propulsion Tech ----
            new TheatreTechNode("mprop_solid_rocket", "Solid Rocket Motor", "Single-burn solid-fuel booster — the baseline missile motor. Always available.", 0, TechGroup.Missile, ColPropulsion, 1),
            new TheatreTechNode("mprop_dual_pulse", "Dual-Pulse Rocket Motor", "A second burn stage for extended terminal energy/range.", 30, TechGroup.Missile, ColPropulsion, 2, "mprop_solid_rocket"),
            new TheatreTechNode("mprop_ramjet", "Air-Breathing Ramjet", "Sustained air-breathing cruise for much longer powered range.", 55, TechGroup.Missile, ColPropulsion, 3, "mprop_dual_pulse"),
            new TheatreTechNode("mprop_scramjet", "Scramjet Booster", "Supersonic-combustion ramjet for hypersonic sustained flight.", 90, TechGroup.Missile, ColPropulsion, 4, "mprop_ramjet"),

            // ---- Countermeasures ----
            new TheatreTechNode("cm_chaff_dispenser", "Chaff Dispenser", "Radar-reflective chaff to defeat enemy radar-guided intercepts.", 15, TechGroup.Missile, ColCountermeasure, 1),
            new TheatreTechNode("cm_ecm_pod", "Onboard ECM Pod", "Active jamming pod to degrade enemy tracking radars.", 30, TechGroup.Missile, ColCountermeasure, 2, "cm_chaff_dispenser"),
            new TheatreTechNode("cm_decoy_flares", "Decoy Flare Sequencer", "Programmed flare bursts to defeat IR-guided intercepts.", 45, TechGroup.Missile, ColCountermeasure, 3, "cm_ecm_pod"),
            new TheatreTechNode("cm_terminal_evasion", "Terminal Evasion Maneuvers", "Programmed jinking during terminal approach to defeat point defense.", 65, TechGroup.Missile, ColCountermeasure, 4, "cm_decoy_flares"),

            // ---- Missile Evolution (architectural unlocks — each tier gates on a mix of the sub-component columns above) ----
            new TheatreTechNode("missile_tier1_unguided", "Unguided Rocket", "Simple ballistic unguided rocket. Always available.", 0, TechGroup.Missile, ColMissile, 1),
            new TheatreTechNode("missile_tier2_guided", "Laser-Guided Missile", "Precision terminal guidance onto a designated target.", 60, TechGroup.Missile, ColMissile, 2, "guide_laser_seeker", "warhead_frag_sleeve"),
            new TheatreTechNode("missile_tier3_standoff", "Standoff Precision-Guided Missile", "Long-range GPS/INS midcourse flight with a dual-pulse motor for standoff strikes.", 110, TechGroup.Missile, ColMissile, 4, "guide_gps_ins", "mprop_dual_pulse"),
            new TheatreTechNode("missile_tier4_hypersonic", "Hypersonic Glide Missile", "Scramjet-boosted, radar-seeking, thermobaric-payload hypersonic weapon.", 170, TechGroup.Missile, ColMissile, 6, "mprop_scramjet", "guide_mmw_radar", "warhead_thermobaric"),
        };

        public static TheatreTechNode? Find(string id)
        {
            foreach (TheatreTechNode node in Nodes)
            {
                if (node.Id == id)
                    return node;
            }
            return null;
        }
    }
}
