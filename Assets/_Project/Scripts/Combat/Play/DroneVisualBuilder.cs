using UnityEngine;

namespace Vanquish.Combat.Play
{
    /// <summary>
    /// Builds a procedural multirotor drone mesh from Unity primitives — no imported
    /// art assets. Adapted from the pre-pivot project's DroneVisualBuilder
    /// (BuildMultirotorVisual): a central body, N arms in an "X" configuration
    /// (rotor count driven by <see cref="DroneRotorConfiguration"/> — quadcopter or
    /// hexacopter), each arm ending in a spinning rotor, plus a row of hardpoint
    /// sockets underneath for mounted missile props (see
    /// <see cref="MountedMissileVisuals"/>). Deliberately simplified vs. that
    /// project's version (no sensor-pod/hull-material/rotor-material variation) since
    /// this pivot doesn't have that richer part-composition data model yet — the
    /// silhouette and rotor/hardpoint structure are what was asked to look "more like
    /// the old one," and that's what this reproduces.
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
                BuildArmAndRotor(visualRoot, angleDeg, armLength, bodyColor);
            }

            hardpoints = CreateHardpointSockets(visualRoot, hardpointCount, halfSpanX: armLength * 0.5f, y: -0.22f, z: 0f);

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
            SetColor(prop, Color.yellow);
            return prop;
        }

        private static void BuildBody(Transform visualRoot, Color bodyColor)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(visualRoot, false);
            body.transform.localScale = new Vector3(0.6f, 0.25f, 0.6f);
            SetColor(body, bodyColor);
            RemoveCollider(body);
        }

        private static void BuildArmAndRotor(Transform visualRoot, float angleDeg, float armLength, Color bodyColor)
        {
            Quaternion armRotation = Quaternion.Euler(0f, angleDeg, 0f);
            Vector3 tipPosition = armRotation * Vector3.forward * armLength;

            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "Arm";
            arm.transform.SetParent(visualRoot, false);
            arm.transform.localRotation = armRotation;
            arm.transform.localPosition = tipPosition * 0.5f;
            arm.transform.localScale = new Vector3(0.06f, 0.05f, armLength);
            SetColor(arm, bodyColor * 0.8f);
            RemoveCollider(arm);

            GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hub.name = "RotorHub";
            hub.transform.SetParent(visualRoot, false);
            hub.transform.localPosition = tipPosition + Vector3.up * 0.05f;
            hub.transform.localScale = new Vector3(0.12f, 0.03f, 0.12f);
            SetColor(hub, Color.black);
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
            SetColor(blade, Color.black);
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

        private static void SetColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color;
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
