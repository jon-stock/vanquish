using System.Collections.Generic;

namespace Vanquish.Theatre
{
    /// <summary>
    /// PLAN.md: "a side wins by controlling at least X% of the map's contestable
    /// hexes, sustained for some minimum duration (to avoid a single lucky turn
    /// triggering it)." Both numbers are the plan's own flagged open tuning values —
    /// exposed as constructor parameters rather than hardcoded so playtesting can
    /// adjust them without code changes.
    /// </summary>
    public class TerritorialControlCondition : ITheatreVictoryCondition
    {
        private readonly float _requiredFraction;
        private readonly int _requiredConsecutiveTurns;
        private readonly Dictionary<TheatreFaction, int> _consecutiveTurnsMet = new Dictionary<TheatreFaction, int>
        {
            { TheatreFaction.Player, 0 },
            { TheatreFaction.Enemy, 0 },
        };

        public TerritorialControlCondition(float requiredFraction = 0.75f, int requiredConsecutiveTurns = 3)
        {
            _requiredFraction = requiredFraction;
            _requiredConsecutiveTurns = requiredConsecutiveTurns;
        }

        /// <summary>How many consecutive turns this faction has already sustained the ownership threshold for — save/load restoration and UI display.</summary>
        public int ConsecutiveTurnsMet(TheatreFaction faction) => _consecutiveTurnsMet.TryGetValue(faction, out int turns) ? turns : 0;

        /// <summary>Directly sets the sustain counter — save/load restoration only, not part of normal evaluation.</summary>
        public void RestoreConsecutiveTurns(TheatreFaction faction, int turns) => _consecutiveTurnsMet[faction] = turns;

        public TheatreFaction? Evaluate(TheatreWorldState state)
        {
            foreach (TheatreFaction faction in new[] { TheatreFaction.Player, TheatreFaction.Enemy })
            {
                if (state.Grid.OwnershipFraction(faction) >= _requiredFraction)
                {
                    _consecutiveTurnsMet[faction]++;
                    if (_consecutiveTurnsMet[faction] >= _requiredConsecutiveTurns)
                        return faction;
                }
                else
                {
                    _consecutiveTurnsMet[faction] = 0;
                }
            }

            return null;
        }
    }
}
