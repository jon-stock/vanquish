using System.Collections.Generic;
using UnityEngine;

namespace Vanquish.Theatre.Play
{
    /// <summary>
    /// Renders a small animated 2D icon of a <see cref="DronePlan"/>'s 3D preview
    /// (<see cref="PlanPreviewBuilder"/>) for use directly inside the bottom
    /// action bar's cards — replacing the previous "3D models floating in world
    /// space next to whichever building is selected" showcase, per design
    /// feedback that it was unclear/out of place. The preview model is built far
    /// below the actual map (never visible to the main theatre camera) and
    /// captured, with a transparent background, once per rotation angle by a
    /// disposable one-shot camera into a small ring of cached
    /// <see cref="Texture2D"/> frames keyed by the plan's name/category/accent
    /// color — designing/producing more of the same Plan never triggers a
    /// re-render. <see cref="GetOrCreateIcon"/> then picks whichever frame in the
    /// ring corresponds to the current time, so the icon appears to spin in
    /// place wherever it's drawn (OnGUI immediate-mode textures are cheap to
    /// swap per-frame; re-rendering the 3D preview every draw would not be).
    /// </summary>
    public static class PlanIconRenderer
    {
        private const int IconSize = 128;

        // Number of pre-rendered angles making up one full rotation, and how
        // fast the icon appears to spin. Both are tuning knobs, not correctness
        // constraints — more frames costs more one-time render setup + memory
        // per unique plan, but yields a smoother spin.
        private const int FrameCount = 48;
        private const float DegreesPerSecond = 60f;

        // Far enough below the visible map that it's never in the main theatre
        // camera's view frustum — simpler than culling-mask/layer bookkeeping for
        // a one-shot, immediately-destroyed capture rig.
        private static readonly Vector3 StagePosition = new Vector3(0f, -5000f, 0f);

        private static readonly Dictionary<string, Texture2D[]> Cache = new Dictionary<string, Texture2D[]>();

        /// <summary>
        /// Returns whichever pre-rendered rotation frame of <paramref name="plan"/>'s
        /// icon corresponds to the current time, rendering (and caching) the full
        /// rotation ring on first use.
        /// </summary>
        public static Texture2D GetOrCreateIcon(DronePlan plan)
        {
            Texture2D[] frames = GetOrCreateIconFrames(plan);
            float degrees = (Time.time * DegreesPerSecond) % 360f;
            int index = Mathf.FloorToInt(degrees / (360f / FrameCount)) % FrameCount;
            return frames[index];
        }

        /// <summary>Returns (rendering and caching on first use) the full ring of rotation frames depicting <paramref name="plan"/>.</summary>
        public static Texture2D[] GetOrCreateIconFrames(DronePlan plan)
        {
            string key = CacheKey(plan);
            if (Cache.TryGetValue(key, out Texture2D[] cached) && cached != null && cached.Length == FrameCount && cached[0] != null)
                return cached;

            Texture2D[] frames = RenderFrames(plan);
            Cache[key] = frames;
            return frames;
        }

        /// <summary>Frees every cached icon texture — call on New Game/Load so stale plans (from a previous session) don't linger in memory forever.</summary>
        public static void ClearCache()
        {
            foreach (Texture2D[] frames in Cache.Values)
            {
                if (frames == null)
                    continue;
                foreach (Texture2D texture in frames)
                {
                    if (texture != null)
                        Object.DestroyImmediate(texture);
                }
            }
            Cache.Clear();
        }

        private static string CacheKey(DronePlan plan) =>
            $"{plan.Name}|{plan.Category}|{ColorUtility.ToHtmlStringRGBA(plan.AccentColor)}";

        private static Texture2D[] RenderFrames(DronePlan plan)
        {
            var stage = new GameObject($"PlanIconStage_{plan.Name}");
            RenderTexture renderTexture = null;
            try
            {
                stage.transform.position = StagePosition;

                // The model sits on its own child transform (rotated per frame
                // below) so the camera — a sibling, not a child of the model —
                // stays fixed relative to the stage while the model spins.
                var modelRoot = new GameObject("Model");
                modelRoot.transform.SetParent(stage.transform, false);
                // Larger scale than PlanPreviewBuilder's own 0.6f default, and a
                // tighter camera framing below — both push the model to fill the
                // icon frame edge-to-edge instead of leaving a lot of empty margin
                // around a small centered subject.
                PlanPreviewBuilder.Build(modelRoot.transform, plan, scale: 0.95f);

                var cameraGo = new GameObject("PlanIconCamera");
                cameraGo.transform.SetParent(stage.transform, false);
                cameraGo.transform.localPosition = new Vector3(0f, 0.22f, -0.62f);
                cameraGo.transform.localRotation = Quaternion.LookRotation(new Vector3(0f, -0.22f, 0.62f), Vector3.up);

                var camera = cameraGo.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f); // transparent — alpha written by the render texture below
                camera.fieldOfView = 30f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 4f;
                camera.enabled = false; // rendered manually below, once per frame

                renderTexture = new RenderTexture(IconSize, IconSize, 16, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;

                var frames = new Texture2D[FrameCount];
                RenderTexture previousActive = RenderTexture.active;
                for (int i = 0; i < FrameCount; i++)
                {
                    modelRoot.transform.localRotation = Quaternion.Euler(0f, i * (360f / FrameCount), 0f);
                    camera.Render();

                    RenderTexture.active = renderTexture;
                    var texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
                    texture.ReadPixels(new Rect(0, 0, IconSize, IconSize), 0, 0);
                    texture.Apply();
                    frames[i] = texture;
                }
                RenderTexture.active = previousActive;

                return frames;
            }
            finally
            {
                // DestroyImmediate (not Destroy) — this can run in the Editor
                // outside Play mode too (e.g. the headless smoke test), where
                // Destroy only warns and defers rather than actually destroying.
                Object.DestroyImmediate(stage);
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    Object.DestroyImmediate(renderTexture);
                }
            }
        }
    }
}
