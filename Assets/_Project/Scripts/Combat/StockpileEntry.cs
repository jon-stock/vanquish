using System;
using Vanquish.Data;

namespace Vanquish.Combat
{
    /// <summary>
    /// One committable unit/munition type's finite count within a single combat
    /// instance (PLAN.md "Stockpile economy"). Not a save-data/theatre-map concept —
    /// Phase 0/1 stockpiles are a hardcoded, Inspector-authored per-instance loadout;
    /// Phase 2 wires this up to the theatre map's actual production/stockpile state
    /// instead (the depletion behavior here does not need to change).
    /// </summary>
    [Serializable]
    public class StockpileEntry
    {
        public PartDefinition part;
        public int startingCount;

        /// <summary>Runtime-only remaining count. Not Inspector-authored — set via Reset().</summary>
        [NonSerialized] public int remainingCount;

        public bool IsDepleted => remainingCount <= 0;

        /// <summary>Reset to full stock — call once at engagement start.</summary>
        public void Reset()
        {
            remainingCount = startingCount;
        }

        /// <summary>Attempt to commit one unit of this stockpile entry. Returns false if depleted.</summary>
        public bool TryCommitOne()
        {
            if (IsDepleted)
                return false;

            remainingCount--;
            return true;
        }
    }
}
