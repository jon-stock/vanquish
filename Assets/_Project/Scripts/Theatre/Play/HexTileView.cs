using UnityEngine;
using Vanquish.Theatre;

namespace Vanquish.Theatre.Play
{
    /// <summary>
    /// The visual+clickable representation of one <see cref="HexTile"/>. Holds a
    /// direct reference to the underlying simulation tile (the source of truth —
    /// this component never mutates gameplay state itself beyond what the harness's
    /// UI actions do) and refreshes its own material color from that tile's current
    /// terrain/owner whenever <see cref="Refresh"/> is called (e.g. after a turn
    /// advances or a hex is captured).
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class HexTileView : MonoBehaviour
    {
        public HexTile Tile { get; private set; }

        private MeshRenderer _renderer;

        public void Initialize(HexTile tile)
        {
            Tile = tile;
            _renderer = GetComponent<MeshRenderer>();
            Refresh();
        }

        public void Refresh()
        {
            if (_renderer == null)
                _renderer = GetComponent<MeshRenderer>();

            _renderer.material.color = ComputeColor(Tile);
        }

        private static Color ComputeColor(HexTile tile)
        {
            Color terrainColor = tile.Terrain switch
            {
                TerrainType.Road => new Color(0.55f, 0.5f, 0.38f),
                TerrainType.Mountain => new Color(0.42f, 0.4f, 0.4f),
                _ => new Color(0.32f, 0.48f, 0.28f),
            };

            Color ownerTint = tile.Owner switch
            {
                TheatreFaction.Player => new Color(0.2f, 0.4f, 0.85f),
                TheatreFaction.Enemy => new Color(0.8f, 0.2f, 0.2f),
                _ => terrainColor,
            };

            return tile.Owner == TheatreFaction.Neutral ? terrainColor : Color.Lerp(terrainColor, ownerTint, 0.55f);
        }
    }
}
