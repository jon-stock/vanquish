namespace Vanquish.Theatre
{
    /// <summary>
    /// Long-term territory/site ownership on the theatre map. Deliberately separate
    /// from <see cref="Vanquish.Combat.EngagementSide"/> (Attacker/Defender), which is
    /// a per-combat-instance role, not a persistent allegiance — the same faction can
    /// be the attacker in one engagement and the defender in the next.
    /// </summary>
    public enum TheatreFaction
    {
        Neutral,
        Player,
        Enemy,
    }
}
