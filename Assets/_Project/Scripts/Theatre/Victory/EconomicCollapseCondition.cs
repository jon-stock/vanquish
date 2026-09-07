using System.Linq;

namespace Vanquish.Theatre
{
    /// <summary>
    /// PLAN.md: "a side loses when it has zero operational factories and none
    /// currently under construction — i.e. no remaining path back to production, not
    /// just a momentary zero." Only meaningful between the two playable factions
    /// (Player/Enemy) — Neutral never collapses or wins this way.
    /// </summary>
    public class EconomicCollapseCondition : ITheatreVictoryCondition
    {
        public TheatreFaction? Evaluate(TheatreWorldState state)
        {
            bool playerCollapsed = HasNoPathToProduction(state, TheatreFaction.Player);
            bool enemyCollapsed = HasNoPathToProduction(state, TheatreFaction.Enemy);

            // Both collapsing the same turn is an edge case the plan doesn't define a
            // tiebreaker for; Enemy is returned here only because Player is checked
            // first — revisit if this ever matters in practice.
            if (playerCollapsed)
                return TheatreFaction.Enemy;
            if (enemyCollapsed)
                return TheatreFaction.Player;

            return null;
        }

        private static bool HasNoPathToProduction(TheatreWorldState state, TheatreFaction faction)
        {
            return !state.SitesOwnedBy(faction).Any(site =>
                site.Type == SiteType.Factory &&
                (site.State == SiteState.Operational || site.State == SiteState.UnderConstruction || site.State == SiteState.Repairing || site.State == SiteState.Relocating));
        }
    }
}
