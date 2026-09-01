using UnityEngine;

namespace Vanquish.Simulation.Flight
{
    /// <summary>
    /// Samples terrain/ground height directly below a world position via a downward
    /// raycast against physics colliders. Falls back to a flat y=0 placeholder ground
    /// (matching every existing scene builder's ground plane — see
    /// Phase1CombatSceneBuilder.BuildGround/Phase0TestSceneBuilder.BuildGround, both at
    /// position Vector3.zero) when no collider is hit, per PLAN.md Phase 2B's technical
    /// note to "build against a flat placeholder ground first and revisit" since Phase
    /// 2E's real heightmap terrain doesn't exist yet. Because this samples via a real
    /// raycast rather than hardcoding 0, it will pick up actual terrain colliders
    /// unmodified once Phase 2E adds them.
    /// </summary>
    public static class GroundSampler
    {
        public const float FlatPlaceholderGroundY = 0f;

        /// <summary>How far above the query position the raycast starts, so a unit that has
        /// already sunk slightly below its target altitude still finds the ground below it.</summary>
        public const float RaycastStartHeightOffset = 2f;

        public static float RaycastMaxDistance = 5000f;

        /// <summary>All layers by default — set this to a dedicated "Ground/Terrain" layer
        /// once one exists, so this doesn't accidentally hit other units' colliders.</summary>
        public static LayerMask GroundLayerMask = ~0;

        public static float SampleGroundHeight(Vector3 worldPosition)
        {
            Vector3 origin = worldPosition + Vector3.up * RaycastStartHeightOffset;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, RaycastMaxDistance, GroundLayerMask,
                    QueryTriggerInteraction.Ignore))
            {
                return hit.point.y;
            }
            return FlatPlaceholderGroundY;
        }
    }
}
