using UnityEngine;

namespace Vanquish.Simulation.Guidance
{
    /// <summary>
    /// True proportional navigation (PN): commanded lateral acceleration is
    /// proportional to the line-of-sight (LOS) rotation rate and closing velocity,
    /// per PLAN.md Phase 2C — "steering ∝ line-of-sight rate × closing velocity ×
    /// navigation constant", not just pursuit. Represents radar/advanced-seeker
    /// missiles (SemiActiveRadar/ActiveRadar/MultiSpectral — see GuidanceLawFactory)
    /// that lead the target's predicted intercept point rather than chasing its
    /// current position.
    ///
    /// Vector form (the standard 3D generalization of the classic 2D PN law, as used
    /// in e.g. Zarchan's "Tactical and Strategic Missile Guidance"):
    ///   ω = (r × v_rel) / |r|²         — LOS rotation rate, an axial vector ⊥ to r
    ///   Vc = -(r · v_rel) / |r|        — closing velocity (positive = approaching)
    ///   a_cmd = N · Vc · (ω × r̂)       — commanded acceleration, in-plane and ⊥ to LOS
    /// where r = targetPosition - selfPosition and v_rel = targetVelocity - selfVelocity.
    ///
    /// A key, correct property of true PN: if the LOS isn't rotating (ω = 0), the
    /// missile is already on a collision course and PN commands zero extra
    /// correction — unlike PursuitGuidance, which keeps steering toward the target's
    /// instantaneous position even on a dead-on intercept (wasting control effort
    /// and, against a maneuvering target, chronically lagging behind its turns).
    /// </summary>
    public class ProportionalNavigation : IGuidanceLaw
    {
        /// <summary>Navigation gain N — typically 3-5 for real PN-guided missiles;
        /// higher values react more aggressively to LOS rotation but can overshoot/
        /// oscillate against a fast-weaving target.</summary>
        public float navigationConstant = 4f;

        /// <summary>Below this range, LOS rate becomes numerically unstable (division by a
        /// near-zero range) and physically meaningless anyway — fall back to zero command
        /// rather than pursuit here; at this range FlightBody's own thrust/momentum and the
        /// warhead's blast/proximity fuse are what matters, not guidance correction.</summary>
        public float minimumRangeMeters = 1f;

        public Vector3 ComputeSteering(
            Vector3 selfPosition,
            Vector3 selfVelocity,
            Vector3 targetPosition,
            Vector3 targetVelocity,
            float deltaTime)
        {
            Vector3 relativePosition = targetPosition - selfPosition;
            float range = relativePosition.magnitude;
            if (range < minimumRangeMeters)
                return Vector3.zero;

            Vector3 relativeVelocity = targetVelocity - selfVelocity;
            Vector3 losUnit = relativePosition / range;

            // ω: LOS rotation rate as an axial vector (⊥ to both r and v_rel by
            // definition of the cross product), magnitude = actual d(bearing)/dt.
            Vector3 losRotationRate = Vector3.Cross(relativePosition, relativeVelocity) / (range * range);

            // Positive when closing (approaching), negative when opening (receding).
            float closingVelocity = -Vector3.Dot(relativePosition, relativeVelocity) / range;

            // Rotate the axial ω back into the engagement plane (⊥ to LOS) to get the
            // actual lateral correction direction — ω alone points out of the plane of
            // relative motion, which isn't a useful steering direction on its own.
            Vector3 correctionDirection = Vector3.Cross(losRotationRate, losUnit);

            return navigationConstant * closingVelocity * correctionDirection;
        }
    }
}
