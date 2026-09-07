namespace Vanquish.Data
{
    /// <summary>
    /// Broad classification used for tech-tree filtering, UI grouping, and
    /// compatibility checks (e.g. a weapon bay slot only accepts Missile-category parts).
    /// </summary>
    public enum PartCategory
    {
        MissilePayload,
        MissileEngine,
        MissileAirframe,
        MissileSeeker,
        MissileFuel,
        MissileCountermeasure,
        MissileJamming,

        DronePropulsion,
        DroneAirframe,
        DroneWingOrPropeller,
        DroneHullMaterial,
        DroneEngine,
        DroneFuel,
        DroneWeaponBay,
        DroneSensorSuite,

        SupportLaunchPlatform,
        SupportRadarInstallation,
        SupportDatalink,

        /// <summary>
        /// Consolidated point-defense category (PLAN.md "Radar comes in two flavors" /
        /// point-defense consolidation, Phase 0). Covers SAM sites, gun-based
        /// interceptors/CIWS, and future directed-energy defenses as one category with
        /// an implementation sub-type (see <see cref="Support.PointDefenseImplementation"/>)
        /// rather than separate near-duplicate categories. Player-facing text can still
        /// informally call any of these "SAM" — the data model treats them as one thing.
        /// </summary>
        SupportPointDefense,
    }

    public enum TechTier
    {
        Tier0_Improvised,   // grenade-drop drones, dumb-fire munitions
        Tier1_Guided,       // basic guided missiles, subsonic drones
        Tier2_Advanced,     // supersonic propulsion, active-radar seekers, ECM
        Tier3_Stealth,      // RAM materials, low-RCS airframes, ECCM
        Tier4_Hypersonic,   // scramjets, hypersonic missiles, CCA-class drones
    }

    public enum SeekerType
    {
        None,
        Optical,
        Infrared,
        SemiActiveRadar,
        ActiveRadar,
        WireOrDatalinkGuided,

        /// <summary>
        /// Requires an active designator (player, ally, or scout) painting the
        /// target for terminal guidance (PLAN.md Command &amp; Control Model). Distinct
        /// from SemiActiveRadar (illuminated by radar, not a laser) even though both
        /// require a third party actively marking the target.
        /// </summary>
        LaserDesignated,

        /// <summary>
        /// Anti-radiation seeker — homes on an active radar's own emissions rather
        /// than RCS/IR (PLAN.md "Radar &amp; SEAD" / Wild Weasel tactic).
        /// </summary>
        AntiRadiation,
    }

    public enum PropulsionType
    {
        Electric,
        SubsonicJet,
        SupersonicJet,
        Ramjet,
        Scramjet,
        SolidRocket,
        LiquidRocket,
        HybridRocket,
    }

    public enum FuelType
    {
        Battery,
        JetFuel,
        SolidPropellant,
        LiquidPropellant,
        HybridPropellant,
    }

    public enum PayloadType
    {
        HighExplosiveFragmentation,
        ShapedCharge,
        Kinetic,
        Cluster,
        Grenade,
    }
}
