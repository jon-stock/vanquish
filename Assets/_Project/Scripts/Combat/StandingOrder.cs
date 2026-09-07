namespace Vanquish.Combat
{
    /// <summary>
    /// Minimum-viable "battlefield tactics" standing order (PLAN.md Command &amp;
    /// Control Model, tier 2 — Phase 0 prototype). Executed by AI only for now;
    /// hired operators (Phase 2+) and direct manual flight control (Phase 1) both
    /// layer on top of this same concept later without changing it.
    /// </summary>
    public enum StandingOrder
    {
        /// <summary>Commit available stockpile against the objective as soon as possible.</summary>
        AttackNow,

        /// <summary>Hold committed units back; do not spend stockpile this tick.</summary>
        Hold,
    }
}
