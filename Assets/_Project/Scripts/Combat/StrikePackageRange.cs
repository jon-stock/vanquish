using System.Collections.Generic;

namespace Vanquish.Combat
{
    /// <summary>
    /// PLAN.md's "bottlenecked by the least capable committed unit" rule: a
    /// strike/defense package's usable engagement radius is the MINIMUM
    /// range/endurance across all committed units — a small drone can't fly far, so
    /// if it's in the package, the package can't reach further than it can. Pure,
    /// engine-agnostic, and reused unchanged at theatre scale for army relocation
    /// speed in Phase 2 (same rule, different unit).
    /// </summary>
    public static class StrikePackageRange
    {
        /// <summary>
        /// Returns the usable radius for a package given each committed unit's own
        /// operational range/endurance. Returns 0 for an empty package (nothing to
        /// commit means nothing can reach anywhere).
        /// </summary>
        public static float ComputeUsableRadiusMeters(IEnumerable<float> committedUnitRangesMeters)
        {
            bool any = false;
            float min = float.PositiveInfinity;

            foreach (float range in committedUnitRangesMeters)
            {
                any = true;
                if (range < min)
                    min = range;
            }

            return any ? min : 0f;
        }
    }
}
