using UnityEngine;

namespace Vanquish.Combat.Play
{
    /// <summary>
    /// Builds a procedural multirotor drone mesh from Unity primitives — no imported
    /// art assets. Adapted from the pre-pivot project's DroneVisualBuilder
    /// (BuildMultirotorVisual): a central body with a nose canopy/sensor-pod bump and
    /// landing legs, N arms in an "X" configuration (rotor count driven by
    /// <see cref="DroneRotorConfiguration"/> — quadcopter or hexacopter), each arm
    /// ending in a spinning rotor, plus a row of hardpoint sockets underneath for
    /// mounted missile props (see <see cref="MountedMissileVisuals"/>). Deliberately
    /// simplified vs. that project's version (no sensor-pod/hull-material/rotor-
    /// material variation driven by a design's parts) since this pivot doesn't have
    /// that richer part-composition data model yet — the silhouette, proportions,
    /// and material finish are what matter for reading as "a real drone" rather than
    /// a plain colored box, and that's what this reproduces.
    /// </summary>
    public static class DroneVisualBuilder
    {
        /// <summary>
        /// Builds the visual under a new "Visual" child of <paramref name="parent"/>
        /// (kept separate from the physics root so <see cref="QuadcopterTiltVisual"/>
        /// can bank it without touching the actual flight physics rotation) and
        /// returns both that child transform and the hardpoint sockets missiles get
        /// mounted to.
        /// </summary>
        public static Transform Build(
            Transform parent,
            Color bodyColor,
            DroneRotorConfiguration rotorConfiguration,
            out Transform[] hardpoints,
            float armLength = 0.5f,
            int hardpointCount = 4)
        {
            var visualRoot = new GameObject("Visual").transform;
            visualRoot.SetParent(parent, false);

            BuildBody(visualRoot, bodyColor);

            int rotorCount = Mathf.Max(3, rotorConfiguration.ToRotorCount());
            float angleStep = 360f / rotorCount;
            for (int i = 0; i < rotorCount; i++)
            {
                // Start at 45 degrees for an "X" configuration (arms between the
                // body's forward/back/left/right axes) — the common FPV/multirotor look.
                float angleDeg = 45f + i * angleStep;
                BuildArmAndRotor(visualRoot, angleDeg, armLength);
            }

            hardpoints = CreateHardpointSockets(visualRoot, hardpointCount, halfSpanX: armLength * 0.5f, y: -0.24f, z: 0f);

            return visualRoot;
        }

        /// <summary>
        /// Builds one small missile-shaped prop mounted at a hardpoint socket — a
        /// visible "this is armed" tell that disappears per shot fired (see
        /// <see cref="MountedMissileVisuals"/>), not a physically-simulated munition
        /// (that's <see cref="MissileFactory"/>, spawned separately on actual fire).
        /// </summary>
        public static GameObject BuildMountedMissileProp(Transform hardpoint)
        {
            GameObject prop = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            prop.name = "MountedMissile";
            prop.transform.SetParent(hardpoint, false);
            prop.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            prop.transform.localScale = new Vector3(0.06f, 0.14f, 0.06f);
            RemoveCollider(prop);
            ApplyMaterial(prop, new Color(0.85f, 0.7f, 0.1f), metallic: 0.5f, smoothness: 0.6f);
            return prop;
        }

        private static void BuildBody(Transform visualRoot, Color bodyColor)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(visualRoot, false);
            body.transform.localScale = new Vector3(0.42f, 0.2f, 0.65f);
            ApplyMaterial(body, bodyColor, metallic: 0.3f, smoothness: 0.55f);
            RemoveCollider(body);

            // Nose canopy/sensor pod — a dark glassy bump toward the front, breaking
            // up the plain-box silhouette and reading as a camera/sensor turret
            // (matching the pre-pivot project's nose-pod convention, simplified to a
            // single fixed look rather than one driven by an equipped sensor suite).
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "Canopy";
            canopy.transform.SetParent(visualRoot, false);
            canopy.transform.localPosition = new Vector3(0f, 0.08f, 0.24f);
            canopy.transform.localScale = new Vector3(0.2f, 0.16f, 0.22f);
            ApplyMaterial(canopy, new Color(0.04f, 0.05f, 0.06f), metallic: 0.2f, smoothness: 0.85f);
            RemoveCollider(canopy);

