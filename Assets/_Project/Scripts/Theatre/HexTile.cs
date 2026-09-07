namespace Vanquish.Theatre
{
    /// <summary>One hex of the theatre map — terrain plus current ownership.</summary>
    public class HexTile
    {
        public HexCoordinate Coordinate { get; }
        public TerrainType Terrain { get; set; }
        public TheatreFaction Owner { get; set; }

        public HexTile(HexCoordinate coordinate, TerrainType terrain = TerrainType.Open, TheatreFaction owner = TheatreFaction.Neutral)
        {
            Coordinate = coordinate;
            Terrain = terrain;
            Owner = owner;
        }

        /// <summary>
        /// Cost to enter this tile from an adjacent one. Roads are the backbone of
        /// theatre-scale multi-front reach (PLAN.md); mountains are impassable
        /// (returns <see cref="float.PositiveInfinity"/>).
        /// </summary>
        public float MovementCostToEnter()
        {
            switch (Terrain)
            {
                case TerrainType.Road:
                    return 0.25f;
                case TerrainType.Mountain:
                    return float.PositiveInfinity;
                case TerrainType.Open:
                default:
                    return 1f;
            }
        }

        public bool IsPassable => !float.IsPositiveInfinity(MovementCostToEnter());
    }
}
