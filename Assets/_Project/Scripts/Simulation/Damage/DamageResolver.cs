namespace Vanquish.Simulation.Damage
{
    /// <summary>
    /// Pure, engine-agnostic implementation of PLAN.md's "Damage & Payload Model"
    /// soft-cap rule. Deliberately has no UnityEngine dependency so it can be unit
    /// tested directly (EditMode, or even plain NUnit) without needing a Unity
    /// Editor/Player — this is the highest-value piece of Phase 0 logic to get right
    /// and cheap to verify in isolation.
    ///
    /// Rule: a payload whose size meets or exceeds the target's hardness lands full,
    /// uncapped damage (can destroy the target outright). A payload below the
    /// target's hardness can still chip damage in, but cannot push the target's
    /// health below a residual "soft cap" floor — e.g. FPV drone munitions can batter
    /// a hardened warehouse/SAM site down to a stubborn residual percentage, but
    /// can't finish the job; that requires at least one adequately-sized payload.
    /// </summary>
    public static class DamageResolver
    {
        /// <summary>
        /// Computes the new health value after one hit.
        /// </summary>
        /// <param name="currentHealth">Health before this hit.</param>
        /// <param name="maxHealth">Target's max health (used to compute the soft-cap floor).</param>
        /// <param name="hardness">
        /// Target's hardness rating. A payloadSize &gt;= hardness bypasses the soft cap
        /// entirely. A hardness &lt;= 0 means the target has no hardness at all (e.g. a
        /// flying unit rather than a hardened structure) — always full damage.
        /// </param>
        /// <param name="softCapResidualFraction">
        /// Fraction of maxHealth (0..1) that sub-threshold payloads cannot reduce the
        /// target below. Tunable per target type (see PLAN.md Risks & Open Questions —
        /// "Damage/payload soft-cap tuning").
        /// </param>
        /// <param name="rawDamage">The munition's un-mitigated damage value.</param>
        /// <param name="payloadSize">The munition's payload size/yield stat.</param>
        public static float ApplyHit(
            float currentHealth,
            float maxHealth,
            float hardness,
            float softCapResidualFraction,
            float rawDamage,
            float payloadSize)
        {
            if (rawDamage <= 0f || currentHealth <= 0f)
                return currentHealth;

            bool exceedsHardness = hardness <= 0f || payloadSize >= hardness;
            float proposed = currentHealth - rawDamage;

            if (exceedsHardness)
            {
                // Full, uncapped damage — can finish the target off regardless of any
                // earlier soft-cap floor left by weaker payloads.
                return proposed < 0f ? 0f : proposed;
            }

            float floor = maxHealth * Clamp01(softCapResidualFraction);

            // Already at or below the floor from previous sub-threshold hits: a
            // further sub-threshold hit cannot soften it any more.
            if (currentHealth <= floor)
                return currentHealth;

            return proposed < floor ? floor : proposed;
        }

        /// <summary>True once health has reached zero (only possible via a hit that exceeded hardness).</summary>
        public static bool IsDestroyed(float currentHealth) => currentHealth <= 0f;

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }
}
