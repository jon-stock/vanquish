namespace Vanquish.Theatre
{
    /// <summary>
    /// PLAN.md Theatre Map — "buildable installations placed on hexes." Mirrors the
    /// combat-instance target types (Factory/Warehouse/Base) plus theatre-only
    /// infrastructure (radar, launch platforms, recon stations) that don't
    /// necessarily correspond 1:1 to a strikeable combat-instance objective yet.
    /// </summary>
    public enum SiteType
    {
        Factory,
        Warehouse,
        Base,
        RadarInstallation,
        LaunchPlatform,
        ReconStation,
    }
}
