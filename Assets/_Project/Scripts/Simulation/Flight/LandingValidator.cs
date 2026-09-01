using UnityEngine;

namespace Vanquish.Simulation.Flight
{
    /// <summary>
    /// Sink-rate/ground-speed/slope safe-landing check, per the Subsystem Design Deep
    /// Dive §6: SafeLanding = (v_vertical &lt;= v_max_vert) AND (v_horizontal &lt;=
    /// v_max_horiz) AND (slope_angle &lt;= max_landing_slope_for_surface). Pure
    /// calculation utility (no MonoBehaviour/Physics dependency) so it's headlessly
    /// testable and reusable by whichever component ends up owning "drone returned to
    /// base and is touching down" detection (out of scope for Phase 2B itself — see
    /// PLAN.md's technical note: this is scoped to whatever's needed for drones that
    /// return to a base/pad, not a full landing-gear physics model).
    /// </summary>
    public static class LandingValidator
    {
        public static bool CanLandSafely(Vector3 velocity, Vector3 surfaceNormal, LandingSurfaceType surface,
            float maxVerticalSpeedMetersPerSecond, float maxHorizontalSpeedMetersPerSecond, out string reason)
        {
            SurfaceFrictionProfile profile = SurfaceFrictionMatrix.Get(surface);

            if (!profile.isLandable)
            {
                reason = $"{surface} cannot be landed on (risk: destruction/vehicle sink).";
                return false;
            }

            float verticalSpeed = Mathf.Abs(velocity.y);
            if (verticalSpeed > maxVerticalSpeedMetersPerSecond)
            {
                reason = $"Sink rate {verticalSpeed:F1} m/s exceeds max safe {maxVerticalSpeedMetersPerSecond:F1} m/s.";
                return false;
            }

            float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            if (horizontalSpeed > maxHorizontalSpeedMetersPerSecond)
            {
                reason = $"Ground speed {horizontalSpeed:F1} m/s exceeds max safe {maxHorizontalSpeedMetersPerSecond:F1} m/s.";
                return false;
            }

            float slopeAngleDegrees = Vector3.Angle(surfaceNormal, Vector3.up);
            if (slopeAngleDegrees > profile.maxLandingSlopeDegrees)
            {
                reason = $"Slope {slopeAngleDegrees:F1}\u00b0 exceeds {surface}'s max landing slope of " +
                    $"{profile.maxLandingSlopeDegrees:F1}\u00b0.";
                return false;
            }

            reason = "Safe landing conditions met.";
            return true;
        }
    }
}
