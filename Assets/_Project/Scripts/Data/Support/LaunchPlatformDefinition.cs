using UnityEngine;

namespace Vanquish.Data.Support
{
    public enum LaunchPlatformType
    {
        GroundPad,
        MobileLauncher,
        CarrierDeck,
    }

    [CreateAssetMenu(menuName = "Vanquish/Support/Launch Platform", fileName = "NewLaunchPlatform")]
    public class LaunchPlatformDefinition : PartDefinition
    {
        [Header("Launch Platform")]
        public LaunchPlatformType platformType;

        [Tooltip("Number of drones/missiles that can be staged simultaneously.")]
        public int stagingCapacity;

        [Tooltip("Time in seconds to launch one unit from this platform.")]
        public float launchCycleTimeSeconds;

        [Tooltip("Structural health of the platform itself (a destroyed platform can't launch).")]
        public float health;
    }
}
