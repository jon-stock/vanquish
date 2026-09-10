using System.Collections.Generic;
using UnityEngine;

namespace Vanquish.Theatre.Play
{
    /// <summary>
    /// Builds a procedural missile mesh from scratch — a real nose cone (an actual
    /// triangulated cone mesh, not a squashed sphere), a cylindrical body, tail
    /// fins, and propulsion-specific tail/intake detail — for a Missile
    /// <see cref="DronePlan"/>'s preview (see <see cref="PlanPreviewBuilder"/>).
    /// No imported art, same "primitives/procedural meshes only" convention as
    /// <see cref="Combat.Play.DroneVisualBuilder"/>. Every visible part is driven by
    /// the design's actual selected parts (<see cref="PlanPartCatalog"/>):
    /// <list type="bullet">
    /// <item>Nose cone color/size — <see cref="PartSlot.Warhead"/>.</item>
    /// <item>Body length/color — <see cref="PartSlot.Propulsion"/>.</item>
    /// <item>Tail fin color, present at all — <see cref="PartSlot.Guidance"/> (omitted for an unguided design).</item>
    /// <item>Tail/intake detail — <see cref="PartSlot.Propulsion"/>: a solid rocket motor gets a plain tapered nozzle;
    /// a dual-pulse motor adds a stage-separator ring; a ramjet adds a boxy underside intake scoop with a
    /// darker recessed "mouth"; a scramjet gets a larger, sharper intake plus stabilizing side strakes.</item>
    /// </list>
    /// </summary>
    public static class MissileVisualBuilder
    {
        private const float BodyRadius = 0.08f;
        private const float NoseHeight = 0.22f;
        private const float NozzleHeight = 0.09f;
        private const int ConeSegments = 14;

        public static GameObject Build(Transform parent, DronePlan plan)
        {
            PlanPartOption? warhead = PlanPartCatalog.Find(PartSlot.Warhead, plan.WarheadId);
            PlanPartOption? guidance = PlanPartCatalog.Find(PartSlot.Guidance, plan.GuidanceId);
            PlanPartOption? propulsion = PlanPartCatalog.Find(PartSlot.Propulsion, plan.PropulsionId);

            float bodyLength = 0.55f * (propulsion?.VisualSizeMultiplier ?? 1f);
            Color bodyColor = propulsion?.VisualColor ?? plan.AccentColor;
            Color noseColor = warhead?.VisualColor ?? plan.AccentColor;
            bool hasGuidance = guidance != null && guidance.Value.Id != PlanPartCatalog.NoGuidanceId;
            Color finColor = hasGuidance ? guidance.Value.VisualColor : new Color(0.28f, 0.28f, 0.3f);

            var root = new GameObject("MissileAssembly");
            root.transform.SetParent(parent, false);

            // Everything below is built pointing "up" (+Y = nose direction), then
            // the whole assembly is laid on its side so it reads as a missile lying
            // flat rather than standing on its tail — matches the orientation
            // convention the old capsule-based preview used.
            root.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            BuildBody(root.transform, bodyLength, bodyColor);
            BuildNoseCone(root.transform, bodyLength, noseColor);
            BuildFins(root.transform, finColor);
            BuildPropulsionDetail(root.transform, bodyLength, propulsion);

            return root;
        }

        private static void BuildBody(Transform parent, float bodyLength, Color color)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            body.transform.SetParent(parent, false);
            body.transform.localPosition = new Vector3(0f, bodyLength * 0.5f, 0f);
            body.transform.localScale = new Vector3(BodyRadius * 2f, bodyLength * 0.5f, BodyRadius * 2f);
            Tint(body, color, metallic: 0.5f, smoothness: 0.5f);
        }

        private static void BuildNoseCone(Transform parent, float bodyLength, Color color)
        {
            GameObject cone = CreateCone(parent, "NoseCone", BodyRadius, NoseHeight, ConeSegments, color);
            cone.transform.localPosition = new Vector3(0f, bodyLength, 0f);
        }

        private static void BuildFins(Transform parent, Color color)
        {
            for (int i = 0; i < 4; i++)
            {
                float angleDeg = i * 90f;
                Quaternion rotation = Quaternion.Euler(0f, angleDeg, 0f);

                GameObject fin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fin.name = "Fin";
                fin.transform.SetParent(parent, false);
                fin.transform.localRotation = rotation;
                fin.transform.localPosition = rotation * new Vector3(BodyRadius + 0.05f, 0f, 0f) + new Vector3(0f, 0.05f, 0f);
                fin.transform.localScale = new Vector3(0.11f, 0.02f, 0.07f);
                Tint(fin, color, metallic: 0.3f, smoothness: 0.4f);
            }
        }

