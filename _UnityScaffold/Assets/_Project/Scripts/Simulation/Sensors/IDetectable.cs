using UnityEngine;

namespace Vanquish.Simulation.Sensors
{
    /// <summary>
    /// Implemented by anything that can be detected by sensors (drones, missiles,
    /// installations). Exposes signature values that sensors compare against range/
    /// threshold to decide detection. Phase 0 keeps this binary (detected or not);
    /// Phase 2 upgrades to probability-based detection using these same signatures.
    /// </summary>
    public interface IDetectable
    {
        Vector3 Position { get; }

        /// <summary>Radar cross-section in m^2, after all stealth/countermeasure modifiers.</summary>
        float RadarCrossSection { get; }

        /// <summary>Infrared signature, arbitrary units after all modifiers.</summary>
        float InfraredSignature { get; }
    }
}
