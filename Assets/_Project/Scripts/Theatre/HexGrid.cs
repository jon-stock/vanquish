using System.Collections.Generic;
using System.Linq;

namespace Vanquish.Theatre
{
    /// <summary>
    /// The theatre map's hex grid (PLAN.md "Theatre Map Representation"). Front line
    /// is deliberately NOT a stored/tracked object — it's derived on demand each turn
    /// from ownership adjacency, so it can never desync from the actual territory
    /// state, and so a front line "shape" naturally emerges from terrain/ownership
    /// rather than needing its own maintenance code.
    /// </summary>
    public class HexGrid
    {
        private readonly Dictionary<HexCoordinate, HexTile> _tiles = new Dictionary<HexCoordinate, HexTile>();

        public IReadOnlyCollection<HexTile> Tiles => _tiles.Values;

        public HexTile GetOrAddTile(HexCoordinate coordinate, TerrainType terrain = TerrainType.Open, TheatreFaction owner = TheatreFaction.Neutral)
        {
            if (!_tiles.TryGetValue(coordinate, out HexTile tile))
            {
                tile = new HexTile(coordinate, terrain, owner);
                _tiles[coordinate] = tile;
            }

            return tile;
        }

        public HexTile GetTile(HexCoordinate coordinate) => _tiles.TryGetValue(coordinate, out HexTile tile) ? tile : null;

        public IEnumerable<HexTile> NeighborsOf(HexCoordinate coordinate)
        {
            foreach (HexCoordinate neighborCoord in coordinate.Neighbors())
            {
                HexTile neighbor = GetTile(neighborCoord);
                if (neighbor != null)
                    yield return neighbor;
            }
        }

        /// <summary>
        /// True if this tile is on the front line: owned by a non-neutral faction and
        /// adjacent to at least one tile owned by a different non-neutral faction.
        /// </summary>
        public bool IsFrontLineTile(HexCoordinate coordinate)
        {
            HexTile tile = GetTile(coordinate);
            if (tile == null || tile.Owner == TheatreFaction.Neutral)
                return false;

            foreach (HexTile neighbor in NeighborsOf(coordinate))
            {
                if (neighbor.Owner != TheatreFaction.Neutral && neighbor.Owner != tile.Owner)
                    return true;
            }

            return false;
        }

        /// <summary>All tiles currently on the front line, derived fresh — see class remarks.</summary>
        public IEnumerable<HexTile> FrontLineTiles() => _tiles.Values.Where(t => IsFrontLineTile(t.Coordinate));

        public int CountOwnedBy(TheatreFaction faction) => _tiles.Values.Count(t => t.Owner == faction);

        public int ContestableTileCount() => _tiles.Values.Count(t => t.IsPassable);

        /// <summary>Fraction (0..1) of all passable, contestable hexes currently owned by a faction.</summary>
        public float OwnershipFraction(TheatreFaction faction)
        {
            int contestable = ContestableTileCount();
            return contestable <= 0 ? 0f : CountOwnedBy(faction) / (float)contestable;
        }
    }
}
