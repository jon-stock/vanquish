using UnityEngine;
using Vanquish.Simulation.Flight;
using Vanquish.Simulation.Guidance;

namespace Vanquish.Combat.Play
{
    /// <summary>
    /// Builds a simple procedural missile GameObject — physics root with a
    /// Z-oriented CapsuleCollider (matching FlightBody's forward-thrust convention)
    /// and a purely-cosmetic capsule visual child, wired up with FlightBody +
    /// GuidanceController (pursuit) + MissileBurnController + MissileImpact. No
    /// imported art assets, no rich part-composition data model — just enough to
    /// make "fire a missile that actually flies and hits something" real in a scene.
    /// </summary>
    public static class MissileFactory
    {
        public static GameObject SpawnMissile(Vector3 position, Quaternion rotation, Transform target, float rawDamage, float payloadSize)
        {
            var go = new GameObject("Missile");
            go.transform.SetPositionAndRotation(position, rotation);

            var collider = go.AddComponent<CapsuleCollider>();
            collider.direction = 2; // Z axis, matching FlightBody's transform.forward thrust convention
            collider.radius = 0.08f;
            collider.height = 0.6f;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            visual.transform.localScale = new Vector3(0.16f, 0.35f, 0.16f);
            var visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
                Object.Destroy(visualCollider);
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = Color.yellow;

            var rigidbody = go.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;

            var flightBody = go.AddComponent<FlightBody>();
            flightBody.Configure(mass: 12f, thrust: 900f, drag: 0.02f, maxG: 25f, gravity: false, orientToVel: true);
            flightBody.isThrusting = true;

            var guidance = go.AddComponent<GuidanceController>();
            guidance.SetTarget(target);

            var burn = go.AddComponent<MissileBurnController>();
            burn.flightBody = flightBody;
            burn.burnTimeSeconds = 4f;

            var impact = go.AddComponent<MissileImpact>();
            impact.rawDamage = rawDamage;
            impact.payloadSize = payloadSize;

            return go;
        }
    }
}
