using UnityEngine;
using Vanquish.Theatre;

namespace Vanquish.Theatre.Play
{
    /// <summary>
    /// The visual representation of one <see cref="Site"/> sitting on top of its
    /// hex — a distinctive procedural shape per <see cref="SiteType"/> (see
    /// <see cref="SiteVisualBuilder"/>), uniformly tinted by owner/state across all
    /// of its constituent primitives. Hides itself once its site is destroyed
    /// rather than trying to show a "wreckage" state.
    /// </summary>
    public class SiteMarkerView : MonoBehaviour
    {
        public Site Site { get; private set; }

        private Renderer[] _renderers;

        /// <summary>
        /// </summary>
        /// <param name="renderers">Every renderer built for this site's shape (see <see cref="SiteVisualBuilder.Build"/>) — all tinted identically by <see cref="Refresh"/>.</param>
        public void Initialize(Site site, Renderer[] renderers)
        {
            Site = site;
            _renderers = renderers ?? System.Array.Empty<Renderer>();
            Refresh();
        }

        public void Refresh()
        {
            gameObject.SetActive(Site.State != SiteState.Destroyed);

            if (_renderers == null || _renderers.Length == 0)
                return;

            Color ownerColor = Site.Owner switch
            {
                TheatreFaction.Player => new Color(0.25f, 0.45f, 0.9f),
                TheatreFaction.Enemy => new Color(0.85f, 0.25f, 0.25f),
                _ => Color.gray,
            };

            // Under-construction/repairing/relocating sites read as dimmer/greyed
            // out — not yet (or temporarily not) contributing, at a glance.
            bool dimmed = Site.State != SiteState.Operational;
            Color finalColor = dimmed ? Color.Lerp(ownerColor, Color.gray, 0.6f) : ownerColor;

            foreach (Renderer renderer in _renderers)
            {
                if (renderer != null)
                    renderer.material.color = finalColor;
            }
        }

        public static float HeightForType(SiteType type) => type switch
        {
            SiteType.Factory => 0.9f,
            SiteType.Base => 0.6f,
            SiteType.LaunchPlatform => 0.7f,
            SiteType.RadarInstallation => 0.8f,
            SiteType.Warehouse => 0.45f,
            SiteType.ReconStation => 0.4f,
            SiteType.Lab => 0.75f,
            _ => 0.5f,
        };
    }
}
