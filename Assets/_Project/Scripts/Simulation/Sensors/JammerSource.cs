using UnityEngine;
using Vanquish.Core;

namespace Vanquish.Simulation.Sensors
{
    /// <summary>
    /// Marks a GameObject as an active jammer, per PLAN.md Phase 2C's
    /// jamming/counter-jamming item: degrades nearby enemy DetectionSensor lock
    /// probability within jammingRangeMeters. Added by VehicleFactory when a
    /// MissileLoadout carries a JammingDefinition (the only current jamming-capable
    /// slot — see JammingDefinition's own doc comment on ECM/ECCM). Checked by
    /// DetectionSensor.Rescan against the sensor's own position (broadband ECM jams
    /// the receiver, not any one specific target-to-sensor path), offset by the
    /// sensor's own jamResistance (from JammingDefinition.counterJammingStrength,
    /// via DesignStatsCalculator's existing MissileRuntimeStats.jamResistance).
    /// </summary>
    public class JammerSource : MonoBehaviour
    {
        [Range(0f, 1f)]
        public float jammingStrength;
        public float jammingRangeMeters;
        public Team team;
    }
}
