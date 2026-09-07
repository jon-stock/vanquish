namespace Vanquish.Theatre
{
    /// <summary>
    /// PLAN.md "Strategic Victory &amp; Defeat Conditions" — pluggable and scenario-
    /// configurable. Each implementation is a turn-end check over state the theatre
    /// map already tracks; a scenario picks which conditions are active rather than
    /// the engine always checking all of them.
    /// </summary>
    public interface ITheatreVictoryCondition
    {
        /// <summary>Called once per turn advance. Returns the winning faction, or null if no one has won via this condition yet.</summary>
        TheatreFaction? Evaluate(TheatreWorldState state);
    }
}
