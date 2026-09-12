namespace Vanquish.Theatre
{
    /// <summary>
    /// Per-<see cref="SiteType"/> storage limits for produced drones/missiles, and
    /// which site types can deploy stored drones into a new field <c>Army</c> (see
    /// <c>Theatre.Play.Army</c>/<c>TheatreMapHarness</c>). Placeholder tuning
    /// numbers, same as everywhere else in this POC (<see cref="SiteBuildCatalog"/>).
    /// Factories deliberately have no storage cap of their own — finished
    /// production must be queued straight into an Airfield or Warehouse with room
    /// (see <c>TheatreMapHarness.TryQueueProduction</c>).
    /// </summary>
    public static class SiteStorageCatalog
    {
        public static int DroneCapacity(SiteType type) => type switch
        {
            SiteType.LaunchPlatform => 50,   // "Airfield"
            SiteType.Warehouse => 1000,
            _ => 0,
        };

        public static int MissileCapacity(SiteType type) => type switch
        {
            SiteType.LaunchPlatform => 200,  // "Airfield"
            SiteType.Warehouse => 4000,
            _ => 0,
        };

        public static bool CanStore(SiteType type) => DroneCapacity(type) > 0 || MissileCapacity(type) > 0;

        /// <summary>Only Airfields (SiteType.LaunchPlatform) can deploy their stored drones/missiles into a new field army.</summary>
        public static bool CanDeployArmies(SiteType type) => type == SiteType.LaunchPlatform;
    }
}
