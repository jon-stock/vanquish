using System.Collections.Generic;
using UnityEngine;

namespace Vanquish.Theatre.Play
{
    /// <summary>
    /// Builds a distinctive, procedural (primitives-only — no imported art, same
    /// "no imported art assets" convention as
    /// <see cref="Vanquish.Combat.Play.DroneVisualBuilder"/>) small silhouette per
    /// <see cref="SiteType"/>, so buildings read as different kinds of
    /// installations at a glance instead of one uniformly-scaled colored cube.
    ///
    /// Note on realism: true photorealistic/"ultra HD"/perfect-likeness building
    /// models aren't achievable this way — that needs authored 3D art (modeled,
    /// UV-unwrapped, and textured meshes with real PBR materials, typically from an
    /// art pipeline, asset store, or 3D scan), which is a fundamentally different
    /// scope than assembling Unity primitives procedurally at runtime. This builder
    /// only makes the existing procedural convention read as distinct shapes, not
    /// realistic materials/geometry — consistent with every other visual in this
    /// project (drones, missiles, hex tiles) being primitive-built rather than
    /// imported art.
    ///
    /// Every shape is authored with its local origin at ground level (resting on
    /// y=0 of <paramref name="parent"/>), so callers only need to position
    /// <paramref name="parent"/> at the hex's surface height — no per-type vertical
    /// offset math needed at the call site.
    /// </summary>
    public static class SiteVisualBuilder
    {
        public static Renderer[] Build(Transform parent, SiteType type, Color color)
        {
            var renderers = new List<Renderer>();

            switch (type)
            {
                case SiteType.Factory:
                    BuildFactory(parent, color, renderers);
                    break;
                case SiteType.Warehouse:
                    BuildWarehouse(parent, color, renderers);
                    break;
                case SiteType.Base:
                    BuildBase(parent, color, renderers);
                    break;
                case SiteType.RadarInstallation:
                    BuildRadar(parent, color, renderers);
                    break;
                case SiteType.LaunchPlatform:
                    BuildAirfield(parent, color, renderers);
                    break;
                case SiteType.ReconStation:
                    BuildReconStation(parent, color, renderers);
                    break;
                case SiteType.Lab:
                    BuildLab(parent, color, renderers);
                    break;
                default:
                    renderers.Add(CreateBox(parent, "Body", new Vector3(0f, 0.25f, 0f), new Vector3(0.55f, 0.5f, 0.55f), color));
                    break;
            }

            return renderers.ToArray();
        }

        /// <summary>Large main hall + a smaller annex + a smokestack — reads as an industrial plant.</summary>
        private static void BuildFactory(Transform parent, Color color, List<Renderer> renderers)
        {
            renderers.Add(CreateBox(parent, "MainHall", new Vector3(0f, 0.275f, 0f), new Vector3(0.75f, 0.55f, 0.5f), color));
            renderers.Add(CreateBox(parent, "AnnexWing", new Vector3(0.35f, 0.2f, 0.32f), new Vector3(0.32f, 0.4f, 0.22f), color));
            renderers.Add(CreateCylinder(parent, "Smokestack", new Vector3(-0.2f, 0.8f, -0.1f), radius: 0.06f, height: 0.5f, color));
        }

        /// <summary>Long low body with an angled two-panel roof — reads as a barn/warehouse.</summary>
        private static void BuildWarehouse(Transform parent, Color color, List<Renderer> renderers)
        {
            renderers.Add(CreateBox(parent, "Body", new Vector3(0f, 0.2f, 0f), new Vector3(0.85f, 0.4f, 0.55f), color));

            var roofPivot = new GameObject("RoofPivot");
            roofPivot.transform.SetParent(parent, false);
            roofPivot.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            renderers.Add(CreateBox(roofPivot.transform, "RoofLeft", new Vector3(-0.21f, 0.06f, 0f), new Vector3(0.5f, 0.06f, 0.58f), color, rotationZ: 22f));
            renderers.Add(CreateBox(roofPivot.transform, "RoofRight", new Vector3(0.21f, 0.06f, 0f), new Vector3(0.5f, 0.06f, 0.58f), color, rotationZ: -22f));
        }

