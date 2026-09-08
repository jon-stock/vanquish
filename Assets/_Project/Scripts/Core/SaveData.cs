using System;
using System.Collections.Generic;

namespace Vanquish.Core
{
    /// <summary>
    /// Root save file schema. Kept as plain serializable data (no ScriptableObject/
    /// UnityEngine.Object references) so it can be freely JSON-serialized and
    /// versioned independently of the asset database.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>Bump when the schema changes in a breaking way; used to run migrations.</summary>
        public int saveVersion = 1;

        public int currency;

        /// <summary>IDs of TechNode assets the player has unlocked.</summary>
        public List<string> unlockedTechNodeIds = new List<string>();

        public List<SavedDesign> missileDesigns = new List<SavedDesign>();
        public List<SavedDesign> droneDesigns = new List<SavedDesign>();

        public List<string> completedMissionIds = new List<string>();

        // --- Theatre map state (PLAN.md Phase 2) ---
        // Enums are stored as their string names (not the Theatre-namespace enum
        // types directly) so this save schema doesn't need to reference/depend on
        // Vanquish.Theatre at all, and stays robust to JsonUtility's handling of enums.

        /// <summary>Every hex's current owner — only non-Neutral entries are stored, since Neutral is the default for anything not listed.</summary>
        public List<SavedHexOwnership> hexOwnership = new List<SavedHexOwnership>();

        /// <summary>Every non-destroyed site on the theatre map, in enough detail to fully restore its Site.Restore(...) state.</summary>
        public List<SavedSite> sites = new List<SavedSite>();

        /// <summary>Every Plan the player has designed at a Lab.</summary>
        public List<SavedPlan> plans = new List<SavedPlan>();

        /// <summary>Completed-production counts per Plan (matched back up by name at load time).</summary>
        public List<SavedInventoryEntry> inventory = new List<SavedInventoryEntry>();
    }

    [Serializable]
    public class SavedHexOwnership
    {
        public int q;
        public int r;
        public string owner;
    }

    [Serializable]
    public class SavedSite
    {
        public string type;
        public string owner;
        public int q;
        public int r;
        public string state;
        public int turnsRemaining;
        public float healthFraction01;
    }

    [Serializable]
    public class SavedPlan
    {
        public string name;
        public string category;
    }

    [Serializable]
    public class SavedInventoryEntry
    {
        public string planName;
        public int count;
    }

    /// <summary>
    /// A player-created design, stored as a name plus a list of PartDefinition ids
    /// (resolved back to actual assets via a lookup table at load time).
    /// </summary>
    [Serializable]
    public class SavedDesign
    {
        public string designName;
        public List<string> partIds = new List<string>();
    }
}
