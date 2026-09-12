namespace Vanquish.Theatre.Play
{
    /// <summary>
    /// Placeholder per-<see cref="SiteType"/> combat stats (max health/hardness/
    /// soft-cap residual) used only by <see cref="TheatreCombatResolver"/> to build
    /// a combat-instance <c>Damageable</c> for a site being attacked by an army —
    /// not balanced values, same disclaimer as <see cref="SiteBuildCatalog"/>'s turn
    /// costs. Site types with no dedicated combat-instance objective type yet
    /// (LaunchPlatform/RadarInstallation/ReconStation/Lab) share one generic
    /// fallback profile and are resolved as a <c>BaseObjective</c> stand-in.
    /// </summary>
    public static class SiteCombatProfile
    {
        public readonly struct Profile
        {
            public readonly float MaxHealth;
            public readonly float Hardness;
            public readonly float SoftCapResidualFraction;

            public Profile(float maxHealth, float hardness, float softCapResidualFraction)
            {
                MaxHealth = maxHealth;
                Hardness = hardness;
                SoftCapResidualFraction = softCapResidualFraction;
            }
        }

        public static Profile For(SiteType type) => type switch
        {
            SiteType.Factory => new Profile(220f, 30f, 0.2f),
            SiteType.Warehouse => new Profile(160f, 15f, 0.15f),
            SiteType.Base => new Profile(200f, 25f, 0.2f),
            _ => new Profile(120f, 10f, 0.15f),
        };
    }
}