        /// <summary>Barracks block + annex + a radio mast — reads as a military base.</summary>
        private static void BuildBase(Transform parent, Color color, List<Renderer> renderers)
        {
            renderers.Add(CreateBox(parent, "Barracks", new Vector3(0f, 0.175f, 0f), new Vector3(0.6f, 0.35f, 0.45f), color));
            renderers.Add(CreateBox(parent, "Annex", new Vector3(0.4f, 0.125f, -0.05f), new Vector3(0.28f, 0.25f, 0.3f), color));
            renderers.Add(CreateCylinder(parent, "Mast", new Vector3(-0.22f, 0.2f, 0.15f), radius: 0.025f, height: 0.4f, color));
            renderers.Add(CreateSphere(parent, "MastTip", new Vector3(-0.22f, 0.44f, 0.15f), radius: 0.05f, color));
        }

        /// <summary>A tall mast topped by a tilted dish — reads as a radar installation.</summary>
        private static void BuildRadar(Transform parent, Color color, List<Renderer> renderers)
        {
            renderers.Add(CreateCylinder(parent, "Mast", new Vector3(0f, 0.325f, 0f), radius: 0.07f, height: 0.65f, color));

            var dishPivot = new GameObject("DishPivot");
            dishPivot.transform.SetParent(parent, false);
            dishPivot.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            dishPivot.transform.localRotation = Quaternion.Euler(35f, 0f, 0f);
            renderers.Add(CreateCylinder(dishPivot.transform, "Dish", Vector3.zero, radius: 0.32f, height: 0.04f, color));
        }

        /// <summary>A flat pad with a small control tower — reads as an airfield.</summary>
        private static void BuildAirfield(Transform parent, Color color, List<Renderer> renderers)
        {
            renderers.Add(CreateBox(parent, "Pad", new Vector3(0f, 0.04f, 0f), new Vector3(0.95f, 0.08f, 0.65f), color));
            renderers.Add(CreateBox(parent, "Tower", new Vector3(-0.3f, 0.355f, 0f), new Vector3(0.18f, 0.55f, 0.18f), color));
            renderers.Add(CreateBox(parent, "TowerCab", new Vector3(-0.3f, 0.7f, 0f), new Vector3(0.26f, 0.14f, 0.26f), color));
        }

        /// <summary>A short tower with an observation dome — reads as a recon station.</summary>
        private static void BuildReconStation(Transform parent, Color color, List<Renderer> renderers)
        {
            renderers.Add(CreateCylinder(parent, "Tower", new Vector3(0f, 0.25f, 0f), radius: 0.16f, height: 0.5f, color));
            renderers.Add(CreateSphere(parent, "Dome", new Vector3(0f, 0.58f, 0f), radius: 0.18f, color));
        }

        /// <summary>A block with a research dome on top — reads as a lab.</summary>
        private static void BuildLab(Transform parent, Color color, List<Renderer> renderers)
        {
            renderers.Add(CreateBox(parent, "Body", new Vector3(0f, 0.225f, 0f), new Vector3(0.55f, 0.45f, 0.5f), color));
            renderers.Add(CreateSphere(parent, "Dome", new Vector3(0f, 0.58f, 0f), radius: 0.28f, color));
        }

        private static Renderer CreateBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color, float rotationZ = 0f)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            go.transform.localScale = localScale;
            RemoveCollider(go);
            return ApplyColor(go, color);
        }

        // Unity's built-in cylinder primitive has a default radius of 0.5 and
        // height of 2, so localScale.xz = radius*2 and localScale.y = height/2
        // reproduce the requested world-space radius/height.
        private static Renderer CreateCylinder(Transform parent, string name, Vector3 localPosition, float radius, float height, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            RemoveCollider(go);
            return ApplyColor(go, color);
        }

        // Unity's built-in sphere primitive has a default diameter of 1, so
        // localScale = radius*2 reproduces the requested world-space radius.
        private static Renderer CreateSphere(Transform parent, string name, Vector3 localPosition, float radius, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = Vector3.one * radius * 2f;
            RemoveCollider(go);
            return ApplyColor(go, color);
        }

        private static Renderer ApplyColor(GameObject go, Color color)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color;
            return renderer;
        }

        private static void RemoveCollider(GameObject go)
        {
            // Purely visual pieces — sites are selected via the hex tile's own
            // collider underneath, not by clicking the marker itself.
            var collider = go.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);
        }
    }
}
