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
    /// The optional <c>bladeColor</c>/<c>bladeSizeMultiplier</c>/<c>batteryColor</c>/
    /// <c>batterySizeMultiplier</c> parameters on <see cref="Build"/> let a Lab
    /// design's chosen Propeller/Battery parts (see <see cref="Vanquish.Theatre.Play.PlanPartCatalog"/>)
    /// visibly change the rotor blades and battery pack, not just its stats — real
    /// combat-instance drones (no Plan) simply omit them and get the defaults.
    /// </summary>
    public static class DroneVisualBuilder
    {
        private static readonly Color CarbonFrame = new Color(0.045f, 0.045f, 0.05f);
        private static readonly Color GunmetalMotor = new Color(0.1f, 0.1f, 0.11f);
        private static readonly Color PropPlastic = new Color(0.82f, 0.82f, 0.8f);
        private static readonly Color BatteryBlack = new Color(0.07f, 0.07f, 0.08f);
        private static readonly Color CameraGlass = new Color(0.03f, 0.04f, 0.05f);

        /// <summary>
        /// Resources-relative path (no extension) to the imported quadcopter model
        /// — see <see cref="Vanquish.EditorTools.QuadcopterWireframeExporter"/> for
        /// how it's (re-)generated from this same procedural geometry, exported as
        /// a Wavefront OBJ, and placed here so <see cref="Resources.Load"/> can find
        /// it at runtime (works in both the Editor and real builds). Replace the
        /// asset at <c>Assets/_Project/Art/Resources/Models/Quadcopter.obj</c> (and
        /// its .mtl) directly once real textured art exists — no code changes
        /// needed here.
        /// </summary>
        private const string QuadcopterModelResourcePath = "Models/Quadcopter";

        // Must match the armLength Quadcopter.obj was exported with (the Build
        // default below) so TryBuildImportedQuadcopterBody can rescale the fixed
        // imported mesh proportionally for callers that pass a different armLength
        // (e.g. the theatre map's smaller army markers).
        private const float ExportedQuadcopterArmLength = 0.5f;

        /// <summary>
        /// Builds the visual under a new "Visual" child of <paramref name="parent"/>
        /// (kept separate from the physics root so <see cref="QuadcopterTiltVisual"/>
        /// can bank it without touching the actual flight physics rotation) and
        /// returns both that child transform and the hardpoint sockets missiles get
        /// mounted to.
        /// </summary>
        /// <param name="preferImportedAsset">
        /// If true (the default) and <paramref name="rotorConfiguration"/> is
        /// Quadcopter, uses the imported static airframe at
        /// <see cref="QuadcopterModelResourcePath"/> when present instead of the
        /// procedural body/arm primitives (motors/rotor blades are always
        /// procedural regardless — see <see cref="BuildStaticAirframeForExport"/>).
        /// Pass false to force the fully-procedural body too. Hexacopters always
        /// use the procedural body — no imported hexacopter asset exists (yet).
        /// </param>
        public static Transform Build(
            Transform parent,
            Color accentColor,
            DroneRotorConfiguration rotorConfiguration,
            out Transform[] hardpoints,
            float armLength = 0.5f,
            int hardpointCount = 4,
            bool preferImportedAsset = true,
            Color? bladeColor = null,
            float bladeSizeMultiplier = 1f,
            Color? batteryColor = null,
            float batterySizeMultiplier = 1f)
        {
            var visualRoot = new GameObject("Visual").transform;
            visualRoot.SetParent(parent, false);

            bool builtFromImportedAsset = preferImportedAsset &&
                rotorConfiguration == DroneRotorConfiguration.Quadcopter &&
                TryBuildImportedQuadcopterBody(visualRoot, accentColor, armLength);

            if (!builtFromImportedAsset)
                BuildBody(visualRoot, accentColor, batteryColor ?? BatteryBlack, batterySizeMultiplier);

            // Motors + spinning rotor blades are always built procedurally, even
            // when using the imported static airframe above — a plain mesh import
            // has no per-part pivots/animation to carry (see
            // TryBuildImportedQuadcopterBody's doc comment), so the imported
            // wireframe deliberately excludes them (see
            // BuildStaticAirframeForExport) and they're layered on top here
            // instead, exactly as if built procedurally throughout.
            int rotorCount = Mathf.Max(3, rotorConfiguration.ToRotorCount());
            float angleStep = 360f / rotorCount;
            for (int i = 0; i < rotorCount; i++)
            {
                // Start at 45 degrees for an "X" configuration (arms between the
                // body's forward/back/left/right axes) — the common FPV/multirotor look.
                float angleDeg = 45f + i * angleStep;
                Vector3 tipPosition = Quaternion.Euler(0f, angleDeg, 0f) * Vector3.forward * armLength;

                if (!builtFromImportedAsset)
                    BuildArm(visualRoot, angleDeg, armLength, accentColor, tipPosition);

                BuildRotor(visualRoot, tipPosition, armLength, bladeColor ?? PropPlastic, bladeSizeMultiplier);
            }

            hardpoints = CreateHardpointSockets(visualRoot, hardpointCount, halfSpanX: armLength * 0.5f, y: -0.26f, z: -0.05f);

            return visualRoot;
        }

        /// <summary>
        /// Builds only the static (non-spinning) airframe — body + arms/tips, no
        /// motors/rotor blades — for <see cref="Vanquish.EditorTools.QuadcopterWireframeExporter"/>
        /// to export as a wireframe. The live game always builds motors/spinning
        /// rotor blades procedurally (see <see cref="Build"/>) even when using an
        /// imported static airframe, so those are deliberately excluded from this
        /// export-only build — otherwise the imported (non-spinning) copies would
        /// render duplicated underneath the real spinning ones.
        /// </summary>
        public static Transform BuildStaticAirframeForExport(Transform parent, Color accentColor, DroneRotorConfiguration rotorConfiguration, float armLength = 0.5f)
        {
            var visualRoot = new GameObject("Visual").transform;
            visualRoot.SetParent(parent, false);

            BuildBody(visualRoot, accentColor, BatteryBlack, batterySizeMultiplier: 1f);

            int rotorCount = Mathf.Max(3, rotorConfiguration.ToRotorCount());
            float angleStep = 360f / rotorCount;
            for (int i = 0; i < rotorCount; i++)
            {
                float angleDeg = 45f + i * angleStep;
                Vector3 tipPosition = Quaternion.Euler(0f, angleDeg, 0f) * Vector3.forward * armLength;
                BuildArm(visualRoot, angleDeg, armLength, accentColor, tipPosition);
            }

            return visualRoot;
        }

        // Names Unity gave the two "team ID mark" materials on import — matching
        // the "mat_" + part-name convention ObjExporter's MTL writer uses (see
        // GetOrWriteMaterial). Unity's importer merges every "o" group from the
        // OBJ into one combined mesh with one submesh/material per unique
        // material name (not one GameObject per part), so tinting has to target
        // these specific materials on the single renderer, not named child objects.
        private const string StackMaterialName = "mat_Stack";
        private const string ArmTipMaterialName = "mat_ArmTip";

        /// <summary>
        /// Instantiates the imported quadcopter model (if present) under
        /// <paramref name="visualRoot"/>, rescaled to approximate whatever
        /// <paramref name="armLength"/> the caller asked for (the imported mesh is
        /// a fixed size baked at export time, unlike the procedural arms which
        /// scale exactly), and tints its Stack/ArmTip submesh materials with
        /// <paramref name="accentColor"/> to keep the same "colored parts = team ID
        /// mark" convention the procedural body uses. Returns false (caller falls
        /// back to the procedural body) if no imported model exists at
        /// <see cref="QuadcopterModelResourcePath"/>.
        ///
        /// This only covers the static airframe (frame/arms/legs/camera/battery) —
        /// motors and spinning rotor blades are never part of the import (see
        /// <see cref="BuildStaticAirframeForExport"/>) and are always layered on
        /// top procedurally by <see cref="Build"/>, since a plain mesh import has
        /// no per-part pivots/animation to carry (every vertex gets baked into one
        /// shared root space, and Unity's importer further merges every part into
        /// a single combined mesh — see <see cref="Vanquish.EditorTools.ObjExporter"/>).
        /// </summary>
        private static bool TryBuildImportedQuadcopterBody(Transform visualRoot, Color accentColor, float armLength)
        {
            GameObject prefab = Resources.Load<GameObject>(QuadcopterModelResourcePath);
            if (prefab == null)
                return false;

            GameObject instance = Object.Instantiate(prefab, visualRoot, false);
            instance.name = "ImportedAirframe";
            instance.transform.localScale = Vector3.one * (armLength / ExportedQuadcopterArmLength);

            foreach (Renderer partRenderer in instance.GetComponentsInChildren<Renderer>())
            {
                Material[] materials = partRenderer.materials; // instances — the shared imported asset's materials are untouched
                bool anyTinted = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    string materialName = materials[i].name.Replace(" (Instance)", "");
                    if (materialName != StackMaterialName && materialName != ArmTipMaterialName)
                        continue;

                    materials[i].color = accentColor;
                    anyTinted = true;
                }

                if (anyTinted)
                    partRenderer.materials = materials;
            }

            return true;
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

        private static void BuildBody(Transform visualRoot, Color accentColor, Color batteryColor, float batterySizeMultiplier)
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
            // one of the most recognizable "this is a real drone" details. Size and
            // color are design-driven (see PlanPartCatalog.Batteries) so a higher-
            // tier battery choice visibly reads as a bigger/different pack, not just
            // a stat change.
            GameObject battery = GameObject.CreatePrimitive(PrimitiveType.Cube);
            battery.name = "Battery";
            battery.transform.SetParent(visualRoot, false);
            battery.transform.localPosition = new Vector3(0f, -0.075f * batterySizeMultiplier, -0.08f);
            battery.transform.localScale = new Vector3(0.18f, 0.09f, 0.4f) * batterySizeMultiplier;
            ApplyMaterial(battery, batteryColor, metallic: 0.1f, smoothness: 0.3f);
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

            // Thin VTX antenna angled up/back off the stack — a small extra detail
            // real FPV quads always have, breaking up the silhouette beyond just
            // "flat plate + box on top."
            GameObject antenna = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            antenna.name = "Antenna";
            antenna.transform.SetParent(visualRoot, false);
            antenna.transform.localPosition = new Vector3(0.09f, 0.16f, -0.24f);
            antenna.transform.localRotation = Quaternion.Euler(35f, 0f, -12f);
            antenna.transform.localScale = new Vector3(0.012f, 0.11f, 0.012f);
            ApplyMaterial(antenna, CarbonFrame, metallic: 0.1f, smoothness: 0.3f);
            RemoveCollider(antenna);

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

        /// <summary>The static arm + colored tip cap — no motor/blades (see <see cref="BuildRotor"/> for those).</summary>
        private static void BuildArm(Transform visualRoot, float angleDeg, float armLength, Color accentColor, Vector3 tipPosition)
        {
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "Arm";
            arm.transform.SetParent(visualRoot, false);
            arm.transform.localRotation = Quaternion.Euler(0f, angleDeg, 0f);
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
        }

        // A real prop's disc is large relative to the arm — nearly reaching the
        // neighboring arms — not a tiny sliver. PropRadius drives both the blade
        // length and the guard ring below, so they always stay in proportion to
        // each other regardless of armLength.
        private const float PropRadiusFraction = 0.46f;
        private const int PropGuardSegments = 10;

        /// <summary>The motor + spinning blades + a static (non-spinning) prop guard ring at one arm's tip — always built procedurally (never imported), so rotors always actually spin (see <see cref="RotorSpinner"/>).</summary>
        private static void BuildRotor(Transform visualRoot, Vector3 tipPosition, float armLength, Color bladeColor, float bladeSizeMultiplier)
        {
            GameObject motor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            motor.name = "Motor";
            motor.transform.SetParent(visualRoot, false);
            motor.transform.localPosition = tipPosition + Vector3.up * 0.035f;
            motor.transform.localScale = new Vector3(0.1f, 0.05f, 0.1f);
            ApplyMaterial(motor, GunmetalMotor, metallic: 0.75f, smoothness: 0.6f);
            RemoveCollider(motor);

            // bladeSizeMultiplier is a design-driven knob independent of armLength
            // (see PlanPartCatalog.Propellers) — e.g. "larger blades" as a distinct
            // propeller choice, not just a bigger airframe.
            float propRadius = armLength * PropRadiusFraction * bladeSizeMultiplier;

            // Spin pivot is a sibling of the motor (not its child) so it doesn't
            // inherit the motor's non-uniform scale when positioning/sizing the blades.
            var spinPivot = new GameObject("RotorSpin");
            spinPivot.transform.SetParent(visualRoot, false);
            spinPivot.transform.localPosition = tipPosition + Vector3.up * 0.07f;
            spinPivot.AddComponent<RotorSpinner>();

            // Two crossed blades (not one) so the spinning prop reads as a disc
            // silhouette rather than a single flat bar.
            BuildPropBlade(spinPivot.transform, propRadius, yRotation: 0f, bladeColor);
            BuildPropBlade(spinPivot.transform, propRadius, yRotation: 90f, bladeColor);

            // Static prop guard ring around the blades — a real racing-drone detail
            // that also visually reads as "there's a proper rotor disc here" even
            // when the blades themselves are a thin blur/silhouette, addressing how
            // small/sparse a bare spinning blade alone reads at drone scale.
            BuildPropGuardRing(visualRoot, spinPivot.transform.localPosition, propRadius * 1.18f);
        }

        private static void BuildPropBlade(Transform spinPivot, float propRadius, float yRotation, Color bladeColor)
        {
            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Blades";
            blade.transform.SetParent(spinPivot, false);
            blade.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            blade.transform.localScale = new Vector3(propRadius * 2f, 0.012f, 0.05f);
            ApplyMaterial(blade, bladeColor, metallic: 0.05f, smoothness: 0.35f);
            RemoveCollider(blade);
        }

        /// <summary>
        /// A circular guard built from short straight segments around one rotor's
        /// blades (Unity has no hollow-ring primitive) — fixed to the frame, not
        /// spinning, unlike the blades it encircles.
        /// </summary>
        private static void BuildPropGuardRing(Transform visualRoot, Vector3 center, float radius)
        {
            var ring = new GameObject("PropGuard");
            ring.transform.SetParent(visualRoot, false);
            ring.transform.localPosition = center;

            float segmentLength = 2f * radius * Mathf.Sin(Mathf.PI / PropGuardSegments);
            float angleStep = 360f / PropGuardSegments;

            for (int i = 0; i < PropGuardSegments; i++)
            {
                float angleDeg = i * angleStep;
                Vector3 vertexOffset = Quaternion.Euler(0f, angleDeg, 0f) * Vector3.forward * radius;

                GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                segment.name = "PropGuardSegment";
                segment.transform.SetParent(ring.transform, false);
                segment.transform.localPosition = vertexOffset;
                segment.transform.localRotation = Quaternion.Euler(0f, angleDeg + 90f, 0f);
                segment.transform.localScale = new Vector3(0.008f, 0.05f, segmentLength);
                ApplyMaterial(segment, GunmetalMotor, metallic: 0.6f, smoothness: 0.4f);
                RemoveCollider(segment);
            }
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
