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

            // Top cap (fan from a center vertex).
            int topCenter = vertices.Count;
            vertices.Add(new Vector3(0f, height * 0.5f, 0f));
            int topRimStart = vertices.Count;
            vertices.AddRange(rimTop);
            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                triangles.Add(topCenter);
                triangles.Add(topRimStart + i);
                triangles.Add(topRimStart + next);
            }

            // Bottom cap (reverse winding so it faces down).
            int bottomCenter = vertices.Count;
            vertices.Add(new Vector3(0f, -height * 0.5f, 0f));
            int bottomRimStart = vertices.Count;
            vertices.AddRange(rimBottom);
            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                triangles.Add(bottomCenter);
                triangles.Add(bottomRimStart + next);
                triangles.Add(bottomRimStart + i);
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
    }
}
