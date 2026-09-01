using Vanquish.Data;
using Vanquish.Data.Missiles;

namespace Vanquish.Simulation.Guidance
{
    /// <summary>
    /// Selects the right IGuidanceLaw for a missile design, per PLAN.md Phase 2C:
    /// "WeaponController/GuidanceController need a way to pick the guidance law
    /// based on the missile's SeekerDefinition.seekerType (e.g. wire/datalink-guided
    /// early tiers stay on pursuit, radar-seeker tiers get PN)." This is the single
    /// place that mapping lives — VehicleFactory.SpawnMissile calls this and hands
    /// the result to GuidanceController.SetGuidanceLaw, rather than special-casing
    /// missile behavior anywhere else (see this file's own extension-point role in
    /// Phase 2C's technical notes).
    /// </summary>
    public static class GuidanceLawFactory
    {
        /// <summary>How often a datalink mid-course guidance wrapper re-samples its
        /// relayed target position/velocity while outside seeker range, in seconds —
        /// simulates real datalink update latency rather than continuous omniscience.</summary>
        public const float DatalinkUpdateIntervalSeconds = 2f;

        public static IGuidanceLaw Create(MissileLoadout loadout)
        {
            IGuidanceLaw terminalLaw = CreateTerminalLaw(loadout.seeker.seekerType);

            if (loadout.datalink != null && loadout.datalink.supportsMidCourseUpdates)
            {
                // Mid-course phase (outside the seeker's own detection range) always
                // uses simple pursuit toward the periodically-relayed position —
                // there's no LOS-rate benefit to PN against a stale, infrequently
                // updated position, and pursuit is cheaper/simpler for "fly toward
                // roughly where the target should be." PN only matters once the
                // missile's own seeker (terminalLaw) takes over for precision terminal
                // homing, which DatalinkMidCourseGuidance switches to automatically.
                IGuidanceLaw midCourseLaw = new PursuitGuidance();
                return new DatalinkMidCourseGuidance(midCourseLaw, terminalLaw,
                    loadout.seeker.detectionRangeMeters, DatalinkUpdateIntervalSeconds);
            }

            return terminalLaw;
        }

        private static IGuidanceLaw CreateTerminalLaw(SeekerType seekerType)
        {
            switch (seekerType)
            {
                // Radar-return-based and sensor-fusion seekers can track target
                // velocity/closing-rate well enough to fly a true PN intercept
                // profile instead of chasing the target's raw position.
                case SeekerType.SemiActiveRadar:
                case SeekerType.ActiveRadar:
                case SeekerType.MultiSpectral:
                    return new ProportionalNavigation();

                // Everything else (Optical, Infrared, ImagingInfrared, Laser,
                // WireOrDatalinkGuided, None) stays on simple pursuit — matches
                // PLAN.md's own example ("wire/datalink-guided early tiers stay on
                // pursuit"). ImagingInfrared/Laser are seeker-quality upgrades within
                // the IR/optical family (better lock-on, longer range, more jam
                // resistance) rather than a fire-control-radar-style tracking
                // upgrade, so pursuit remains the appropriate law for them too.
                default:
                    return new PursuitGuidance();
            }
        }
    }
}
