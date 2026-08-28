using UnityEngine;

namespace Vanquish.Data
{
    /// <summary>
    /// Base class for every researchable/equippable part in the game (missile parts,
    /// drone parts, support architecture). Contains ONLY static configuration — no
    /// runtime state. Runtime state belongs on components that reference these assets.
    /// </summary>
    public abstract class PartDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable unique id used for save data references. Do not change once shipped.")]
        public string id;

        public string displayName;

        [TextArea(2, 5)]
        public string description;

        public Sprite icon;

        [Header("Classification")]
        public PartCategory category;
        public TechTier tier;

        [Header("Economy")]
        [Tooltip("Resource cost to unlock this part in the tech tree.")]
        public int researchCost;

        [Tooltip("Resource cost to manufacture one instance of this part for a design.")]
        public int buildCost;

        [Header("Physical")]
        [Tooltip("Mass contribution in kilograms.")]
        public float massKg;

        [Header("Tech Tree")]
        [Tooltip("Parts/tech nodes that must be unlocked before this one becomes available.")]
        public PartDefinition[] prerequisites;
    }
}
