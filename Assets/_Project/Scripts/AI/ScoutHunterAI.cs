using Vanquish.Combat;
using Vanquish.Core;
using Vanquish.Simulation.Sensors;

namespace Vanquish.AI
{
    /// <summary>
    /// Phase 2D: the second CPU archetype — prioritizes killing known/likely scout
    /// drones first, since a scout's whole value is feeding its team's TeamAwareness
    /// (see TeamAwareness's own class comment); killing it blinds every other unit on
    /// that team, not just the scout itself, which is a much higher-leverage kill than
    /// whichever contact merely happens to be nearest. Shares the same patrol/steering/
    /// fire loop as InterceptorAI via the DroneCombatAI base — only the targeting
    /// policy below differs.
    ///
    /// Role discrimination reuses Interceptor's own approach (a per-contact flag baked
    /// in once at spawn time rather than a live GetComponent probe per candidate):
    /// DetectableSignature.isScout, set by VehicleFactory.SpawnDrone from
    /// SensorSuiteDefinition.sharesContactsWithTeam — exactly the mechanism this
    /// PLAN.md item itself suggested.
    /// </summary>
    public class ScoutHunterAI : DroneCombatAI
    {
        /// <summary>
        /// Prefer the nearest known contact whose sensor suite shares contacts with its
        /// team (i.e. a scout) over any other same-team contact; only fall back to the
        /// nearest contact of any role if no scout is known yet, so this archetype
        /// isn't inert when the opposing team fields no scout at all.
        /// </summary>
        protected override DetectableSignature AcquireTarget()
        {
            if (TeamAwareness.Instance == null)
                return null;

            DetectableSignature scoutTarget = TeamAwareness.Instance.GetNearestKnownScoutEnemy(Team.Enemy, transform.position);
            return scoutTarget != null
                ? scoutTarget
                : TeamAwareness.Instance.GetNearestKnownEnemy(Team.Enemy, transform.position);
        }
    }
}
