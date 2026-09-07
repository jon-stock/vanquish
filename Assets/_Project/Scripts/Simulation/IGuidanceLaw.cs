using UnityEngine;

namespace Vanquish.Simulation.Guidance
{
    /// <summary>
    /// Strategy interface for missile guidance. Implementations compute a desired
    /// lateral acceleration (steering command) each physics tick given current and
    /// target kinematics. Kept mode-agnostic: nothing here references Combat/ or
    /// Theatre/ types, so the same guidance law behaves identically regardless of
    /// which combat-instance target type or attack/defense role triggered it.
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
