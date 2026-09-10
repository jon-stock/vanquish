using System.Collections.Generic;

namespace Vanquish.Theatre
{
    public enum TheatreResult
    {
        InProgress,
        PlayerVictory,
        EnemyVictory,
    }

    /// <summary>
    /// PLAN.md Theatre Map turn loop: advancing a turn ticks every site's
    /// construction/repair/relocation progress, runs a minimal production tick
    /// (Operational factories add to their owner's resource pool — a stand-in for
    /// the fuller production/logistics simulation the plan describes, deferred), and
    /// evaluates whichever victory conditions this scenario has plugged in.
    /// Deliberately plain C# — a future UI layer drives this, it doesn't live on a
    /// scene object itself.
    /// </summary>
    public class TheatreTurnController
    {
        public TheatreWorldState World { get; }
        public int CurrentTurn { get; private set; }
        public TheatreResult Result { get; private set; } = TheatreResult.InProgress;

        /// <summary>Per-faction resource pool, fed by Operational factories each turn (Phase 2 production simplification — see PLAN.md note on the deferred full logistics simulation).</summary>
        public Dictionary<TheatreFaction, int> ResourcePool { get; } = new Dictionary<TheatreFaction, int>
        {
            { TheatreFaction.Player, 0 },
            { TheatreFaction.Enemy, 0 },
        };

        /// <summary>Resource yield per Operational factory per turn (plain int for now — no currency/economy model exists yet).</summary>
        public int ResourcePerOperationalFactoryPerTurn = 10;

        /// <summary>Per-faction research point pool, fed by Operational Labs each turn (see <see cref="TheatreTechCatalog"/>/<see cref="Play.TechTreeController"/>) — separate from <see cref="ResourcePool"/> since research spends against the tech tree, not production.</summary>
        public Dictionary<TheatreFaction, int> ResearchPool { get; } = new Dictionary<TheatreFaction, int>
        {
            { TheatreFaction.Player, 0 },
            { TheatreFaction.Enemy, 0 },
        };

        /// <summary>Research point yield per Operational Lab per turn.</summary>
        public int ResearchPerOperationalLabPerTurn = 10;

        private readonly List<ITheatreVictoryCondition> _victoryConditions;

        public TheatreTurnController(TheatreWorldState world, IEnumerable<ITheatreVictoryCondition> victoryConditions)
        {
            World = world;
            _victoryConditions = new List<ITheatreVictoryCondition>(victoryConditions);
        }

        /// <summary>Directly sets the turn counter and result — save/load restoration only (see Core/SaveData.cs), not part of normal turn resolution.</summary>
        public void RestoreProgress(int turn, TheatreResult result)
        {
            CurrentTurn = turn;
            Result = result;
        }

        public void AdvanceTurn()
        {
            if (Result != TheatreResult.InProgress)
                return;

            CurrentTurn++;

            foreach (Site site in World.Sites)
                site.Tick();

            RunProductionTick();
            EvaluateVictoryConditions();
        }

        private void RunProductionTick()
        {
            foreach (Site site in World.Sites)
            {
                if (site.Owner == TheatreFaction.Neutral || !site.IsOperational)
                    continue;

                if (site.Type == SiteType.Factory)
                    ResourcePool[site.Owner] += ResourcePerOperationalFactoryPerTurn;
                else if (site.Type == SiteType.Lab)
                    ResearchPool[site.Owner] += ResearchPerOperationalLabPerTurn;
            }
        }

        private void EvaluateVictoryConditions()
        {
            foreach (ITheatreVictoryCondition condition in _victoryConditions)
            {
                TheatreFaction? winner = condition.Evaluate(World);
                if (winner == TheatreFaction.Player)
                {
                    Result = TheatreResult.PlayerVictory;
                    return;
                }

                if (winner == TheatreFaction.Enemy)
                {
                    Result = TheatreResult.EnemyVictory;
                    return;
                }
            }
        }
    }
}
