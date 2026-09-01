using UnityEngine;

namespace Vanquish.Simulation.Guidance
{
    /// <summary>
    /// Wraps two guidance laws to implement PLAN.md Phase 2C's datalink mid-course
    /// update behavior: outside the missile's own seeker range, fly toward a
    /// periodically-relayed target position/velocity (simulating real datalink
    /// update latency, not instantaneous omniscience) using a cheap mid-course law;
    /// once within seeker range, switch to full every-tick precision using the
    /// missile's real terminal guidance law, representing the seeker taking over
    /// for terminal homing.
    ///
    /// Simplification: this project's GuidanceController currently hands every
    /// guidance law the target's true, exact position/velocity each tick (there's no
    /// separate "TeamAwareness-relayed contact" data path threaded into IGuidanceLaw's
    /// interface) — see PLAN.md's own phrasing "using whatever contact TeamAwareness
    /// has". Rather than plumb TeamAwareness through the whole guidance interface for
    /// this one guidance law, this wrapper reproduces the *effect* that matters
    /// (periodic-only updates far out, continuous updates close in) by simply
    /// re-sampling the position/velocity it's given only every updateIntervalSeconds
    /// while outside terminalRangeMeters. The result is behaviorally equivalent: a
    /// datalink+PN missile flies confidently toward a slightly-stale intercept
    /// solution mid-course, then locks onto the live, precise position for the final
    /// terminal-homing seconds — without needing a wider architecture change.
    /// </summary>
    public class DatalinkMidCourseGuidance : IGuidanceLaw
    {
        private readonly IGuidanceLaw _midCourseLaw;
        private readonly IGuidanceLaw _terminalLaw;
        private readonly float _terminalRangeMeters;
        private readonly float _updateIntervalSeconds;

        private Vector3 _relayedTargetPosition;
        private Vector3 _relayedTargetVelocity;
        private float _timeSinceLastUpdate;
        private bool _hasRelayedData;

        /// <summary>True once ComputeSteering has switched to the terminal law (i.e. the
        /// target came within terminalRangeMeters at least once) — exposed for headless
        /// validation/telemetry, not required for guidance itself to function.</summary>
        public bool HasHandedOffToTerminalSeeker { get; private set; }

        public DatalinkMidCourseGuidance(IGuidanceLaw midCourseLaw, IGuidanceLaw terminalLaw,
            float terminalRangeMeters, float updateIntervalSeconds)
        {
            _midCourseLaw = midCourseLaw;
            _terminalLaw = terminalLaw;
            _terminalRangeMeters = terminalRangeMeters;
            _updateIntervalSeconds = Mathf.Max(0.05f, updateIntervalSeconds);
        }

        public Vector3 ComputeSteering(
            Vector3 selfPosition,
            Vector3 selfVelocity,
            Vector3 targetPosition,
            Vector3 targetVelocity,
            float deltaTime)
        {
            float distance = Vector3.Distance(selfPosition, targetPosition);

            if (distance <= _terminalRangeMeters)
            {
                HasHandedOffToTerminalSeeker = true;
                return _terminalLaw.ComputeSteering(selfPosition, selfVelocity, targetPosition, targetVelocity, deltaTime);
            }

            _timeSinceLastUpdate += deltaTime;
            if (!_hasRelayedData || _timeSinceLastUpdate >= _updateIntervalSeconds)
            {
                _relayedTargetPosition = targetPosition;
                _relayedTargetVelocity = targetVelocity;
                _timeSinceLastUpdate = 0f;
                _hasRelayedData = true;
            }

            return _midCourseLaw.ComputeSteering(selfPosition, selfVelocity, _relayedTargetPosition, _relayedTargetVelocity, deltaTime);
        }
    }
}
