using UnityEngine;
using Vanquish.Core;
using Vanquish.Data.Support;
using Vanquish.Simulation.Sensors;

namespace Vanquish.Combat
{
    /// <summary>
    /// Phase 2D: spawner for static/semi-static support installations (starting with
    /// base defense/SAM sites), kept as a sibling class to VehicleFactory rather than
    /// another VehicleFactory method — per this item's own instruction ("needs its own
    /// spawner path, not VehicleFactory.SpawnDrone, since it's not a drone") and
    /// PLAN.md's independently-arrived-at 2F design intent ("placed installations need
    /// their own spawner, parallel to VehicleFactory, since they're static/semi-static,
    /// not flying units"). Deliberately skips everything VehicleFactory.SpawnDrone does
    /// that's flight-specific (Rigidbody, FlightBody, orientToVelocity, CrashDamage) —
    /// a static site never moves, so none of that applies. Shares WeaponController,
    /// DetectableSignature, DetectionSensor, and Health verbatim with drones/missiles,
    /// since none of those are actually drone-coupled (confirmed before writing this:
    /// WeaponController only needs a MissileLoadout + transform + optional Collider).
    /// </summary>
    public static class InstallationFactory
    {
        public static GameObject SpawnBaseDefense(BaseDefenseDefinition definition, Vector3 position, Quaternion rotation, Team team)
        {
            var installation = new GameObject($"BaseDefense_{definition.displayName}_{team}");
            installation.transform.SetPositionAndRotation(position, rotation);

            // Static collider, no Rigidbody — Unity's physics still generates
            // OnCollisionEnter against it as long as the *other* body (an incoming
            // missile) has one, which every missile spawned by VehicleFactory does.
            var collider = installation.AddComponent<BoxCollider>();
            collider.size = new Vector3(2.5f, 2.5f, 2.5f);

            var signature = installation.AddComponent<DetectableSignature>();
            signature.radarCrossSection = 4f; // bigger, more exposed than any drone — a static target
            signature.infraredSignature = 1.5f;
            signature.team = team;
            signature.isArmed = definition.missileLoadout != null && definition.missileLoadout.IsComplete;

            var sensor = installation.AddComponent<DetectionSensor>();
            // A SAM's own detection reach should be at least its engagement range —
            // otherwise it could theoretically be in range to fire at something its
            // own sensor could never actually detect/track first.
            sensor.baseRangeMeters = Mathf.Max(definition.engagementRangeMeters, 1000f);
            sensor.ownerTeam = team;

            var health = installation.AddComponent<Health>();
            health.SetMaxHealth(Mathf.Max(1f, definition.health));

            if (definition.missileLoadout != null && definition.missileLoadout.IsComplete)
            {
                var weapon = installation.AddComponent<WeaponController>();
                weapon.missileLoadout = definition.missileLoadout;
                weapon.ammoRemaining = Mathf.Max(0, definition.ammoCount);
                weapon.fireCooldownSeconds = definition.rateOfFirePerSecond > 0f
                    ? 1f / definition.rateOfFirePerSecond
                    : 2.5f;
                weapon.ownerTeam = team;
            }

            BuildVisual(installation.transform);
            TeamColorUtility.ApplyTeamColor(installation.transform, team);

            if (CombatManager.Instance != null)
                CombatManager.Instance.RegisterUnit(installation, team);

            return installation;
        }

        /// <summary>Cheap prototype silhouette (base + radar dish) from primitives — same
        /// "no imported models yet" convention as DroneVisualBuilder.</summary>
        private static void BuildVisual(Transform parent)
        {
            var root = new GameObject("Visual");
            root.transform.SetParent(parent, worldPositionStays: false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            GameObject baseBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseBox.name = "Base";
            DestroyCollider(baseBox);
            baseBox.transform.SetParent(root.transform, worldPositionStays: false);
            baseBox.transform.localPosition = Vector3.zero;
            baseBox.transform.localScale = new Vector3(2f, 1f, 2f);

            GameObject dish = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dish.name = "Dish";
            DestroyCollider(dish);
            dish.transform.SetParent(root.transform, worldPositionStays: false);
            dish.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            dish.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            dish.transform.localScale = new Vector3(0.12f, 0.9f, 0.12f);
        }

        private static void DestroyCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(collider);
            else
                Object.DestroyImmediate(collider);
        }
    }
}
