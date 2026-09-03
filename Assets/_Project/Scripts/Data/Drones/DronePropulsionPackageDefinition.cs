using UnityEngine;

namespace Vanquish.Data.Drones
{
    /// <summary>
    /// A curated pairing of one PropulsionDefinition with one DroneEngineDefinition,
    /// presented to the player as a single "Propulsion" choice in the Workshop
    /// instead of two independent dropdowns.
    ///
    /// Depth pass (direct user feedback: "propulsion and engine are the same: one
    /// can go"): DesignStatsCalculator/VehicleFactory research confirmed the two
    /// slots substantially duplicate each other in practice — both contribute mass,
    /// both contribute IR signature (summed together), thrust comes solely from the
    /// engine (PropulsionDefinition.maxSpeedMetersPerSecond/
    /// accelerationMetersPerSecondSquared are never read by the simulation), and
    /// requiresForwardFlight only ever mattered from the propulsion side (the
    /// engine's copy of that field is read only by DroneCompatibility's mismatch
    /// warning, never by the actual flight model). Two dropdowns that always have to
    /// be picked in lockstep (an Electric propulsion with a Jet engine makes no
    /// sense, and nothing stopped a player from doing it before DroneCompatibility's
    /// validation pass) is exactly the kind of "why are these two separate choices"
    /// friction this type removes — mirroring DronePlanformDefinition's Airframe+Wing
    /// merge, and for the same reason: a real propulsion system and its engine are
    /// one integrated unit, not an arbitrary cross-product.
    ///
    /// Deliberately NOT a PartDefinition/tech-tree-gated entity in its own right —
    /// see DronePlanformDefinition's own doc comment for why (no independent cost/
    /// tier; "unlocked" means both halves are, via WorkshopController.
    /// IsPropulsionPackageUnlocked). DroneLoadout still has separate propulsion/
    /// engine fields — DesignStatsCalculator/VehicleFactory/DroneCompatibility are
    /// completely unaware this type exists; it only changes what the Workshop offers.
    /// </summary>
    [CreateAssetMenu(menuName = "Vanquish/Drone/Propulsion Package (Propulsion+Engine Preset)", fileName = "NewPropulsionPackage")]
    public class DronePropulsionPackageDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;

        [TextArea(2, 5)]
        public string description;

        [Header("Preset Pairing")]
        public PropulsionDefinition propulsion;
        public DroneEngineDefinition engine;
    }
}
