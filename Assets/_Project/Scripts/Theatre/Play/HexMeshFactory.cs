using UnityEngine;
using Vanquish.Theatre;

namespace Vanquish.Theatre.Play
{
    /// <summary>
    /// Procedural pointy-top hexagon prism mesh + the matching axial-to-world
    /// coordinate conversion, so <see cref="HexCoordinate"/> tiles can actually be
    /// drawn and clicked in a scene. No imported art — same "primitives/procedural
    /// meshes only" convention as <c>Combat/Play/DroneVisualBuilder.cs</c>.
    /// </summary>
    public static class HexMeshFactory
    {
        /// <summary>
        /// Standard pointy-top axial-to-world conversion (see redblobgames' hex grid
        /// reference) — must stay in sync with <see cref="CreateHexPrism"/>'s vertex
        /// angles (offset -30 degrees) for tiles to actually tile seamlessly.
        /// </summary>
        public static Vector3 AxialToWorld(HexCoordinate coordinate, float radius)
        {
            float x = radius * Mathf.Sqrt(3f) * (coordinate.Q + coordinate.R * 0.5f);
            float z = radius * 1.5f * coordinate.R;
            return new Vector3(x, 0f, z);
        }

        /// <summary>A flat-capped hexagonal prism, pointy-top, centered on the origin, extending +/-height/2 in Y.</summary>
        public static Mesh CreateHexPrism(float radius, float height)
        {
            var mesh = new Mesh { name = "HexPrism" };

            Vector3[] rimTop = new Vector3[6];
            Vector3[] rimBottom = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = Mathf.Deg2Rad * (60f * i - 30f);
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                rimTop[i] = new Vector3(x, height * 0.5f, z);
                rimBottom[i] = new Vector3(x, -height * 0.5f, z);
            }

            var vertices = new System.Collections.Generic.List<Vector3>();
            var triangles = new System.Collections.Generic.List<int>();

            // Top cap (fan from a center vertex). Winding order here was verified by
            // hand (cross product of the two fan edges) to face +Y — the previous
            // (center, i, next) order actually faced -Y, culling the top face from
            // above entirely (the tile looked like an open-topped dish/trough with
            // no lid). (center, next, i) is the correct upward-facing order.
            int topCenter = vertices.Count;
            vertices.Add(new Vector3(0f, height * 0.5f, 0f));
            int topRimStart = vertices.Count;
            vertices.AddRange(rimTop);
            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                triangles.Add(topCenter);
                triangles.Add(topRimStart + next);
                triangles.Add(topRimStart + i);
            }

            // Bottom cap — opposite winding from the top so it faces -Y instead.
            int bottomCenter = vertices.Count;
            vertices.Add(new Vector3(0f, -height * 0.5f, 0f));
            int bottomRimStart = vertices.Count;
            vertices.AddRange(rimBottom);
            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                triangles.Add(bottomCenter);
                triangles.Add(bottomRimStart + i);
                triangles.Add(bottomRimStart + next);
            }

            // Side walls — one quad (2 triangles) per edge, own vertices (not shared
            // with the caps) so normals aren't smoothed across the hard edge.
            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                int baseIndex = vertices.Count;
                vertices.Add(rimTop[i]);
                vertices.Add(rimTop[next]);
                vertices.Add(rimBottom[next]);
                vertices.Add(rimBottom[i]);

                triangles.Add(baseIndex + 0);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 0);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 3);
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// A pointy-top cone (apex at +height/2, base rim at -height/2), centered on
        /// the local origin and facing +Y — used to give mountain hexes a real
        /// peaked silhouette instead of just a tall flat-topped cylinder. Procedural,
        /// same "no imported art" convention as <see cref="CreateHexPrism"/>.
        /// </summary>
        public static Mesh CreateConeMesh(float radius, float height, int segments = 16)
        {
            var mesh = new Mesh { name = "Cone" };

            var vertices = new System.Collections.Generic.List<Vector3>();
            var triangles = new System.Collections.Generic.List<int>();

            int apex = vertices.Count;
            vertices.Add(new Vector3(0f, height * 0.5f, 0f)); // apex (top)
            int baseCenter = vertices.Count;
            vertices.Add(new Vector3(0f, -height * 0.5f, 0f)); // base center
            int rimStart = vertices.Count;
            for (int i = 0; i < segments; i++)
            {
                float angle = Mathf.Deg2Rad * (360f * i / segments);
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                vertices.Add(new Vector3(x, -height * 0.5f, z));
            }

            // Side walls: apex -> rim[i] -> rim[i+1], wound to face outward.
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles.Add(apex);
                triangles.Add(rimStart + i);
                triangles.Add(rimStart + next);
            }

            // Solid base cap, wound to face -Y (down).
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles.Add(baseCenter);
                triangles.Add(rimStart + next);
                triangles.Add(rimStart + i);
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
