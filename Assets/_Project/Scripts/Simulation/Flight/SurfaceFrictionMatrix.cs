namespace Vanquish.Simulation.Flight
{
    /// <summary>Per-surface landing tolerances, verbatim from the Subsystem Design Deep Dive §6
    /// Surface Friction Matrix table.</summary>
    public struct SurfaceFrictionProfile
    {
        public float maxLandingSlopeDegrees;
        public float rollingFriction;
        public float staticFriction;

        /// <summary>False for Water/Marsh — Deep Dive §6 lists its risk as "Destruction/Vehicle
        /// Sink", i.e. never a valid landing surface regardless of speed/slope.</summary>
        public bool isLandable;
    }

    /// <summary>Static lookup for the Deep Dive §6 Surface Friction Matrix table.</summary>
    public static class SurfaceFrictionMatrix
    {
        public static SurfaceFrictionProfile Get(LandingSurfaceType surface)
        {
            switch (surface)
            {
                case LandingSurfaceType.PavedRunwayOrHelipad:
                    return new SurfaceFrictionProfile
                    {
                        maxLandingSlopeDegrees = 15f, rollingFriction = 0.02f, staticFriction = 0.80f, isLandable = true,
                    };
                case LandingSurfaceType.FlatGrassOrSoil:
                    return new SurfaceFrictionProfile
                    {
                        maxLandingSlopeDegrees = 10f, rollingFriction = 0.08f, staticFriction = 0.65f, isLandable = true,
                    };
                case LandingSurfaceType.UnevenOrRock:
                    return new SurfaceFrictionProfile
                    {
                        maxLandingSlopeDegrees = 5f, rollingFriction = 0.25f, staticFriction = 0.50f, isLandable = true,
                    };
                case LandingSurfaceType.WaterOrMarsh:
                    return new SurfaceFrictionProfile
                    {
                        maxLandingSlopeDegrees = 0f, rollingFriction = 0f, staticFriction = 0f, isLandable = false,
                    };
                default:
                    return default;
            }
        }
    }
}
