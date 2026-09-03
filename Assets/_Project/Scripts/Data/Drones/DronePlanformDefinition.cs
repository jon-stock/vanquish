using UnityEngine;

namespace Vanquish.Data.Drones
{
    /// <summary>
    /// A curated, real-world-referenced pairing of one DroneAirframeDefinition with
    /// one WingOrPropellerDefinition, presented to the player as a single "Planform"
    /// choice in the Workshop instead of two independent Airframe/Wing dropdowns.
    ///
    /// Why this exists: a real aircraft's fuselage and wing are one integrated
    /// design — nobody bolts an arbitrary generic wing onto an arbitrary fuselage.
    /// Before this, the Workshop let a player freely cross an airframe class
    /// (SmallQuad/FixedWing/FlyingWingStealth/CcaScale) with any wing type
    /// (Propeller/FixedWing/DeltaWing/VariableSweepWing/FlyingWing), producing
    /// combinations that were sometimes visually nonsensical (see DroneVisualBuilder.
    /// BuildWing's own "mismatched pairing" fallback note). This type doesn't change
    /// that underlying flexibility at the data level — DroneLoadout still has separate
    /// airframe/wingOrPropeller fields, and DesignStatsCalculator/VehicleFactory/
    /// DroneVisualBuilder are completely unaware this type exists — it only changes
    /// what the Workshop *offers* for fixed-wing designs: a short list of
    /// deliberately-designed, named planforms (see Phase3HPlanformSeeder for the
    /// concrete presets, each modeled after a specific real aircraft) rather than a
    /// free cross-product. Multirotor designs are unaffected — their Airframe and
    /// Wing-or-Rotor slots stay two independent dropdowns, since a rotor genuinely is
    /// a separable accessory choice the way a wing planform isn't.
    ///
    /// Deliberately NOT a PartDefinition/tech-tree-gated entity in its own right: a
    /// planform preset has no independent cost/mass/tier of its own — it's just a
    /// named pointer at an airframe+wing pair, both of which remain the real,
    /// separately-tech-gated parts (see WorkshopController.ResolvePlanformSelection,
    /// which treats a preset as "unlocked" iff both its airframe and wing are
    /// unlocked). Wiring a planform into the tech tree means creating one TechNode
    /// whose `unlocks` array contains both parts together — a single research
    /// purchase for the whole planform, not two.
    /// </summary>
    [CreateAssetMenu(menuName = "Vanquish/Drone/Planform (Airframe+Wing Preset)", fileName = "NewPlanform")]
    public class DronePlanformDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;

        [TextArea(2, 5)]
        public string description;

        public Sprite icon;

        [Header("Preset Pairing")]
        public DroneAirframeDefinition airframe;
        public WingOrPropellerDefinition wing;
    }
}
