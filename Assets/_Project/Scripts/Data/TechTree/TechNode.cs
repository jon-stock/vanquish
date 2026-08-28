using UnityEngine;

namespace Vanquish.Data.TechTree
{
    /// <summary>
    /// A single node in the research tech tree. Unlocking a node grants access to one
    /// or more PartDefinitions (or an upgrade tier of an existing one).
    /// </summary>
    [CreateAssetMenu(menuName = "Vanquish/Tech Tree/Node", fileName = "NewTechNode")]
    public class TechNode : ScriptableObject
    {
        public string id;
        public string displayName;

        [TextArea(2, 5)]
        public string description;

        public TechTier tier;

        [Tooltip("Resource cost to unlock this node.")]
        public int researchCost;

        [Tooltip("Nodes that must already be unlocked before this one can be researched.")]
        public TechNode[] prerequisites;

        [Tooltip("Part definitions this node unlocks when researched.")]
        public PartDefinition[] unlocks;

        [Tooltip("Position hint for the tech tree graph UI (editor/UI concern, not gameplay logic).")]
        public Vector2 graphPosition;
    }
}
