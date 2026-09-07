using Vanquish.Simulation.Damage;

namespace Vanquish.Combat
{
    /// <summary>
    /// PLAN.md Phase 1 — "the engagement engine has no target-type-specific
    /// special-casing that would block adding the theatre map later." Every combat-
    /// instance target type (base/factory/warehouse/supply line, and later radar
    /// site) implements this instead of <see cref="EngagementController"/> knowing
    /// about concrete objective types.
    /// </summary>
    public interface IObjective
    {
        /// <summary>Every objective type has health/hardness — this is what an attacker strike that gets through actually damages.</summary>
        Damageable Damageable { get; }

        /// <summary>Advance any time-based objective state (e.g. a supply line's escort progress). No-op for static structures.</summary>
        void Tick(float deltaTime);

        /// <summary>True once the attacker has done enough to win outright (e.g. destroyed enough of the objective).</summary>
        bool HasMetAttackerWinCondition { get; }

        /// <summary>
        /// True once the defender has won on the objective's own terms, independent of
        /// the generic attacker-stockpile-exhausted/timeout defender-win path in
        /// <see cref="EngagementController"/> (e.g. a supply line reaching safety).
        /// Static structure objectives always return false here — their only
        /// defender-win path is the generic one.
        /// </summary>
        bool HasMetDefenderWinCondition { get; }
    }
}
