using UnityEngine;

namespace Vanquish.Data.Drones
{
    /// <summary>
    /// Detection/recon payload. Scout drones use a high-range, low-signature sensor
    /// suite with little or no weapon bay; strike drones typically carry a lighter
    /// suite focused on targeting rather than wide-area search.
    /// </summary>
    [CreateAssetMenu(menuName = "Vanquish/Drone/Sensor Suite", fileName = "NewSensorSuite")]
    public class SensorSuiteDefinition : PartDefinition
    {
        [Header("Radar")]
        public float radarRangeMeters;
        public float radarFieldOfViewDegrees;

        [Header("Electro-Optical / Infrared")]
        public float eoIrRangeMeters;
        public float eoIrFieldOfViewDegrees;

        [Header("Electronic Support Measures (passive RF detection / radar warning)")]
        public float esmRangeMeters;

        [Header("Data Sharing")]
        [Tooltip("If true, contacts detected by this sensor are shared to the whole team's contact picture (typical of scout drones).")]
        public bool sharesContactsWithTeam = true;

        [Tooltip("Delay in seconds before shared contacts propagate over the datalink network.")]
        public float datalinkRelayDelaySeconds;
    }
}
