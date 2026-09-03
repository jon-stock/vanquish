namespace Vanquish.Data.Drones
{
    /// <summary>
    /// The two mutually-exclusive flight models a drone design can use — the concept
    /// this codebase previously left implicit (inferred separately from
    /// DroneAirframeDefinition.rotorCount, WingOrPropellerDefinition.liftSurfaceType,
    /// and PropulsionDefinition.requiresForwardFlight, with nothing checking the three
    /// agreed). Introduced alongside the fixed-wing flight-model rework so the
    /// Workshop can offer an explicit "Airframe Type" toggle that filters part options
    /// to compatible choices, and so DroneCompatibility can flag a design whose parts
    /// disagree (e.g. a jet engine bolted to a quadcopter airframe) instead of quietly
    /// allowing it.
    ///
    /// Deliberately not stored as a new field on every part — each part type already
    /// has a field that implies one of these two values (see DroneCompatibility for
    /// the mapping), and duplicating that as a second explicit field per part would
    /// just create a second source of truth that could drift out of sync with the
    /// field that actually drives simulation behavior (rotorCount, liftSurfaceType,
    /// requiresForwardFlight). DroneEngineDefinition is the one exception — it had no
    /// field implying a flight model at all before this rework — see its own
    /// requiresForwardFlight field, added to close that gap specifically.
    /// </summary>
    public enum FlightConfiguration
    {
        Multirotor,
        FixedWing,
    }
}
