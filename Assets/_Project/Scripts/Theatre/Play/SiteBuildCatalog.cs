using Vanquish.Theatre;

namespace Vanquish.Theatre.Play
{
    /// <summary>One buildable site type, as offered in the theatre map's "build here" menu.</summary>
    public readonly struct SiteBuildOption
    {
        public readonly SiteType Type;
        public readonly string DisplayName;
        public readonly int TurnsToBuild;

        public SiteBuildOption(SiteType type, string displayName, int turnsToBuild)
        {
            Type = type;
            DisplayName = displayName;
            TurnsToBuild = turnsToBuild;
        }
    }

    /// <summary>
    /// The catalog of what a player can choose to build on an eligible owned hex
    /// (PLAN.md Theatre Map — "players assign researched part designs and production
    /// output" / "sites take turns to build"). Turn costs are placeholder tuning
    /// numbers for this POC, not balanced values.
    /// </summary>
    public static class SiteBuildCatalog
    {
        public static readonly SiteBuildOption[] Options =
        {
            new SiteBuildOption(SiteType.Factory, "Factory", 4),
            new SiteBuildOption(SiteType.Warehouse, "Warehouse", 2),
            new SiteBuildOption(SiteType.Base, "Base", 3),
            new SiteBuildOption(SiteType.LaunchPlatform, "Airfield", 3),
            new SiteBuildOption(SiteType.RadarInstallation, "Radar Installation", 2),
            new SiteBuildOption(SiteType.ReconStation, "Recon Station", 1),
            new SiteBuildOption(SiteType.Lab, "Research Lab", 3),
        };

        /// <summary>
        /// The player-facing name for a site type (e.g. "Airfield" rather than the
        /// raw <see cref="SiteType.LaunchPlatform"/> enum name) — use this anywhere a
        /// site type is shown in the UI (selected-hex info, production destination
        /// picker, etc.) instead of calling <c>SiteType.ToString()</c> directly, so
        /// the same site is never referred to by two different names on screen.
        /// </summary>
        public static string DisplayName(SiteType type)
        {
            foreach (SiteBuildOption option in Options)
            {
                if (option.Type == type)
                    return option.DisplayName;
            }
            return type.ToString();
        }
    }
}