        private static void BuildPropulsionDetail(Transform parent, float bodyLength, PlanPartOption? propulsion)
        {
            string id = propulsion?.Id ?? "mprop_solid_rocket";
            Color accent = propulsion?.VisualColor ?? new Color(0.25f, 0.25f, 0.27f);

            switch (id)
            {
                case "mprop_dual_pulse":
                    BuildSeparatorRing(parent, bodyLength * 0.55f, accent);
                    BuildNozzle(parent, accent);
                    break;
                case "mprop_ramjet":
                    BuildIntakeScoop(parent, bodyLength * 0.35f, accent, sizeMultiplier: 1f);
                    BuildNozzle(parent, accent);
                    break;
                case "mprop_scramjet":
                    BuildIntakeScoop(parent, bodyLength * 0.3f, accent, sizeMultiplier: 1.5f);
                    BuildStrakes(parent, bodyLength, accent);
                    BuildNozzle(parent, accent);
                    break;
                default: // mprop_solid_rocket baseline — plain tail, just the nozzle
                    BuildNozzle(parent, accent);
                    break;
            }
        }

        /// <summary>A tapered nozzle cone at the very tail, pointing backward off the body.</summary>
        private static void BuildNozzle(Transform parent, Color accent)
        {
            GameObject nozzle = CreateCone(parent, "Nozzle", BodyRadius * 0.85f, NozzleHeight, ConeSegments, new Color(0.12f, 0.12f, 0.13f));
            nozzle.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
            nozzle.transform.localPosition = Vector3.zero;
        }

        /// <summary>A thin ring band partway up the body — a visible "second stage" seam for a dual-pulse motor.</summary>
        private static void BuildSeparatorRing(Transform parent, float y, Color color)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "StageSeparator";
            ring.transform.SetParent(parent, false);
            ring.transform.localPosition = new Vector3(0f, y, 0f);
            ring.transform.localScale = new Vector3(BodyRadius * 2.15f, 0.015f, BodyRadius * 2.15f);
            Tint(ring, color, metallic: 0.6f, smoothness: 0.5f);
        }

        /// <summary>
        /// A boxy scoop/duct on the underside of the body, with a darker recessed
        /// "mouth" inset into its front face so it reads as a hollow air intake
        /// rather than a solid block — bigger and more angular (<paramref name="sizeMultiplier"/>)
        /// for a scramjet than a subsonic ramjet.
        /// </summary>
        private static void BuildIntakeScoop(Transform parent, float y, Color color, float sizeMultiplier)
        {
            GameObject scoop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            scoop.name = "IntakeScoop";
            scoop.transform.SetParent(parent, false);
            scoop.transform.localPosition = new Vector3(0f, y, -(BodyRadius + 0.028f * sizeMultiplier));
            scoop.transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
            scoop.transform.localScale = new Vector3(0.05f * sizeMultiplier, 0.05f * sizeMultiplier, 0.15f * sizeMultiplier);
            Tint(scoop, color, metallic: 0.4f, smoothness: 0.4f);

            GameObject mouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mouth.name = "IntakeMouth";
            mouth.transform.SetParent(scoop.transform, false);
            mouth.transform.localPosition = new Vector3(0f, 0f, 0.42f);
            mouth.transform.localScale = new Vector3(0.72f, 0.72f, 0.3f);
            Tint(mouth, new Color(0.02f, 0.02f, 0.025f), metallic: 0.1f, smoothness: 0.1f);
        }

        /// <summary>Two thin stabilizing strakes along the body sides — a hypersonic-airframe detail for the scramjet tier.</summary>
        private static void BuildStrakes(Transform parent, float bodyLength, Color color)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                GameObject strake = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strake.name = "Strake";
                strake.transform.SetParent(parent, false);
                strake.transform.localPosition = new Vector3(side * (BodyRadius + 0.018f), bodyLength * 0.6f, 0f);
                strake.transform.localScale = new Vector3(0.018f, bodyLength * 0.5f, 0.045f);
                Tint(strake, color, metallic: 0.3f, smoothness: 0.4f);
            }
        }

        /// <summary>Builds a real triangulated cone mesh (apex at local +Y * height, base circle of <paramref name="radius"/> at the local origin) — no built-in Unity cone primitive exists, so this generates one directly rather than faking it with a squashed sphere.</summary>
        private static GameObject CreateCone(Transform parent, string name, float radius, float height, int segments, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var vertices = new List<Vector3> { new Vector3(0f, height, 0f), Vector3.zero };
            var triangles = new List<int>();

            int firstRim = vertices.Count;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }

            for (int i = 0; i < segments; i++)
            {
                int current = firstRim + i;
                int next = firstRim + (i + 1) % segments;

                // Side wall (apex, current, next).
                triangles.Add(0);
                triangles.Add(current);
                triangles.Add(next);

                // Base cap (baseCenter, next, current) — opposite winding to face downward/outward.
                triangles.Add(1);
                triangles.Add(next);
                triangles.Add(current);
            }

            var mesh = new Mesh { name = "Cone" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            go.AddComponent<MeshFilter>().mesh = mesh;
            go.AddComponent<MeshRenderer>();
            Tint(go, color, metallic: 0.3f, smoothness: 0.5f);

            return go;
        }

        private static void Tint(GameObject go, Color color, float metallic, float smoothness)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);

            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return;

            Material material = renderer.material;
            material.color = color;
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            else if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", smoothness);
        }
    }
}
