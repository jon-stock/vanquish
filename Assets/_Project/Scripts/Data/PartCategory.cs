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
        SupportBaseDefense,
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

        // Added in Phase 2A for full seeker spectrum breadth. Appended rather than
        // inserted alphabetically to keep the existing values' serialized int ordinals
        // stable on already-saved assets.
        Laser,
        ImagingInfrared,
        MultiSpectral,
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

        // Added in Phase 2B for drone propulsion spectrum breadth (Petrol/Diesel
        // internal combustion engines). Appended rather than inserted to keep
        // existing values' serialized int ordinals stable on already-saved assets.
        InternalCombustion,
    }

    public enum FuelType
    {
        Battery,
        JetFuel,
        SolidPropellant,
        LiquidPropellant,
        HybridPropellant,

        // Added in Phase 2B for drone ICE propulsion (Petrol/Diesel). Appended, not
        // inserted, for the same serialized-ordinal-stability reason as above.
        Petrol,
        Diesel,
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
