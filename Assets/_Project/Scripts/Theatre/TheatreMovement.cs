using System.Collections.Generic;

namespace Vanquish.Theatre
{
    /// <summary>
    /// PLAN.md "Movement &amp; multi-front reach": an army's movement budget per turn
    /// is capped by its slowest/least-capable member (the same bottleneck rule as
    /// <see cref="Vanquish.Combat.StrikePackageRange"/>, applied at theatre scale), and
    /// movement cost per hex is sharply reduced on roads and infinite on impassable
    /// terrain. This is deliberately the *only* mechanism behind "a well-placed rear
    /// site can reach multiple fronts" — no separate multi-front-reach system needed.
    /// </summary>
    public static class TheatreMovement
    {
        /// <summary>
        /// Dijkstra over the hex grid: every tile reachable from <paramref name="start"/>
        /// within <paramref name="movementBudget"/>, mapped to the cheapest cost to
        /// reach it. A budget of 0 still yields the start tile itself (cost 0) — an
        /// army that can't move at all is still "at" its own location.
        /// </summary>
        public static Dictionary<HexCoordinate, float> ComputeReachableHexes(
            HexGrid grid, HexCoordinate start, float movementBudget)
        {
            var costSoFar = new Dictionary<HexCoordinate, float> { [start] = 0f };
            var frontier = new List<HexCoordinate> { start };

            while (frontier.Count > 0)
            {
                // Simple O(n) min-extraction — theatre-map hex counts are small enough
                // that a priority queue isn't worth the complexity yet.
                int bestIndex = 0;
                for (int i = 1; i < frontier.Count; i++)
                {
                    if (costSoFar[frontier[i]] < costSoFar[frontier[bestIndex]])
                        bestIndex = i;
                }

                HexCoordinate current = frontier[bestIndex];
                frontier.RemoveAt(bestIndex);
                float currentCost = costSoFar[current];

                foreach (HexTile neighbor in grid.NeighborsOf(current))
                {
                    if (!neighbor.IsPassable)
                        continue;

                    float newCost = currentCost + neighbor.MovementCostToEnter();
                    if (newCost > movementBudget)
                        continue;

                    if (!costSoFar.TryGetValue(neighbor.Coordinate, out float existing) || newCost < existing)
                    {
                        costSoFar[neighbor.Coordinate] = newCost;
                        frontier.Add(neighbor.Coordinate);
                    }
                }
            }

            return costSoFar;
        }

        /// <summary>
        /// The movement budget for a mixed army this turn — bottlenecked by its
        /// slowest/least-capable committed unit, exactly like a real convoy (and
        /// exactly like <see cref="Vanquish.Combat.StrikePackageRange"/> at the
        /// tactical layer).
        /// </summary>
        public static float ComputeArmyMovementBudget(IEnumerable<float> unitSpeeds)
        {
            bool any = false;
            float min = float.PositiveInfinity;

            foreach (float speed in unitSpeeds)
            {
                any = true;
                if (speed < min)
                    min = speed;
            }

            return any ? min : 0f;
        }
    }
}
