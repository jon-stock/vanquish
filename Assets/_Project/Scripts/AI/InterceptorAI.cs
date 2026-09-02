using Vanquish.Combat;
using Vanquish.Core;
using Vanquish.Simulation.Sensors;

namespace Vanquish.AI
{
    /// <summary>
    /// Phase 2D: the first formalized CPU archetype — aggressive, closes distance, and
    /// specifically hunts the player's strike drone rather than "whatever contact
    /// happens to be nearest." This is the Phase 1 EnemyDroneAI promoted out of
    /// Scripts/Combat/ and into Scripts/AI/ (the "CPU opponent behavior" folder
    /// docs/CODING_STANDARDS.md always described but never had content in) as its own
    /// named archetype, since PLAN.md's Phase 2D calls out that today's EnemyDroneAI is
    /// "close to this already" and just needs formalizing rather than a rewrite.
    /// Patrol/steering/fire plumbing lives on the shared DroneCombatAI base (factored
    /// out once ScoutHunterAI made the "every archetype duplicates the same loop"
    /// problem concrete); this class is now just its targeting policy.
    ///
    /// The one substantive behavior change (not just a rename): target *selection* now
    /// prefers DetectableSignature.isArmed contacts (see VehicleFactory.SpawnDrone) via
    /// TeamAwareness.GetNearestKnownEnemy(..., armedOnly: true), falling back to any
    /// known contact only if no armed one is known yet. Previously this used pure
    /// nearest-contact selection, which meant an Interceptor could end up chasing an
    /// unarmed scout escorting the strike drone simply because the scout happened to be
    /// a few meters closer — exactly the gap this PLAN.md item calls out ("engaging the
    /// player's strike drone specifically"). Future archetypes (Scout-hunter, SAM site)
    /// are expected to live alongside this as separate MonoBehaviours per Phase 2D's own
    /// technical note, not as branching modes of one shared controller.
    /// </summary>
    public class InterceptorAI : DroneCombatAI
    {
        /// <summary>
        /// Prefer the nearest known ARMED enemy (the player's strike drone) over any
        /// other same-team contact; only fall back to the nearest contact of any role
        /// if no armed contact is known yet, so this archetype still isn't inert
        /// against, e.g., a lone scout with no strike drone in the fight at all.
        /// </summary>
        protected override DetectableSignature AcquireTarget()
        {
            if (TeamAwareness.Instance == null)
                return null;

            DetectableSignature armedTarget = TeamAwareness.Instance.GetNearestKnownEnemy(Team.Enemy, transform.position, armedOnly: true);
            return armedTarget != null
                ? armedTarget
                : TeamAwareness.Instance.GetNearestKnownEnemy(Team.Enemy, transform.position);
        }
    }
}
