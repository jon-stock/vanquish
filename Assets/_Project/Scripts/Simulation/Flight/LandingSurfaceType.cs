namespace Vanquish.Simulation.Flight
{
    /// <summary>
    /// Ground surface classifications from the Subsystem Design Deep Dive §6's Surface
    /// Friction Matrix. Drives max landing slope and rolling/static friction for
    /// LandingValidator's safe-landing check.
    /// </summary>
    public enum LandingSurfaceType
    {
        PavedRunwayOrHelipad,
        FlatGrassOrSoil,
        UnevenOrRock,
        WaterOrMarsh,
    }
}
