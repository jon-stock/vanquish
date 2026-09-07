using UnityEngine;

namespace Vanquish.Combat.Play
{
    /// <summary>
    /// Builds a procedural multirotor drone mesh from Unity primitives — no imported
    /// art assets. Styled after a real FPV/racing quad rather than a single painted
    /// box: a dark carbon-fiber-look frame plate, a raised flight-controller/battery
    /// stack, a battery pack slung underneath, a low forward FPV camera, and the
    /// team's <c>bodyColor</c> used only as small accents (stack trim, arm tips,
    /// mounted-missile fins) rather than one flat saturated hull color — real
    /// hardware reads as "mostly dark structure with a few colored ID marks," not as
    /// a solid-colored toy. N arms in an "X" configuration (rotor count driven by
    /// <see cref="DroneRotorConfiguration"/> — quadcopter or hexacopter), each with
    /// a motor bell and a twin-blade spinning prop, plus a row of hardpoint sockets
    /// underneath for mounted missile props (see <see cref="MountedMissileVisuals"/>).
    /// Deliberately simplified vs. the pre-pivot project's version (no sensor-pod/
    /// hull-material variation driven by a design's actual parts) since this pivot
    /// doesn't have that richer part-composition data model yet.
    /// </summary>
    public static class DroneVisualBuilder
    {
        private static readonly Color CarbonFrame = new Color(0.045f, 0.045f, 0.05f);
        private static readonly Color GunmetalMotor = new Color(0.1f, 0.1f, 0.11f);
        private static readonly Color PropPlastic = new Color(0.82f, 0.82f, 0.8f);
        private static readonly Color BatteryBlack = new Color(0.07f, 0.07f, 0.08f);
        private static readonly Color CameraGlass = new Color(0.03f, 0.04f, 0.05f);

        /// <summary>
        /// Builds the visual under a new "Visual" child of <paramref name="parent"/>
        /// (kept separate from the physics root so <see cref="QuadcopterTiltVisual"/>
        /// can bank it without touching the actual flight physics rotation) and
        /// returns both that child transform and the hardpoint sockets missiles get
        /// mounted to.
        /// </summary>
        public static Transform Build(
            Transform parent,
            Color accentColor,
            DroneRotorConfiguration rotorConfiguration,
            out Transform[] hardpoints,
            float armLength = 0.5f,
            int hardpointCount = 4)
        {
            var visualRoot = new GameObject("Visual").transform;
            visualRoot.SetParent(parent, false);

            BuildBody(visualRoot, accentColor);

            int rotorCount = Mathf.Max(3, rotorConfiguration.ToRotorCount());
            float angleStep = 360f / rotorCount;
            for (int i = 0; i < rotorCount; i++)
            {
                // Start at 45 degrees for an "X" configuration (arms between the
                // body's forward/back/left/right axes) — the common FPV/multirotor look.
                float angleDeg = 45f + i * angleStep;
                BuildArmAndRotor(visualRoot, angleDeg, armLength, accentColor);
            }

            hardpoints = CreateHardpointSockets(visualRoot, hardpointCount, halfSpanX: armLength * 0.5f, y: -0.26f, z: -0.05f);

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
            prop.transform.localScale = new Vector3(0.055f, 0.14f, 0.055f);
            RemoveCollider(prop);
            ApplyMaterial(prop, new Color(0.75f, 0.75f, 0.72f), metallic: 0.4f, smoothness: 0.5f);
            return prop;
        }

        private static void BuildBody(Transform visualRoot, Color accentColor)
        {
            // Base frame plate — the flat carbon chassis every FPV quad is built
            // around, wider/flatter than a single tall box so the silhouette reads
            // as "flat compact hardware" rather than a stubby toy cube.
            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "FramePlate";
            plate.transform.SetParent(visualRoot, false);
            plate.transform.localScale = new Vector3(0.5f, 0.05f, 0.72f);
            ApplyMaterial(plate, CarbonFrame, metallic: 0.3f, smoothness: 0.65f);
            RemoveCollider(plate);

            // Flight-controller/battery stack — a smaller raised block on top,
            // giving the layered look real quads have instead of one flat slab. The
            // team accent color lives here (a colored top stack reads as an ID mark,
            // not as "the whole drone is painted this color").
            GameObject stack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stack.name = "Stack";
            stack.transform.SetParent(visualRoot, false);
            stack.transform.localPosition = new Vector3(0f, 0.09f, -0.02f);
            stack.transform.localScale = new Vector3(0.24f, 0.1f, 0.34f);
            ApplyMaterial(stack, accentColor, metallic: 0.25f, smoothness: 0.5f);
            RemoveCollider(stack);

            // Battery pack slung underneath, protruding slightly below the frame —
            // one of the most recognizable "this is a real drone" details.
            GameObject battery = GameObject.CreatePrimitive(PrimitiveType.Cube);
            battery.name = "Battery";
            battery.transform.SetParent(visualRoot, false);
            battery.transform.localPosition = new Vector3(0f, -0.075f, -0.08f);
            battery.transform.localScale = new Vector3(0.18f, 0.09f, 0.4f);
            ApplyMaterial(battery, BatteryBlack, metallic: 0.1f, smoothness: 0.3f);
            RemoveCollider(battery);

            // Low forward FPV camera — small, low, and near the very front (not a
            // big dome sitting on top), matching a real FPV cam's placement and size.
            GameObject camera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            camera.name = "Camera";
            camera.transform.SetParent(visualRoot, false);
            camera.transform.localPosition = new Vector3(0f, -0.01f, 0.34f);
            camera.transform.localScale = new Vector3(0.1f, 0.1f, 0.12f);
            ApplyMaterial(camera, CameraGlass, metallic: 0.2f, smoothness: 0.9f);
            RemoveCollider(camera);

            BuildLandingLegs(visualRoot);
        }

