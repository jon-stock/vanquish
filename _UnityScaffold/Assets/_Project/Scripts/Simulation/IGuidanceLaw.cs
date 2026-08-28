using UnityEngine;

namespace Vanquish.Simulation.Guidance
{
    /// <summary>
    /// Strategy interface for missile guidance. Implementations compute a desired
    /// lateral acceleration (steering command) each physics tick given current and
    /// target kinematics. Kept mode-agnostic: used identically in Workshop test-fire
    /// and live Combat.
    /// </summary>
    public interface IGuidanceLaw
    {
        /// <summary>
        /// Compute the desired steering acceleration vector (world space, will be
        /// clamped to the missile's max-G by the flight controller).
        /// </summary>
        Vector3 ComputeSteering(
            Vector3 selfPosition,
            Vector3 selfVelocity,
            Vector3 targetPosition,
            Vector3 targetVelocity,
            float deltaTime);
    }
}
