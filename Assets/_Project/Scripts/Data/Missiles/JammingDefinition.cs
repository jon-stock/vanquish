using UnityEngine;

namespace Vanquish.Data.Missiles
{
    /// <summary>
    /// Represents both ECM (jamming enemy seekers/radar) and ECCM (resisting enemy
    /// jamming) capability. A part can provide one, both, or neither depending on tier.
    /// </summary>
    [CreateAssetMenu(menuName = "Vanquish/Missile/Jamming Module", fileName = "NewJammingModule")]
    public class JammingDefinition : PartDefinition
    {
        [Header("Jamming (ECM) — degrades enemy lock quality")]
        [Range(0f, 1f)]
        public float jammingStrength;

        [Tooltip("Effective radius/cone in which this jamming affects enemy seekers, in meters.")]
        public float jammingRangeMeters;

        [Header("Counter-Jamming (ECCM) — resists being jammed")]
        [Range(0f, 1f)]
        public float counterJammingStrength;

        [Header("Power / Trade-offs")]
        [Tooltip("Continuous power draw — may compete with propulsion/sensor power budget on drones.")]
        public float powerDrawWatts;
    }
}