        /// <summary>
        /// Four thin carbon-rod legs (front pair + rear pair) rather than two sticking
        /// out sideways into the arm silhouette — reads as proper landing gear tucked
        /// under the fuselage instead of stray sticks poking out of the sides.
        /// </summary>
        private static void BuildLandingLegs(Transform visualRoot)
        {
            foreach (int xSide in new[] { -1, 1 })
            {
                foreach (int zSide in new[] { -1, 1 })
                {
                    GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    leg.name = "Leg";
                    leg.transform.SetParent(visualRoot, false);
                    leg.transform.localPosition = new Vector3(xSide * 0.14f, -0.16f, zSide * 0.22f);
                    leg.transform.localRotation = Quaternion.Euler(zSide * 8f, 0f, xSide * 10f);
                    leg.transform.localScale = new Vector3(0.016f, 0.12f, 0.016f);
                    ApplyMaterial(leg, CarbonFrame, metallic: 0.3f, smoothness: 0.4f);
                    RemoveCollider(leg);
                }
            }
        }

        private static void BuildArmAndRotor(Transform visualRoot, float angleDeg, float armLength, Color accentColor)
        {
            Quaternion armRotation = Quaternion.Euler(0f, angleDeg, 0f);
            Vector3 tipPosition = armRotation * Vector3.forward * armLength;

            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "Arm";
            arm.transform.SetParent(visualRoot, false);
            arm.transform.localRotation = armRotation;
            arm.transform.localPosition = tipPosition * 0.5f;
            arm.transform.localScale = new Vector3(0.045f, 0.035f, armLength);
            ApplyMaterial(arm, CarbonFrame, metallic: 0.4f, smoothness: 0.5f);
            RemoveCollider(arm);

            // Small colored tip cap where the arm meets the motor — a real-drone
            // orientation/team-ID trick (colored arm tips or prop tips) that reads
            // clearly at a glance without painting the whole airframe one color.
            GameObject tipCap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tipCap.name = "ArmTip";
            tipCap.transform.SetParent(visualRoot, false);
            tipCap.transform.localPosition = tipPosition * 0.94f;
            tipCap.transform.localScale = Vector3.one * 0.055f;
            ApplyMaterial(tipCap, accentColor, metallic: 0.3f, smoothness: 0.6f);
            RemoveCollider(tipCap);

            GameObject motor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            motor.name = "Motor";
            motor.transform.SetParent(visualRoot, false);
            motor.transform.localPosition = tipPosition + Vector3.up * 0.035f;
            motor.transform.localScale = new Vector3(0.09f, 0.045f, 0.09f);
            ApplyMaterial(motor, GunmetalMotor, metallic: 0.75f, smoothness: 0.6f);
            RemoveCollider(motor);

            // Spin pivot is a sibling of the motor (not its child) so it doesn't
            // inherit the motor's non-uniform scale when positioning/sizing the blades.
            var spinPivot = new GameObject("RotorSpin");
            spinPivot.transform.SetParent(visualRoot, false);
            spinPivot.transform.localPosition = tipPosition + Vector3.up * 0.07f;
            spinPivot.AddComponent<RotorSpinner>();

            // Two crossed blades (not one) so the spinning prop reads as a disc
            // silhouette rather than a single flat bar.
            BuildPropBlade(spinPivot.transform, armLength, yRotation: 0f);
            BuildPropBlade(spinPivot.transform, armLength, yRotation: 90f);
        }

        private static void BuildPropBlade(Transform spinPivot, float armLength, float yRotation)
        {
            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Blades";
            blade.transform.SetParent(spinPivot, false);
            blade.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            blade.transform.localScale = new Vector3(armLength * 0.5f, 0.008f, 0.035f);
            ApplyMaterial(blade, PropPlastic, metallic: 0.05f, smoothness: 0.35f);
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
