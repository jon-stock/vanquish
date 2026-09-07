using System.Collections.Generic;
using Vanquish.Data;

namespace Vanquish.Combat
{
    /// <summary>
    /// One side's finite loadout for a single combat instance (PLAN.md "Stockpile
    /// economy"). Phase 0/1: constructed from a hardcoded loadout (no theatre map
    /// yet). Phase 2 replaces the construction source with the theatre map's actual
    /// stockpile state; the depletion behavior here does not need to change.
    /// </summary>
    public class Stockpile
    {
        private readonly Dictionary<string, StockpileEntry> _entriesByPartId = new Dictionary<string, StockpileEntry>();

        public Stockpile(IEnumerable<StockpileEntry> entries)
        {
            foreach (StockpileEntry entry in entries)
            {
                if (entry?.part == null)
                    continue;

                entry.Reset();
                _entriesByPartId[entry.part.id] = entry;
            }
        }

        public IReadOnlyCollection<StockpileEntry> Entries => _entriesByPartId.Values;

        public int RemainingCount(string partId) =>
            _entriesByPartId.TryGetValue(partId, out StockpileEntry entry) ? entry.remainingCount : 0;

        public bool CanCommit(string partId) => RemainingCount(partId) > 0;

        /// <summary>Look up an entry's data (e.g. rawDamage/payloadSize) by part id. Null if unknown.</summary>
        public StockpileEntry GetEntry(string partId) =>
            _entriesByPartId.TryGetValue(partId, out StockpileEntry entry) ? entry : null;

        /// <summary>
        /// Commit one unit of the given part id, decrementing its remaining count.
        /// Returns false (and commits nothing) if that part type is depleted or unknown.
        /// </summary>
        public bool TryCommit(string partId) =>
            _entriesByPartId.TryGetValue(partId, out StockpileEntry entry) && entry.TryCommitOne();

        /// <summary>True once every entry in this stockpile is depleted (a Phase 0 defender-win condition).</summary>
        public bool IsFullyDepleted()
        {
            foreach (StockpileEntry entry in _entriesByPartId.Values)
            {
                if (!entry.IsDepleted)
                    return false;
            }
            return true;
        }

        public void ResetAll()
        {
            foreach (StockpileEntry entry in _entriesByPartId.Values)
                entry.Reset();
        }
    }
}
