namespace Vanquish.Combat
{
    /// <summary>
    /// Which role a player/AI is playing in a combat instance. The engagement engine
    /// is symmetric (PLAN.md "Combat Instances are symmetric") — the same logic runs
    /// for both sides, only spawn setup and win-condition perspective differ.
    /// </summary>
    public enum EngagementSide
    {
        Attacker,
        Defender,
    }
}
