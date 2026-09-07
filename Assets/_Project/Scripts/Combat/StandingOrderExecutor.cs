using System;

namespace Vanquish.Combat
{
    /// <summary>
    /// Minimum-viable AI executor for a <see cref="StandingOrder"/> (PLAN.md Phase 1 —
    /// "battlefield tactics ... executed by AI only for now"). Given an ordered
    /// commit priority list, it repeatedly commits the current-priority part at a
    /// fixed interval, only moving on to the next entry once the current one is
    /// depleted — this is the mechanism for making "decoys first, real strike after"
    /// a concrete, visible AI behavior (PLAN.md: "should be visible in the enemy's
    /// behavior too, not just available to the player"), not just a player option.
    /// Plain C# (no MonoBehaviour/scene dependency) so it drives either the attacker
    /// or defender side identically, and is directly unit-testable.
    /// </summary>
    public class StandingOrderExecutor
    {
        public StandingOrder Order = StandingOrder.Hold;
        public float CommitIntervalSeconds = 1f;

        /// <summary>Part ids to commit, in priority order (e.g. cheap decoys first, expensive strike units last).</summary>
        public string[] CommitPriorityPartIds;

        private float _timer;
        private int _cursor;

        /// <param name="tryCommit">Attempts to commit one unit of the given part id; returns whether it succeeded (false = depleted/unknown).</param>
        public void Tick(float deltaTime, Func<string, bool> tryCommit)
        {
            if (Order != StandingOrder.AttackNow)
                return;

            if (CommitPriorityPartIds == null || CommitPriorityPartIds.Length == 0 || tryCommit == null)
                return;

            _timer += deltaTime;
            if (_timer < CommitIntervalSeconds)
                return;

            _timer -= CommitIntervalSeconds;

            for (int i = 0; i < CommitPriorityPartIds.Length; i++)
            {
                int index = (_cursor + i) % CommitPriorityPartIds.Length;
                if (tryCommit(CommitPriorityPartIds[index]))
                {
                    // Stay on this priority entry next tick — keep spending decoys
                    // until this specific part type is depleted, then advance.
                    _cursor = index;
                    return;
                }
            }

            // Every entry in the priority list is depleted — nothing to commit.
        }
    }
}
