using UnityEngine;

namespace Vanquish.Data.Support
{
    /// <summary>
    /// Represents the player's command/datalink network capability — governs how
    /// quickly and reliably contact data and mid-course guidance updates propagate
    /// between scouts, launch platforms, and in-flight missiles.
    /// </summary>
    [CreateAssetMenu(menuName = "Vanquish/Support/Datalink Network", fileName = "NewDatalinkNetwork")]
    public class DatalinkNetworkDefinition : PartDefinition
    {
        [Header("Datalink")]
        [Tooltip("Effective range of the network in meters before signal degrades.")]
        public float rangeMeters;

        [Tooltip("Resistance to enemy jamming, 0-1.")]
        [Range(0f, 1f)]
        public float jamResistance;

        [Tooltip("If true, enables mid-course guidance updates for datalink-guided missiles.")]
        public bool supportsMidCourseUpdates;

        [Tooltip("If true, enables seeker handoff between platforms (e.g. scout designates, missile's own seeker takes over terminal phase).")]
        public bool supportsSeekerHandoff;
    }
}
