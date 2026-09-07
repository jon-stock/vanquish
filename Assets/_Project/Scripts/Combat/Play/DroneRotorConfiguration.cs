namespace Vanquish.Combat.Play
{
    /// <summary>
    /// Which multirotor airframe class to build/fly. Phase 1 focuses on the
    /// quadcopter (PLAN.md's tier 0/1 focus), but the hexacopter is kept available —
    /// same visual builder, just a different rotor count — for whenever a heavier-
    /// lift/higher-tier design is needed (per direct request: "keep the hexacopter
    /// handy, in case we need this later").
    /// </summary>
    public enum DroneRotorConfiguration
    {
        Quadcopter,
        Hexacopter,
    }

    public static class DroneRotorConfigurationExtensions
    {
        public static int ToRotorCount(this DroneRotorConfiguration configuration) => configuration switch
        {
            DroneRotorConfiguration.Hexacopter => 6,
            _ => 4,
        };
    }
}
