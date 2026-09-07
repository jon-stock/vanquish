using System.Collections.Generic;

namespace Vanquish.Theatre
{
    /// <summary>Bundles the theatre map's grid and sites for turn resolution/victory checks.</summary>
    public class TheatreWorldState
    {
        public HexGrid Grid { get; }
        public List<Site> Sites { get; } = new List<Site>();

        public TheatreWorldState(HexGrid grid)
        {
            Grid = grid;
        }

        public IEnumerable<Site> SitesOwnedBy(TheatreFaction faction)
        {
            foreach (Site site in Sites)
            {
                if (site.Owner == faction)
                    yield return site;
            }
        }
    }
}