            BuildLandingLegs(visualRoot);
        }

        /// <summary>A pair of simple angled landing skids — cheap, but immediately reads as "multirotor drone" rather than "floating box."</summary>
        private static void BuildLandingLegs(Transform visualRoot)
        {
            Color legColor = new Color(0.12f, 0.12f, 0.13f);
            foreach (int side in new[] { -1, 1 })
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = "Leg";
                leg.transform.SetParent(visualRoot, false);
                leg.transform.localPosition = new Vector3(side * 0.2f, -0.18f, 0f);
                leg.transform.localRotation = Quaternion.Euler(0f, 0f, side * 18f);
                leg.transform.localScale = new Vector3(0.025f, 0.14f, 0.025f);
                ApplyMaterial(leg, legColor, metallic: 0.3f, smoothness: 0.3f);
                RemoveCollider(leg);
            }
        }

        private static void BuildArmAndRotor(Transform visualRoot, float angleDeg, float armLength)
        {
            Quaternion armRotation = Quaternion.Euler(0f, angleDeg, 0f);
            Vector3 tipPosition = armRotation * Vector3.forward * armLength;
            Color armColor = new Color(0.16f, 0.16f, 0.18f);

            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "Arm";
            arm.transform.SetParent(visualRoot, false);
            arm.transform.localRotation = armRotation;
            arm.transform.localPosition = tipPosition * 0.5f;
            arm.transform.localScale = new Vector3(0.06f, 0.05f, armLength);
            ApplyMaterial(arm, armColor, metallic: 0.4f, smoothness: 0.4f);
            RemoveCollider(arm);

            GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hub.name = "RotorHub";
            hub.transform.SetParent(visualRoot, false);
            hub.transform.localPosition = tipPosition + Vector3.up * 0.05f;
            hub.transform.localScale = new Vector3(0.12f, 0.03f, 0.12f);
            ApplyMaterial(hub, new Color(0.06f, 0.06f, 0.07f), metallic: 0.5f, smoothness: 0.5f);
            RemoveCollider(hub);

            // Spin pivot is a sibling of the hub (not its child) so it doesn't
            // inherit the hub's non-uniform scale when positioning/sizing the blade.
            var spinPivot = new GameObject("RotorSpin");
            spinPivot.transform.SetParent(visualRoot, false);
            spinPivot.transform.localPosition = tipPosition + Vector3.up * 0.08f;
            spinPivot.AddComponent<RotorSpinner>();

            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Blades";
            blade.transform.SetParent(spinPivot.transform, false);
            blade.transform.localScale = new Vector3(armLength * 0.55f, 0.01f, 0.05f);
            // Light plastic finish (matching the pre-pivot project's default "Plastic"
            // rotor material) — contrasts against the dark hub/arms so spinning blades
            // read clearly rather than blending into a uniformly dark silhouette.
            ApplyMaterial(blade, new Color(0.85f, 0.85f, 0.82f), metallic: 0.05f, smoothness: 0.3f);
            RemoveCollider(blade);
        }

        /// <summary>Evenly-spaced hardpoint sockets along the local X axis, underneath the body.</summary>
        private static Transform[] CreateHardpointSockets(Transform visualRoot, int count, float halfSpanX, float y, float z)
        {
            if (count <= 0)
                return System.Array.Empty<Transform>();

            var sockets = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                float x = Mathf.Lerp(-halfSpanX, halfSpanX, t);

                var socket = new GameObject($"Hardpoint_{i}");
                socket.transform.SetParent(visualRoot, false);
                socket.transform.localPosition = new Vector3(x, y, z);
                sockets[i] = socket.transform;
            }
            return sockets;
        }

        /// <summary>
        /// Applies color plus basic metallic/smoothness PBR properties, defensively
        /// checking property names since the active render pipeline (URP Lit vs.
        /// built-in Standard) uses different names for the same concept.
        /// </summary>
        private static void ApplyMaterial(GameObject go, Color color, float metallic, float smoothness)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return;

            Material material = renderer.material; // instance, safe to mutate
            material.color = color;
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            else if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", smoothness);
        }

        private static void RemoveCollider(GameObject go)
        {
            // Purely visual pieces — the physics root already has its own collider;
            // primitive-provided colliders here would just add noise/extra collision
            // surfaces to a purely cosmetic child mesh.
            var collider = go.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);
        }
    }
}
