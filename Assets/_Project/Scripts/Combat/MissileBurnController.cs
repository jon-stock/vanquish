using UnityEngine;
using Vanquish.Simulation.Flight;

namespace Vanquish.Combat
{
    /// <summary>
    /// Depth pass (direct user feedback: "the missile range isn't affected by
    /// decreasing the fuel"): before this existed, MissileLoadout.fuelFillFraction
    /// only ever affected mass (more fuel = more inertia to push against drag) —
    /// VehicleFactory set `flightBody.isThrusting = true` once at spawn and nothing
    /// ever turned it back off, so a missile thrust forever regardless of how much
    /// fuel it was carrying. This component gives fuel fill a real, load-bearing
    /// effect: thrust cuts out once burnTimeSeconds (scaled by the fill fraction the
    /// missile was actually loaded with) elapses, after which the missile coasts
    /// on momentum against drag/gravity like a real munition running dry — a
    /// half-full tank genuinely reaches less far than a full one, instead of only
    /// weighing less.
    /// </summary>
    public class MissileBurnController : MonoBehaviour
    {
        public FlightBody flightBody;

        [Tooltip("Effective burn time in seconds — MissileEngineDefinition.burnTimeSeconds scaled by the " +
            "design's fuelFillFraction at spawn time, not the engine's full-tank rating.")]
        public float burnTimeSeconds;

        private float _elapsedSeconds;

        private void Update()
        {
            if (flightBody == null || !flightBody.isThrusting)
                return;

            _elapsedSeconds += Time.deltaTime;
            if (_elapsedSeconds >= burnTimeSeconds)
                flightBody.isThrusting = false;
        }
    }
}
