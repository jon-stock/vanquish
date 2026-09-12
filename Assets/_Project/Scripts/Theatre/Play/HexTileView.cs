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
        private bool _highlighted;
        private bool _selected;

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

            Color baseColor = ComputeColor(Tile);
            Color tinted = _highlighted ? Color.Lerp(baseColor, HighlightColor, 0.6f) : baseColor;
            _renderer.material.color = _selected ? Color.Lerp(tinted, SelectedColor, 0.7f) : tinted;
        }

        private static readonly Color HighlightColor = new Color(0.95f, 0.95f, 0.15f);
        private static readonly Color SelectedColor = new Color(1f, 0.55f, 0.1f);

        /// <summary>
        /// Marks this hex as a legal move destination for the currently selected
        /// army (see <see cref="TheatreMapHarness"/>'s move-highlight logic) — a
        /// bright tint blended over its normal terrain/owner color, cleared once
        /// nothing is selected/movable. Re-applies immediately via <see cref="Refresh"/>.
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (_highlighted == highlighted)
                return;

            _highlighted = highlighted;
            Refresh();
        }

        /// <summary>
        /// Marks this hex as the currently selected one (whichever hex a selected
        /// site/army/tile is on — see <see cref="TheatreMapHarness"/>) — a strong
        /// orange glow blended on top of everything else, so the player can always
        /// see at a glance what's selected, distinct from the (different-colored)
        /// move-destination highlight. Re-applies immediately via <see cref="Refresh"/>.
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (_selected == selected)
                return;

            _selected = selected;
            Refresh();
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
