using UnityEngine;

namespace Vanquish.Theatre.Play
{
    /// <summary>Which slot on a <see cref="DronePlan"/> a <see cref="PlanPartOption"/> fills.</summary>
    public enum PartSlot
    {
        Propeller,
        Battery,
        Warhead,
        Guidance,
        Propulsion,
    }

    /// <summary>
    /// One selectable part option for a given <see cref="PartSlot"/> — a design-time
    /// choice made at the Lab's Design window (<see cref="DesignController"/>), not a
    /// separate <c>Data.PartDefinition</c> asset (see <see cref="TheatreTechCatalog"/>'s
    /// doc comment for why this project's theatre-map systems use plain hardcoded
    /// catalogs instead of ScriptableObject assets). <see cref="RequiredTechId"/>
    /// gates whether a design can select this option — see
    /// <see cref="TheatreMapHarness.IsTechUnlocked"/> — except for the synthetic
    /// always-available "no seeker" Guidance option, which has no backing tech node.
    /// The stat fields are flat additive bonuses on top of a category's base hull
    /// stats (see <see cref="DronePlan"/>); <see cref="VisualColor"/>/
    /// <see cref="VisualSizeMultiplier"/> feed straight into
    /// <see cref="PlanPreviewBuilder"/>/<c>Combat.Play.DroneVisualBuilder</c> so a
    /// design's actual chosen parts are visibly different, not just a stat change.
    /// </summary>
    public readonly struct PlanPartOption
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly string Description;

        /// <summary>Tech node id gating this option, or null/empty if always selectable (the baseline options, plus the synthetic "no seeker" Guidance option).</summary>
        public readonly string RequiredTechId;

        public readonly float WeightKg;
        public readonly float SpeedKph;
        public readonly float RangeKm;
        public readonly float PayloadKg;

        public readonly Color VisualColor;
        public readonly float VisualSizeMultiplier;

