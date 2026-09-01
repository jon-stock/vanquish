using UnityEngine;

namespace Vanquish.Simulation.Flight
{
    /// <summary>
    /// Result of a forward-projected terrain collision probe, per the Subsystem Design
    /// Deep Dive §5's example struct. Used to decide whether AbsoluteMSL altitude
    /// holding (which ignores terrain — see AltitudeMode) is about to fly a unit into
    /// a ridgeline/cliff it can't climb over in time.
    /// </summary>
    public struct TerrainCollisionCheck
    {
        public bool WillCollide;
        public float DistanceToImpact;
        public Vector3 SurfaceNormal;

        /// <summary>True if the hit surface is steep enough to count as a vertical cliff
        /// rather than a climbable slope, per Deep Dive §5's cliffAngleThreshold example.</summary>
        public bool IsVerticalCliff(float cliffAngleThresholdDegrees = 60f)
        {
            return WillCollide && Vector3.Angle(SurfaceNormal, Vector3.up) >= cliffAngleThresholdDegrees;
        }
    }

    /// <summary>
    /// Forward-projects a raycast from a unit's position along its travel direction to
    /// build a TerrainCollisionCheck. Physics-dependent (works against whatever ground/
    /// terrain colliders exist in the scene — the flat placeholder ground plane today,
    /// real heightmap terrain once Phase 2E adds it, same "no hardcoded assumption"
    /// approach as GroundSampler).
    /// </summary>
    public static class TerrainCollisionChecker
    {
        public static TerrainCollisionCheck CheckAhead(Vector3 position, Vector3 forwardDirection, float maxDistance,
            LayerMask groundLayerMask)
        {
            if (Physics.Raycast(position, forwardDirection, out RaycastHit hit, maxDistance, groundLayerMask,
                    QueryTriggerInteraction.Ignore))
            {
                return new TerrainCollisionCheck
                {
                    WillCollide = true,
                    DistanceToImpact = hit.distance,
                    SurfaceNormal = hit.normal,
                };
            }

            return new TerrainCollisionCheck
            {
                WillCollide = false,
                DistanceToImpact = maxDistance,
                SurfaceNormal = Vector3.up,
            };
        }

        /// <summary>
        /// Required climb rate to clear a detected obstacle in time, per Deep Dive §5's
        /// formula: v_forward * height_delta / detection_distance. Compare against the
        /// airframe's actual max climb rate to decide if a collision is imminent. Pure
        /// function, headlessly testable.
        /// </summary>
        public static float RequiredClimbRate(float forwardSpeed, float heightDelta, float detectionDistance)
        {
            if (detectionDistance <= 0.001f)
                return float.PositiveInfinity;
            return forwardSpeed * heightDelta / detectionDistance;
        }
    }
}
