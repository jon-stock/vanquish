namespace Vanquish.Theatre
{
    /// <summary>
    /// PLAN.md Theatre Map — "each hex has a terrain type that affects movement cost
    /// and, in some cases, blocks movement outright." Roads are the backbone of the
    /// logistics/reach system: a well-placed rear site can service multiple front
    /// sectors specifically because road movement is so much cheaper than open
    /// terrain, per the plan's own framing.
    /// </summary>
    public enum TerrainType
    {
        Open,

        /// <summary>Sharply reduced movement cost — the backbone of multi-front logistics reach.</summary>
        Road,

        /// <summary>Blocks movement outright, regardless of an army's remaining movement budget.</summary>
        Mountain,
    }
}