        public PlanPartOption(string id, string displayName, string description, string requiredTechId, float weightKg, float speedKph, float rangeKm, float payloadKg, Color visualColor, float visualSizeMultiplier)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            RequiredTechId = requiredTechId;
            WeightKg = weightKg;
            SpeedKph = speedKph;
            RangeKm = rangeKm;
            PayloadKg = payloadKg;
            VisualColor = visualColor;
            VisualSizeMultiplier = visualSizeMultiplier;
        }
    }

    public static class PlanPartCatalog
    {
        /// <summary>Synthetic id for the always-available "no seeker" Guidance option — not backed by a tech node, since an unguided rocket needs no research.</summary>
        public const string NoGuidanceId = "guide_none";

        public static readonly PlanPartOption[] Propellers =
        {
            new PlanPartOption("prop_oversized_props", "Oversized Molded Propellers", "Baseline lift. Cheap and bulky.", "prop_oversized_props",
                weightKg: 0.30f, speedKph: 70f, rangeKm: 6f, payloadKg: 1.0f, visualColor: new Color(0.82f, 0.82f, 0.8f), visualSizeMultiplier: 1.0f),
            new PlanPartOption("prop_carbon_blades", "Balanced Carbon Fiber Blades", "Lighter, more efficient, noticeably more lift.", "prop_carbon_blades",
                weightKg: 0.18f, speedKph: 85f, rangeKm: 8f, payloadKg: 1.8f, visualColor: new Color(0.05f, 0.05f, 0.06f), visualSizeMultiplier: 1.0f),
            new PlanPartOption("prop_variable_pitch", "Variable-Pitch Propeller Hubs", "Finer thrust control, best lift of any propeller.", "prop_variable_pitch",
                weightKg: 0.30f, speedKph: 95f, rangeKm: 11f, payloadKg: 3.0f, visualColor: new Color(0.16f, 0.16f, 0.18f), visualSizeMultiplier: 1.15f),
            new PlanPartOption("prop_low_rcs_rotors", "Low-RCS Composite Rotors", "Radar-attenuating; slightly less lift for the stealth shaping.", "prop_low_rcs_rotors",
                weightKg: 0.25f, speedKph: 100f, rangeKm: 13f, payloadKg: 2.6f, visualColor: new Color(0.02f, 0.02f, 0.025f), visualSizeMultiplier: 1.1f),
        };

        public static readonly PlanPartOption[] Batteries =
        {
            new PlanPartOption("power_lipo_cells", "High-Capacity LiPo Cells", "Baseline power source.", "power_lipo_cells",
                weightKg: 0.60f, speedKph: 0f, rangeKm: 8f, payloadKg: 0f, visualColor: new Color(0.07f, 0.07f, 0.08f), visualSizeMultiplier: 1.0f),
            new PlanPartOption("power_lihv_packs", "High-Voltage LiHV Smart Packs", "More usable capacity, frees up some payload budget.", "power_lihv_packs",
                weightKg: 0.65f, speedKph: 3f, rangeKm: 13f, payloadKg: 0.8f, visualColor: new Color(0.05f, 0.06f, 0.1f), visualSizeMultiplier: 1.1f),
            new PlanPartOption("power_solid_state", "Solid-State Lithium Batteries", "Denser, safer, longer endurance, and lighter for its capacity.", "power_solid_state",
                weightKg: 0.45f, speedKph: 5f, rangeKm: 20f, payloadKg: 1.8f, visualColor: new Color(0.04f, 0.09f, 0.09f), visualSizeMultiplier: 0.9f),
            new PlanPartOption("power_microturbine", "Micro-Turbine Auxiliary Power", "Hybrid power, extended range; the engine itself eats into payload gains.", "power_microturbine",
                weightKg: 1.10f, speedKph: 8f, rangeKm: 32f, payloadKg: 1.2f, visualColor: new Color(0.25f, 0.25f, 0.27f), visualSizeMultiplier: 1.4f),
        };

        public static readonly PlanPartOption[] Warheads =
        {
            new PlanPartOption("warhead_shaped_charge", "Shaped-Charge Warhead", "Baseline penetrating warhead.", "warhead_shaped_charge",
                weightKg: 0.4f, speedKph: 0f, rangeKm: 0f, payloadKg: 1.0f, visualColor: new Color(0.25f, 0.25f, 0.27f), visualSizeMultiplier: 1.0f),
            new PlanPartOption("warhead_frag_sleeve", "Fragmentation Sleeve", "Wider soft-target lethality.", "warhead_frag_sleeve",
                weightKg: 0.5f, speedKph: 0f, rangeKm: 0f, payloadKg: 1.3f, visualColor: new Color(0.25f, 0.3f, 0.15f), visualSizeMultiplier: 1.05f),
            new PlanPartOption("warhead_tandem_charge", "Tandem Shaped Charge", "Defeats reactive armor.", "warhead_tandem_charge",
                weightKg: 0.65f, speedKph: 0f, rangeKm: 0f, payloadKg: 1.6f, visualColor: new Color(0.35f, 0.08f, 0.08f), visualSizeMultiplier: 1.1f),
            new PlanPartOption("warhead_thermobaric", "Thermobaric Payload", "Fuel-air blast for fortified targets.", "warhead_thermobaric",
                weightKg: 0.8f, speedKph: 0f, rangeKm: 0f, payloadKg: 2.0f, visualColor: new Color(0.6f, 0.35f, 0.05f), visualSizeMultiplier: 1.2f),
        };

        public static readonly PlanPartOption[] Guidances =
        {
            new PlanPartOption(NoGuidanceId, "None (Unguided)", "Simple ballistic flight, no seeker.", null,
                weightKg: 0f, speedKph: 0f, rangeKm: 0f, payloadKg: 0f, visualColor: new Color(0.3f, 0.3f, 0.32f), visualSizeMultiplier: 1.0f),
            new PlanPartOption("guide_laser_seeker", "Semi-Active Laser Seeker", "Homes on a designator spot.", "guide_laser_seeker",
                weightKg: 0.10f, speedKph: 0f, rangeKm: 3f, payloadKg: 0f, visualColor: new Color(0.85f, 0.75f, 0.2f), visualSizeMultiplier: 1.0f),
            new PlanPartOption("guide_ir_seeker", "Imaging Infrared Seeker", "Fire-and-forget thermal lock-on.", "guide_ir_seeker",
                weightKg: 0.15f, speedKph: 0f, rangeKm: 5f, payloadKg: 0f, visualColor: new Color(0.75f, 0.35f, 0.15f), visualSizeMultiplier: 1.0f),
            new PlanPartOption("guide_gps_ins", "GPS/INS Midcourse Guidance", "Standoff midcourse navigation.", "guide_gps_ins",
                weightKg: 0.20f, speedKph: 0f, rangeKm: 9f, payloadKg: 0f, visualColor: new Color(0.3f, 0.6f, 0.85f), visualSizeMultiplier: 1.0f),
            new PlanPartOption("guide_mmw_radar", "Millimeter-Wave Radar Seeker", "All-weather active radar terminal seeker.", "guide_mmw_radar",
                weightKg: 0.25f, speedKph: 0f, rangeKm: 13f, payloadKg: 0f, visualColor: new Color(0.55f, 0.8f, 0.35f), visualSizeMultiplier: 1.0f),
        };

        public static readonly PlanPartOption[] Propulsions =
        {
            new PlanPartOption("mprop_solid_rocket", "Solid Rocket Motor", "Baseline single-burn booster.", "mprop_solid_rocket",
                weightKg: 0.5f, speedKph: 120f, rangeKm: 5f, payloadKg: 0f, visualColor: new Color(0.25f, 0.25f, 0.27f), visualSizeMultiplier: 1.0f),
            new PlanPartOption("mprop_dual_pulse", "Dual-Pulse Rocket Motor", "A second burn stage for range.", "mprop_dual_pulse",
                weightKg: 0.6f, speedKph: 150f, rangeKm: 9f, payloadKg: 0f, visualColor: new Color(0.2f, 0.2f, 0.24f), visualSizeMultiplier: 1.1f),
            new PlanPartOption("mprop_ramjet", "Air-Breathing Ramjet", "Sustained cruise, much longer range.", "mprop_ramjet",
                weightKg: 0.7f, speedKph: 220f, rangeKm: 18f, payloadKg: 0f, visualColor: new Color(0.45f, 0.2f, 0.1f), visualSizeMultiplier: 1.25f),
            new PlanPartOption("mprop_scramjet", "Scramjet Booster", "Hypersonic sustained flight.", "mprop_scramjet",
                weightKg: 0.9f, speedKph: 320f, rangeKm: 30f, payloadKg: 0f, visualColor: new Color(0.85f, 0.45f, 0.1f), visualSizeMultiplier: 1.4f),
        };

        public static PlanPartOption[] OptionsFor(PartSlot slot) => slot switch
        {
            PartSlot.Propeller => Propellers,
            PartSlot.Battery => Batteries,
            PartSlot.Warhead => Warheads,
            PartSlot.Guidance => Guidances,
            PartSlot.Propulsion => Propulsions,
            _ => System.Array.Empty<PlanPartOption>(),
        };

        public static PlanPartOption? Find(PartSlot slot, string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            foreach (PlanPartOption option in OptionsFor(slot))
            {
                if (option.Id == id)
                    return option;
            }
            return null;
        }

        /// <summary>The default (baseline, always-unlocked) option id for a slot — used when designing a new Plan and for older saves loading a Plan that predates a given slot.</summary>
        public static string DefaultId(PartSlot slot) => slot switch
        {
            PartSlot.Propeller => "prop_oversized_props",
            PartSlot.Battery => "power_lipo_cells",
            PartSlot.Warhead => "warhead_shaped_charge",
            PartSlot.Guidance => NoGuidanceId,
            PartSlot.Propulsion => "mprop_solid_rocket",
            _ => null,
        };
    }
}
