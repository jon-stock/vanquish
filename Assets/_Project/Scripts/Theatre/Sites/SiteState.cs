namespace Vanquish.Theatre
{
    /// <summary>
    /// PLAN.md Theatre Map — sites "take turns to build," "can be repaired," and "can
    /// be relocated 'with effort'" (the most expensive of the three actions,
    /// deliberately, per the plan). A site only produces/counts toward victory
    /// conditions while <see cref="Operational"/>.
    /// </summary>
    public enum SiteState
    {
        UnderConstruction,
        Operational,
        Repairing,

        /// <summary>Offline and undefended for the duration — the plan's deliberately-most-costly action.</summary>
        Relocating,

        Destroyed,
    }
}
