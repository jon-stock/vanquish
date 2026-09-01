namespace Vanquish.Simulation.Flight
{
    /// <summary>
    /// Receives high-level flight commands issued by a controller (player input, AI
    /// behavior, or a future scripted objective/waypoint system) without that caller
    /// needing to know how the receiving unit actually achieves them (steering forces,
    /// climb-rate clamping, etc.) — per the Subsystem Design Deep Dive §5. Currently
    /// covers altitude-hold commands only; extend with additional command methods
    /// (heading, loiter, formation slot, ...) as those systems need a similar
    /// decoupled command interface rather than reaching directly into FlightBody.
    /// </summary>
    public interface ICommandReceiver
    {
        /// <summary>
        /// Commands the receiver to hold the given altitude (in the given reference
        /// frame) until a new command is issued. Implementations are expected to
        /// respect their own max climb-rate limits rather than snapping instantly.
        /// </summary>
        void SetAltitudeCommand(float desiredAltitudeMeters, AltitudeMode mode);
    }
}
