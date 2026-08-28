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

        /// <summary>New saves start with enough to research a few tech nodes before the first battle.</summary>
        public int currency = 600;

        /// <summary>IDs of TechNode assets the player has unlocked.</summary>
        public List<string> unlockedTechNodeIds = new List<string>();

        public List<SavedDesign> missileDesigns = new List<SavedDesign>();
        public List<SavedDesign> droneDesigns = new List<SavedDesign>();

        public List<string> completedMissionIds = new List<string>();
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
