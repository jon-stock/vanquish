namespace Vanquish.Theatre.Play
{
    /// <summary>Which branch/column of the tech tree a node belongs to — see <see cref="TechTreeController"/> for how this drives its on-screen position.</summary>
    public enum TechBranch
    {
        Propeller,
        Battery,
        Avionics,
        Signature,
        Airframe,
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
        public readonly TechBranch Branch;

        /// <summary>1-based depth within <see cref="Branch"/> — simple techs sit at tier 1, more complicated ones deeper. Drives vertical position in the tree layout (see <see cref="TechTreeController"/>); not required to be contiguous (the Airframe branch's tiers deliberately skip numbers to line up roughly with the sub-component tiers they depend on).</summary>
        public readonly int Tier;

        public readonly string[] PrerequisiteIds;

        public TheatreTechNode(string id, string displayName, string description, int researchCost, TechBranch branch, int tier, params string[] prerequisiteIds)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            ResearchCost = researchCost;
            Branch = branch;
            Tier = tier;
            PrerequisiteIds = prerequisiteIds ?? System.Array.Empty<string>();
        }
    }

    public static class TheatreTechCatalog
    {
        /// <summary>Always-unlocked baseline airframe — every new game/save starts with this already researched (see TheatreMapHarness.Build/ApplySaveData).</summary>
        public const string DefaultUnlockedId = "airframe_quadcopter";

        public static readonly TheatreTechNode[] Nodes =
        {
            // ---- Propeller & Rotor Tech (always-available sub-component upgrades) ----
            new TheatreTechNode("prop_oversized_props", "Oversized Molded Propellers", "Cheap, bulky injection-molded propellers — the baseline lift solution.", 15, TechBranch.Propeller, 1),
            new TheatreTechNode("prop_carbon_blades", "Balanced Carbon Fiber Blades", "Lighter, stiffer blades for smoother, more efficient flight.", 25, TechBranch.Propeller, 2, "prop_oversized_props"),
            new TheatreTechNode("prop_variable_pitch", "Variable-Pitch Propeller Hubs", "Adjustable blade pitch for finer thrust control and better range.", 45, TechBranch.Propeller, 3, "prop_carbon_blades"),
            new TheatreTechNode("prop_low_rcs_rotors", "Low-RCS Composite Rotors", "Radar-attenuating rotor blades and hubs for stealthier flight.", 70, TechBranch.Propeller, 4, "prop_variable_pitch"),

            // ---- Battery & Power Systems ----
            new TheatreTechNode("power_lipo_cells", "High-Capacity LiPo Cells", "Standard lithium-polymer packs — the baseline power source.", 15, TechBranch.Battery, 1),
            new TheatreTechNode("power_lihv_packs", "High-Voltage LiHV Smart Packs", "Higher-voltage cells with onboard telemetry for more usable capacity.", 25, TechBranch.Battery, 2, "power_lipo_cells"),
            new TheatreTechNode("power_solid_state", "Solid-State Lithium Batteries", "Denser, safer solid-state cells for longer endurance.", 50, TechBranch.Battery, 3, "power_lihv_packs"),
            new TheatreTechNode("power_microturbine", "Micro-Turbine Auxiliary Power", "Small turbine generator for extended-range hybrid power.", 75, TechBranch.Battery, 4, "power_solid_state"),

            // ---- Avionics & Autonomy Tech ----
            new TheatreTechNode("avionics_baro_hold", "Barometric Altitude Hold", "Pressure-sensor altitude lock for stable hovering.", 15, TechBranch.Avionics, 1),
            new TheatreTechNode("avionics_encrypted_telemetry", "Dual-Band Encrypted Telemetry", "Frequency-hopping encrypted control and video link.", 30, TechBranch.Avionics, 2, "avionics_baro_hold"),
            new TheatreTechNode("avionics_satcom", "Satellite Data Link (SATCOM)", "Beyond-line-of-sight control via satellite relay.", 50, TechBranch.Avionics, 3, "avionics_encrypted_telemetry"),
            new TheatreTechNode("avionics_edge_ai", "Edge-AI Autonomous Pilot", "Onboard AI for terrain-following and target tracking without a pilot link.", 70, TechBranch.Avionics, 4, "avionics_satcom"),
            new TheatreTechNode("avionics_swarm_mesh", "Hivemind / Swarm Mesh", "Mesh-networked coordination for multi-drone swarm tactics.", 95, TechBranch.Avionics, 5, "avionics_edge_ai"),

            // ---- Signature & Survivability Tech ----
            new TheatreTechNode("sig_rap_paint", "Matte Radar-Absorbing Paint", "Radar-absorbent coating to reduce reflected signal.", 20, TechBranch.Signature, 1),
            new TheatreTechNode("sig_weapons_bay", "Internal Modular Weapons Bay", "Enclosed payload bay to reduce drag and radar signature.", 35, TechBranch.Signature, 2, "sig_rap_paint"),
            new TheatreTechNode("sig_flush_inlets", "Flush Chin Inlets & Serrated Edges", "Faceted, flush intakes and serrated panel edges to scatter radar returns.", 60, TechBranch.Signature, 3, "sig_weapons_bay"),
            new TheatreTechNode("sig_jamming_suite", "Active Radar Jamming Suite", "Onboard active jammer to degrade enemy radar tracking.", 85, TechBranch.Signature, 4, "sig_flush_inlets"),

            // ---- Airframe Evolution (architectural unlocks — each tier gates on a mix of the sub-component branches above) ----
            new TheatreTechNode(DefaultUnlockedId, "Quadcopter", "Lightweight 4-rotor frame, line-of-sight RC control, unencrypted analog video link. Always available.", 0, TechBranch.Airframe, 1),
            new TheatreTechNode("airframe_hexacopter", "Hexacopter", "6-motor redundant frame with increased payload capacity and GPS waypoint navigation.", 60, TechBranch.Airframe, 2, "prop_carbon_blades", "power_lihv_packs"),
            new TheatreTechNode("airframe_male_hale", "MALE / HALE Fixed-Wing", "Multi-day-endurance fixed-wing airframe with EO/IR targeting, a laser designator, and light guided-munition hardpoints (Bayraktar/Predator-class).", 110, TechBranch.Airframe, 4, "prop_variable_pitch", "avionics_satcom"),
            new TheatreTechNode("airframe_cca", "Collaborative Combat Aircraft (CCA)", "Transonic, low-observable jet airframe with manned-unmanned teaming, an internal weapons bay, and autonomous swarming (Anduril Fury/BAE-class).", 160, TechBranch.Airframe, 6, "prop_low_rcs_rotors", "avionics_swarm_mesh", "sig_flush_inlets"),
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

        public static string DisplayName(TechBranch branch)
        {
            switch (branch)
            {
                case TechBranch.Propeller: return "Propeller & Rotor Tech";
                case TechBranch.Battery: return "Battery & Power Systems";
                case TechBranch.Avionics: return "Avionics & Autonomy Tech";
                case TechBranch.Signature: return "Signature & Survivability";
                case TechBranch.Airframe: return "Airframe Evolution";
                default: return branch.ToString();
            }
        }
    }
}
