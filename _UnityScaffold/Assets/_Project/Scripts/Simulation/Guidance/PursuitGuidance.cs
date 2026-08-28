using UnityEngine;

namespace Vanquish.Simulation.Guidance
{
    /// <summary>
    /// Simplest guidance law: dumb-fire "pure pursuit" — always steers directly toward
    /// the target's current position, ignoring target velocity. Represents unguided
    /// or minimally-guided early-tier munitions (e.g. grenade-drop, basic wire-guided).
    /// This is the Phase 0 prototype guidance law.
    /// </summary>
    public class PursuitGuidance : IGuidanceLaw
    {
        /// <summary>How aggressively to correct toward the target direction, in m/s^2 per radian of heading error.</summary>
        public float steeringGain = 40f;

        public Vector3 ComputeSteering(
            Vector3 selfPosition,
            Vector3 selfVelocity,
            Vector3 targetPosition,
            Vector3 targetVelocity,
            float deltaTime)
        {
            Vector3 toTarget = (targetPosition - selfPosition);
            if (toTarget.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            Vector3 desiredDirection = toTarget.normalized;
            Vector3 currentDirection = selfVelocity.sqrMagnitude > 0.0001f
                ? selfVelocity.normalized
                : desiredDirection;

            // Lateral acceleration needed to rotate current heading toward desired heading.
            Vector3 headingError = Vector3.ProjectOnPlane(desiredDirection - currentDirection, currentDirection);
            return headingError * steeringGain;
        }
    }
}
