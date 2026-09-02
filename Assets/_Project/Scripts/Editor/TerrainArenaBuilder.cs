using System;
using UnityEngine;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Phase 2E's terrain-approach decision, made concrete: procedural heightmap
    /// terrain generated entirely from a height function in script, not hand-sculpted
    /// in the Editor and not an imported terrain asset pack. This was chosen because
    /// it's the only approach consistent with this project's existing "everything
    /// reproducible via code" convention (every scene, down to the ground plane, is
    /// built by an Editor script — Phase1CombatSceneBuilder.BuildGround) — hand-sculpted
    /// terrain can't be diffed/regenerated/tuned by changing a parameter the way every
    /// other part of these scenes can, and an asset pack adds an external dependency
    /// for no real benefit at this project's current "ugly art is fine" stage. Uses
    /// only the built-in Terrain/TerrainPhysics modules (confirmed present in
    /// Packages/manifest.json) — no terrain-tools package needed for scripted
    /// heightmap generation via TerrainData.SetHeights.
    ///
    /// GroundSampler (Simulation/Flight) already anticipated this: it samples ground
    /// height via a real downward raycast against physics colliders rather than
    /// hardcoding a flat y=0, specifically so it would "pick up actual terrain
    /// colliders unmodified once Phase 2E adds them" — Terrain.CreateTerrainGameObject
    /// adds a TerrainCollider automatically, so no GroundSampler changes were needed.
    /// </summary>
    internal static class TerrainArenaBuilder
    {
        /// <summary>
        /// Builds and returns a Terrain GameObject sized widthMeters x heightMeters (max
        /// elevation) x depthMeters, with per-vertex height sampled from heightFunction
        /// (inputs 0..1 normalized across width/depth, output 0..1 normalized elevation).
        /// Positioned so the terrain's horizontal center sits at world XZ origin, matching
        /// every other scene builder's "arena centered on Vector3.zero" convention
        /// (Terrain's own transform position is its min corner, not its center, unlike
        /// every other GameObject built by these scripts — corrected for here).
        /// </summary>
        internal static Terrain BuildTerrain(string name, float widthMeters, float heightMeters, float depthMeters,
            Func<float, float, float> heightFunction, Color baseColor, int resolution = 129)
        {
            var terrainData = new TerrainData
            {
                heightmapResolution = resolution,
                size = new Vector3(widthMeters, heightMeters, depthMeters),
            };

            int size = terrainData.heightmapResolution;
            var heights = new float[size, size];
            for (int z = 0; z < size; z++)
            {
                float normalizedZ = (float)z / (size - 1);
                for (int x = 0; x < size; x++)
                {
                    float normalizedX = (float)x / (size - 1);
                    // TerrainData.SetHeights indexes as [y, x] where "y" here is
                    // actually the terrain's Z axis, not world Y — Unity's own
                    // (confusingly-named) convention for this API.
                    heights[z, x] = Mathf.Clamp01(heightFunction(normalizedX, normalizedZ));
                }
            }
            terrainData.SetHeights(0, 0, heights);
            terrainData.terrainLayers = new[] { CreateSolidColorTerrainLayer(baseColor) };

            GameObject terrainGo = Terrain.CreateTerrainGameObject(terrainData);
            terrainGo.name = name;
            terrainGo.transform.position = new Vector3(-widthMeters / 2f, 0f, -depthMeters / 2f);

            return terrainGo.GetComponent<Terrain>();
        }

        /// <summary>No imported textures — a flat-color TerrainLayer (a small solid-color
        /// procedural texture), same "primitives/procedural only" convention as
        /// DroneVisualBuilder/Phase1CombatSceneBuilder.CreateGridTexture.</summary>
        private static TerrainLayer CreateSolidColorTerrainLayer(Color color)
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { name = "ProceduralTerrainLayer" };
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            texture.SetPixels(pixels);
            texture.Apply();

            return new TerrainLayer { diffuseTexture = texture, tileSize = new Vector2(50f, 50f) };
        }

        /// <summary>World-space ground height directly below a world XZ position, reading the actual generated terrain.</summary>
        internal static float SampleWorldHeight(Terrain terrain, Vector3 worldPosition)
        {
            return terrain.SampleHeight(worldPosition) + terrain.GetPosition().y;
        }

        /// <summary>
        /// V-shaped valley along X: low in the center, rising toward both edges — long
        /// sightlines/engagement distance down the valley floor, with the valley walls
        /// themselves acting as terrain cover from anything not directly down the line.
        /// </summary>
        internal static float ValleyHeight(float normalizedX, float normalizedZ)
        {
            float distanceFromCenterLine = Mathf.Abs(normalizedX - 0.5f) * 2f; // 0 at center, 1 at edges
            return Mathf.Pow(distanceFromCenterLine, 1.6f);
        }

        /// <summary>
        /// A raised central plateau with steep cliff edges falling to lower ground —
        /// short/blocked sightlines around the plateau's edges (the cliff itself blocks
        /// line of sight from below), a different tactical shape than the valley's long
        /// sightline.
        /// </summary>
        internal static float PlateauHeight(float normalizedX, float normalizedZ)
        {
            float distanceFromCenter = new Vector2(normalizedX - 0.5f, normalizedZ - 0.5f).magnitude * 2f; // 0 at center, ~1.4 at corners
            float plateauRadius = 0.45f;
            float cliffSharpness = 10f;
            // Smoothstep-style falloff: ~1 (plateau top) inside plateauRadius, ~0 (low
            // ground) outside it, with a short steep transition band for the cliff face.
            float t = Mathf.Clamp01((plateauRadius - distanceFromCenter) * cliffSharpness + 0.5f);
            return t * t * (3f - 2f * t);
        }
    }
}
