using System.IO;
using UnityEditor;
using UnityEngine;
using Vanquish.Combat;
using Vanquish.Core;
using Vanquish.Data.Drones;
using Vanquish.Data.Missiles;
using Vanquish.Data.Shared;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Diagnostic tool: renders each planform preset to a PNG on disk via the exact
    /// same VehicleFactory/DroneVisualBuilder pipeline the Workshop preview uses, so
    /// visual bugs reported from Workshop screenshots (silhouette/proportions/camera
    /// framing issues) can be reproduced and inspected headlessly instead of guessed
    /// at from code alone. Not part of any gameplay/build path — a development aid.
    /// </summary>
    public static class Phase3HScreenshotTool
    {
        private const string DronesDir = "Assets/_Project/Data/Drones";
        private const string OutDir = "C:/Users/Jon.Stock/AppData/Local/Temp/kilo";

        [MenuItem("Vanquish/Phase 3H/Render Planform Screenshots (Debug)")]
        public static void RenderAll()
        {
            RenderPlanform("Planform_TwinTailFighter", "twintailfighter");
            RenderPlanform("Planform_CrankedKiteRecon", "crankedkiterecon");
            RenderPlanform("Planform_FlyingWingStealth", "flyingwingstealth");
        }

        [MenuItem("Vanquish/Phase 3H/Dump Planform Part Transforms (Debug)")]
        public static void DumpAllTransforms()
        {
            DumpTransforms("Planform_TwinTailFighter");
            DumpTransforms("Planform_CrankedKiteRecon");
            DumpTransforms("Planform_FlyingWingStealth");
        }

        private static void DumpTransforms(string planformAssetName)
        {
            var planform = AssetDatabase.LoadAssetAtPath<DronePlanformDefinition>($"{DronesDir}/{planformAssetName}.asset");
            if (planform == null)
            {
                Debug.LogError($"[Phase3HScreenshotTool] Could not load {planformAssetName}.");
                return;
            }

            DroneLoadout loadout = BuildStrikeLoadout(planform);
            if (loadout == null)
                return;

            var stageRoot = new GameObject("DumpStage");
            try
            {
                GameObject model = VehicleFactory.BuildVisualOnlyDrone(stageRoot.transform, loadout, Team.Player);
                Debug.Log($"[Phase3HScreenshotTool] === {planformAssetName} ===");
                foreach (var renderer in model.GetComponentsInChildren<Renderer>())
                {
                    Transform t = renderer.transform;
                    Debug.Log($"[Phase3HScreenshotTool]   {GetPath(t, model.transform)} | localPos={t.localPosition} " +
                        $"localRot={t.localRotation.eulerAngles} lossyScale={t.lossyScale} | worldBounds center={renderer.bounds.center} size={renderer.bounds.size}");
                }
            }
            finally
            {
                Object.DestroyImmediate(stageRoot);
            }
        }

        private static string GetPath(Transform t, Transform root)
        {
            string path = t.name;
            Transform cur = t.parent;
            while (cur != null && cur != root)
            {
                path = cur.name + "/" + path;
                cur = cur.parent;
            }
            return path;
        }

        private static void RenderPlanform(string planformAssetName, string outputFileSuffix)
        {
            var planform = AssetDatabase.LoadAssetAtPath<DronePlanformDefinition>($"{DronesDir}/{planformAssetName}.asset");
            if (planform == null)
            {
                Debug.LogError($"[Phase3HScreenshotTool] Could not load {planformAssetName}.");
                return;
            }

            DroneLoadout loadout = BuildStrikeLoadout(planform);
            if (loadout == null)
                return;

            var stageRoot = new GameObject("ScreenshotStage");
            try
            {
                var pivot = new GameObject("Pivot");
                pivot.transform.SetParent(stageRoot.transform, false);

                GameObject model = VehicleFactory.BuildVisualOnlyDrone(pivot.transform, loadout, Team.Player);

                Bounds bounds = ComputeBounds(model);
                float radius = Mathf.Max(bounds.extents.magnitude, 1f);

                var lightGo = new GameObject("Light");
                lightGo.transform.SetParent(stageRoot.transform, false);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                lightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

                RenderTexture rt = new RenderTexture(1024, 768, 24);
                var camGo = new GameObject("Camera");
                camGo.transform.SetParent(stageRoot.transform, false);
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.05f, 0.05f, 0.07f);
                cam.targetTexture = rt;
                cam.fieldOfView = 40f;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = radius * 20f;

                // Frame the whole bounding sphere comfortably from a 3/4-ish elevated
                // angle, same general framing philosophy as WorkshopPreviewStage but
                // auto-computed from actual bounds instead of a fixed constant, so this
                // tool works regardless of how big/small a given planform turns out to be.
                float distance = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.15f;
                Vector3 dir = new Vector3(-0.6f, 0.45f, -0.9f).normalized;
                camGo.transform.position = bounds.center + dir * distance;
                camGo.transform.LookAt(bounds.center);

                cam.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;

                Directory.CreateDirectory(OutDir);
                string path = $"{OutDir}/planform_{outputFileSuffix}.png";
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Debug.Log($"[Phase3HScreenshotTool] Wrote {path} (bounds size {bounds.size}, camera distance {distance:F1}m)");

                Object.DestroyImmediate(tex);
                cam.targetTexture = null;
                rt.Release();
                Object.DestroyImmediate(rt);
            }
            finally
            {
                Object.DestroyImmediate(stageRoot);
            }
        }

        private static Bounds ComputeBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(root.transform.position, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static DroneLoadout BuildStrikeLoadout(DronePlanformDefinition planform)
        {
            var propulsion = AssetDatabase.LoadAssetAtPath<PropulsionDefinition>($"{DronesDir}/Propulsion_Jet_Subsonic.asset");
            var engine = AssetDatabase.LoadAssetAtPath<DroneEngineDefinition>($"{DronesDir}/Engine_Jet_Subsonic.asset");
            var fuel = AssetDatabase.LoadAssetAtPath<FuelDefinition>("Assets/_Project/Data/Shared/Fuel_JetFuel_Basic.asset");
            var hull = AssetDatabase.LoadAssetAtPath<HullMaterialDefinition>($"{DronesDir}/Hull_TitaniumAlloy.asset");
            var weaponBay = AssetDatabase.LoadAssetAtPath<WeaponBayDefinition>($"{DronesDir}/WeaponBay_Large.asset");
            var sensor = AssetDatabase.LoadAssetAtPath<SensorSuiteDefinition>($"{DronesDir}/Sensor_Basic.asset");

            var missileAirframe = AssetDatabase.LoadAssetAtPath<MissileAirframeDefinition>("Assets/_Project/Data/Missiles/Airframe_Basic.asset");
            var missileEngine = AssetDatabase.LoadAssetAtPath<MissileEngineDefinition>("Assets/_Project/Data/Missiles/Engine_SolidRocket_Basic.asset");
            var missilePayload = AssetDatabase.LoadAssetAtPath<MissilePayloadDefinition>("Assets/_Project/Data/Missiles/Payload_HEFrag_Small.asset");
            var missileFuel = AssetDatabase.LoadAssetAtPath<FuelDefinition>("Assets/_Project/Data/Shared/Fuel_Solid_Basic.asset");
            var missileSeeker = AssetDatabase.LoadAssetAtPath<SeekerDefinition>("Assets/_Project/Data/Missiles/Seeker_IR_Basic.asset");

            if (propulsion == null || engine == null || fuel == null || hull == null || weaponBay == null || sensor == null ||
                missileAirframe == null || missileEngine == null || missilePayload == null || missileFuel == null || missileSeeker == null)
            {
                Debug.LogError("[Phase3HScreenshotTool] Missing one or more seeded data assets.");
                return null;
            }

            return new DroneLoadout
            {
                designName = "Phase3HScreenshotTool",
                propulsion = propulsion,
                airframe = planform.airframe,
                wingOrPropeller = planform.wing,
                hullMaterial = hull,
                engine = engine,
                fuel = fuel,
                weaponBay = weaponBay,
                sensorSuite = sensor,
                ammoCount = 2,
                missileLoadout = new MissileLoadout
                {
                    designName = "Phase3HScreenshotTool Missile",
                    airframe = missileAirframe,
                    engine = missileEngine,
                    payload = missilePayload,
                    fuel = missileFuel,
                    seeker = missileSeeker,
                },
            };
        }
    }
}
