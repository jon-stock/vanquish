using UnityEngine;
using Vanquish.Simulation.Flight;

namespace Vanquish.Combat.Play
{
    /// <summary>
    /// Gives a missile's fuel a real, load-bearing effect: thrust cuts out once
    /// burnTimeSeconds elapses, after which the missile coasts on momentum against
    /// drag like a real munition running dry, instead of thrusting forever.
    /// (Ported from the pre-pivot project.)
    /// </summary>
    public class MissileBurnController : MonoBehaviour
    {
        public FlightBody flightBody;
        public float burnTimeSeconds = 4f;

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
