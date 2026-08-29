using UnityEngine;
using Vanquish.Core;
using Vanquish.Data;
using Vanquish.Data.Drones;
using Vanquish.Data.Missiles;
using Vanquish.Simulation.Flight;
using Vanquish.Simulation.Guidance;
using Vanquish.Simulation.Sensors;

namespace Vanquish.Combat
{
    /// <summary>
    /// Builds real GameObjects (physics root + visual child, matching the Phase 0
    /// pattern) from a MissileLoadout/DroneLoadout's computed DesignStatsCalculator
    /// output. Used identically by the Workshop's test range and real Combat scenes,
    /// so a design behaves exactly the same in both places.
    /// </summary>
    public static class VehicleFactory
    {
        public static GameObject SpawnMissile(MissileLoadout loadout, Vector3 position, Quaternion rotation,
            Transform target, Team team)
        {
            MissileRuntimeStats stats = DesignStatsCalculator.Calculate(loadout);

            var missile = new GameObject($"Missile_{loadout.designName}");
            missile.transform.SetPositionAndRotation(position, rotation);

            var collider = missile.AddComponent<CapsuleCollider>();
            collider.direction = 2; // Z-axis, matches forward-facing physics root
            collider.radius = 0.3f;
            collider.height = 1.8f;

            var rb = missile.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.useGravity = false;

            var flightBody = missile.AddComponent<FlightBody>();
            flightBody.Configure(stats.massKg, stats.thrustNewtons, stats.dragCoefficient, stats.maxGForce);
            flightBody.isThrusting = true;

            if (target != null)
            {
                var guidance = missile.AddComponent<GuidanceController>();
                guidance.SetTarget(target);
            }

            var signature = missile.AddComponent<DetectableSignature>();
            signature.radarCrossSection = stats.radarCrossSection;
            signature.infraredSignature = stats.infraredSignature;
            signature.team = team;

            var impact = missile.AddComponent<MissileImpact>();
            impact.directDamage = stats.directDamage;
            impact.splashDamage = stats.splashDamage;
            impact.blastRadiusMeters = stats.blastRadiusMeters;

            var sensor = missile.AddComponent<DetectionSensor>();
            sensor.baseRangeMeters = stats.seekerRangeMeters;
            sensor.ownerTeam = team;

            if (loadout.payload.requiresProximityFuse && stats.blastRadiusMeters > 0f)
            {
                var fuseGo = new GameObject("ProximityFuse");
                fuseGo.transform.SetParent(missile.transform, worldPositionStays: false);
                fuseGo.transform.localPosition = Vector3.zero;
                var fuseCollider = fuseGo.AddComponent<SphereCollider>();
                fuseCollider.isTrigger = true;
                fuseCollider.radius = Mathf.Max(3f, stats.blastRadiusMeters);
                var fuseRelay = fuseGo.AddComponent<ProximityFuseRelay>();
                fuseRelay.owner = impact;
            }

            BuildVisualCapsule(missile.transform, new Vector3(0.4f, 0.9f, 0.4f));

            return missile;
        }

        public static GameObject SpawnDrone(DroneLoadout loadout, Vector3 position, Quaternion rotation, Team team)
        {
            DroneRuntimeStats stats = DesignStatsCalculator.Calculate(loadout);

            var drone = new GameObject($"Drone_{loadout.designName}_{team}");
            drone.transform.SetPositionAndRotation(position, rotation);

            var collider = drone.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.5f, 0.8f, 2f);

            var rb = drone.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.useGravity = false;

            var flightBody = drone.AddComponent<FlightBody>();
            flightBody.Configure(stats.massKg, stats.thrustNewtons, stats.dragCoefficient, stats.maxGForce);

            // Phase 1 Tier-0 drones are electric quadcopters (propeller + electric
            // propulsion), not fixed-wing/jet aircraft: they're omnidirectional and
            // shouldn't have a constant forward thrust or auto-orient to face their
            // direction of travel — all movement comes from vectored steering force
            // instead (AI guidance or player WASD input), same as a real multirotor's
            // vectored thrust. Revisit once jet/fixed-wing propulsion tiers exist.
            flightBody.isThrusting = false;
            flightBody.orientToVelocity = false;

            var signature = drone.AddComponent<DetectableSignature>();
            signature.radarCrossSection = stats.radarCrossSection;
            signature.infraredSignature = stats.infraredSignature;
            signature.team = team;

            var sensor = drone.AddComponent<DetectionSensor>();
            sensor.baseRangeMeters = stats.sensorRangeMeters;
            sensor.ownerTeam = team;

            var health = drone.AddComponent<Health>();
            health.SetMaxHealth(stats.maxHealth);

            drone.AddComponent<CrashDamage>();

            if (loadout.missileLoadout != null && loadout.missileLoadout.IsComplete)
            {
                var weapon = drone.AddComponent<WeaponController>();
                weapon.missileLoadout = loadout.missileLoadout;
                weapon.ammoRemaining = loadout.ammoCount;
                weapon.ownerTeam = team;
            }

            // Procedural multirotor visual (body + arms + spinning rotors) — see
            // DroneVisualBuilder. rotorCount is hardcoded to 4 for now, matching the
            // Tier-0 SmallQuad airframe; will read DroneAirframeDefinition.rotorCount
            // once the quadcopter->hexacopter upgrade path (Phase 2B) exists.
            Transform visual = DroneVisualBuilder.BuildMultirotorVisual(drone.transform, rotorCount: 4);

            var tilt = drone.AddComponent<QuadcopterTiltVisual>();
            tilt.body = rb;
            tilt.visualRoot = visual;

            if (CombatManager.Instance != null)
                CombatManager.Instance.RegisterUnit(drone, team);

            return drone;
        }

        private static void BuildVisualCapsule(Transform parent, Vector3 scale)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            DestroyComponent(visual.GetComponent<Collider>());
            visual.transform.SetParent(parent, worldPositionStays: false);
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            visual.transform.localScale = scale;
        }

        /// <summary>
        /// Safely destroys a component whether this factory is called at runtime
        /// (Combat/Workshop Play mode) or from an Editor tool building a scene
        /// (Object.Destroy is invalid outside Play mode and only warns/no-ops there).
        /// </summary>
        private static void DestroyComponent(Object obj)
        {
            if (obj == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(obj);
            else
                Object.DestroyImmediate(obj);
        }
    }
}
