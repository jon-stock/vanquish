namespace Vanquish.Simulation.Flight
{
    /// <summary>
    /// Altitude command reference frame, per the Subsystem Design Deep Dive §5.
    /// AbsoluteMSL holds a fixed world-space Y regardless of terrain — lower collision
    /// risk profile computation (none needed, ignores the ground) but higher actual
    /// collision risk behind ridgelines/terrain since it never climbs to clear them.
    /// RelativeAGL holds altitude relative to the ground directly below (Y_ground +
    /// desiredAltitude), which requires downward terrain sampling but lets a unit use
    /// terrain for cover/masking, per PLAN.md Phase 2B's "a unit that can't manage
    /// altitude relative to terrain can't use terrain for cover" rationale.
    /// </summary>
    public enum AltitudeMode
    {
        AbsoluteMSL,
        RelativeAGL,
    }
}
