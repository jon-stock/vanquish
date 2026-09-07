using UnityEngine;

namespace Vanquish.Combat.Play
{
    /// <summary>
    /// Builds a simple procedural quadcopter mesh from Unity primitives — no
    /// imported art assets needed (matching the pre-pivot project's convention).
    /// Deliberately much simpler than that project's full DroneVisualBuilder (which
    /// assembled visuals from a rich part-composition data model this pivot doesn't
    /// have yet): a central body plus four arm+rotor assemblies is enough to make a
    /// flyable quadcopter read clearly in a test scene.
    /// </summary>
    public static class DroneVisualBuilder
    {
        /// <summary>
        /// Builds the visual under a new "Visual" child of <paramref name="parent"/>
        /// and returns that child transform — kept separate from the physics root so
        /// <see cref="QuadcopterTiltVisual"/> can bank it for readability without
        /// touching the actual flight physics rotation (FlightBody.orientToVelocity
        /// is false for multirotors).
        /// </summary>
        public static Transform Build(Transform parent, Color bodyColor, float bodyRadius = 0.35f, float armLength = 0.5f)
        {
            var visualRoot = new GameObject("Visual").transform;
            visualRoot.SetParent(parent, false);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Body";
            body.transform.SetParent(visualRoot, false);
            body.transform.localScale = Vector3.one * (bodyRadius * 2f);
            SetColor(body, bodyColor);
            RemoveCollider(body);

            Vector3[] armDirections =
            {
                new Vector3(1f, 0f, 1f).normalized, new Vector3(-1f, 0f, 1f).normalized,
                new Vector3(1f, 0f, -1f).normalized, new Vector3(-1f, 0f, -1f).normalized,
            };

            foreach (Vector3 direction in armDirections)
                BuildArmAndRotor(visualRoot, direction, armLength, bodyColor);

            return visualRoot;
        }

        private static void BuildArmAndRotor(Transform visualRoot, Vector3 direction, float armLength, Color bodyColor)
        {
            Vector3 armMidpoint = direction * (armLength * 0.5f);

            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "Arm";
            arm.transform.SetParent(visualRoot, false);
            arm.transform.localPosition = armMidpoint;
            arm.transform.localRotation = Quaternion.LookRotation(direction, Vector3.up);
            arm.transform.localScale = new Vector3(0.06f, 0.06f, armLength);
            SetColor(arm, bodyColor * 0.8f);
            RemoveCollider(arm);

            GameObject rotor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rotor.name = "Rotor";
            rotor.transform.SetParent(visualRoot, false);
            rotor.transform.localPosition = direction * armLength;
            rotor.transform.localScale = new Vector3(0.28f, 0.015f, 0.28f);
            SetColor(rotor, Color.black);
            RemoveCollider(rotor);
            rotor.AddComponent<RotorSpinner>();
        }

        private static void SetColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color;
        }

        private static void RemoveCollider(GameObject go)
        {
            // Purely visual pieces — the physics root already has its own collider
            // (see the scene builder); primitive-provided colliders here would just
            // add noise/extra collision surfaces to a purely cosmetic child mesh.
            var collider = go.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);
        }
    }
}
