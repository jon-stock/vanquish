using UnityEngine;
using Vanquish.Combat.Play;

namespace Vanquish.Theatre.Play
{
    /// <summary>
    /// The always-visible marker for one deployed <see cref="Army"/> — a small
    /// procedural multirotor built from <see cref="DroneVisualBuilder"/> (the same
    /// "no imported art" builder combat-instance drones and Lab/Factory Plan
    /// previews use), so an army reads as an actual flying drone on the map rather
    /// than an abstract token. A thin colored status disc underneath is the only
    /// piece re-tinted at runtime (owner color, dimmed when the army runs out of
    /// missiles and is no longer combat effective) — the drone visual itself keeps
    /// a fixed owner-colored accent set once at spawn, since re-tinting every one
    /// of its many small child renderers individually isn't worth the complexity
    /// for this POC. Unlike <see cref="SiteMarkerView"/> (a static installation
    /// parented once and never moved), this marker's position is refreshed every
    /// time its army moves (see <see cref="TheatreMapHarness.TryMoveArmy"/>/
    /// RefreshArmyViews). Carries its own <see cref="SphereCollider"/> (the visual's
    /// own child pieces are deliberately collider-free) so the army can be selected
    /// by clicking directly on it, per <see cref="TheatreMapHarness"/>'s click
    /// handling.
    /// </summary>
    public class ArmyMarkerView : MonoBehaviour
    {
        public Army Army { get; private set; }

        private Renderer _statusDiscRenderer;
        private GameObject _selectionRing;

        /// <param name="selectionRing">A larger, bright ring around the base — hidden by default, shown only while this army is the harness's SelectedArmy (see <see cref="SetSelected"/>).</param>
        public void Initialize(Army army, Renderer statusDiscRenderer, GameObject selectionRing)
        {
            Army = army;
            _statusDiscRenderer = statusDiscRenderer;
            _selectionRing = selectionRing;
            SetSelected(false);
            Refresh();
        }

        public void Refresh()
        {
            if (_statusDiscRenderer == null)
                return;

            Color ownerColor = Army.Owner switch
            {
                TheatreFaction.Player => new Color(0.35f, 0.65f, 1f),
                TheatreFaction.Enemy => new Color(1f, 0.35f, 0.35f),
                _ => Color.gray,
            };

            _statusDiscRenderer.material.color = Army.IsCombatEffective ? ownerColor : Color.Lerp(ownerColor, Color.gray, 0.5f);
        }

        /// <summary>Shows/hides the bright selection ring — a clear "this is the currently selected army" cue on the 3D marker itself, matching the theatre map's other selection highlights.</summary>
        public void SetSelected(bool selected)
        {
            if (_selectionRing != null)
                _selectionRing.SetActive(selected);
        }
    }
}
