namespace Vanquish.Data
{
    /// <summary>
    /// PLAN.md Command &amp; Control Model — "the guidance-type taxonomy already in
    /// SeekerDefinition.cs directly determines manual-control eligibility — no
    /// separate 'can this be piloted' flag is needed, it falls out of SeekerType."
    /// This is that single source of truth: autonomous seekers fly themselves once
    /// launched; command-guided/datalink can be manually flown by the player exactly
    /// like a drone; laser-designated requires an active designator rather than
    /// flying the weapon at all.
    /// </summary>
    public static class SeekerControlModel
    {
        /// <summary>
        /// True if a player can take direct third-person manual flight control of a
        /// munition using this seeker type (PLAN.md Phase 1 direct-control layer).
        /// Autonomous seekers (Optical/Infrared/SemiActiveRadar/ActiveRadar/
        /// AntiRadiation) and None all return false — once launched they fly
        /// themselves. LaserDesignated also returns false: the player's involvement
        /// there is holding a designator on the target, not flying the weapon.
        /// </summary>
        public static bool IsManuallyFlyable(SeekerType seekerType) =>
            seekerType == SeekerType.WireOrDatalinkGuided;

        /// <summary>
        /// True if this seeker type requires an active designator (player, ally, or
        /// scout) painting the target for terminal guidance to work at all.
        /// </summary>
        public static bool RequiresActiveDesignation(SeekerType seekerType) =>
            seekerType == SeekerType.SemiActiveRadar || seekerType == SeekerType.LaserDesignated;
    }
}
