using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Minimal Wavefront OBJ (+ MTL) exporter for a GameObject hierarchy — the
    /// tool behind the "wireframe out, real texture/imagery back in" workflow for
    /// upgrading specific assets beyond this project's original all-procedural
    /// convention (see <see cref="QuadcopterWireframeExporter"/> for the first
    /// use of it). Every <see cref="MeshFilter"/> under the given root is written
    /// out as its own named "o" group (so an artist/texturing tool can address
    /// e.g. the frame, arms, and rotors separately rather than getting one fused
    /// blob), with vertices baked into the root's local space so the exported
    /// file is a clean, position-independent asset regardless of where the
    /// source GameObject happened to sit in a scene when exported.
    /// </summary>
    public static class ObjExporter
    {
        /// <summary>Writes <paramref name="objPath"/> (and a same-named .mtl alongside it) describing every mesh under <paramref name="root"/>.</summary>
        public static void Export(GameObject root, string objPath)
        {
            string mtlPath = Path.ChangeExtension(objPath, ".mtl");
            string mtlFileName = Path.GetFileName(mtlPath);

            var objBuilder = new StringBuilder();
            var mtlBuilder = new StringBuilder();
            var writtenMaterials = new HashSet<string>();

            // Plain ASCII only in the header comment — some external tools'
            // OBJ parsers/binary-file heuristics choke on non-ASCII bytes (e.g. an
            // em-dash) appearing this early in the file.
            objBuilder.AppendLine($"# Exported from Vanquish (Unity) - {root.name}");
            objBuilder.AppendLine($"mtllib {mtlFileName}");

            int vertexOffset = 0;
            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>();

            foreach (MeshFilter meshFilter in meshFilters)
            {
                Mesh mesh = meshFilter.sharedMesh;
                if (mesh == null)
                    continue;

                Vector3[] vertices = mesh.vertices;
                Vector3[] normals = mesh.normals;
                Vector2[] uv = mesh.uv;
                int[] triangles = mesh.triangles;

                // Bake this part's full world transform, then re-express relative to
                // the export root, so the exported file is centered/oriented
                // sensibly regardless of the root's actual scene position.
                Matrix4x4 toRootSpace = root.transform.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;

                string groupName = SanitizeName(meshFilter.gameObject.name) + "_" + vertexOffset;
                objBuilder.AppendLine($"o {groupName}");

                string materialName = GetOrWriteMaterial(meshFilter, mtlBuilder, writtenMaterials);
                if (materialName != null)
                    objBuilder.AppendLine($"usemtl {materialName}");

                foreach (Vector3 vertex in vertices)
                    objBuilder.AppendLine(FormatVector("v", toRootSpace.MultiplyPoint3x4(vertex)));

                bool hasNormals = normals != null && normals.Length == vertices.Length;
                if (hasNormals)
                {
                    foreach (Vector3 normal in normals)
                        objBuilder.AppendLine(FormatVector("vn", toRootSpace.MultiplyVector(normal).normalized));
                }

                bool hasUv = uv != null && uv.Length == vertices.Length;
                if (hasUv)
                {
                    foreach (Vector2 texCoord in uv)
                        objBuilder.AppendLine($"vt {F(texCoord.x)} {F(texCoord.y)}");
                }

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    // OBJ face winding needs reversing to match the X-flip in
                    // FormatVector below (mirroring the mesh once via a coordinate
                    // flip, without also mirroring it a second time via face winding).
                    int a = triangles[i] + vertexOffset + 1;
                    int b = triangles[i + 1] + vertexOffset + 1;
                    int c = triangles[i + 2] + vertexOffset + 1;
                    objBuilder.AppendLine($"f {FaceVertex(a, hasUv, hasNormals)} {FaceVertex(c, hasUv, hasNormals)} {FaceVertex(b, hasUv, hasNormals)}");
                }

                vertexOffset += vertices.Length;
            }

            string directory = Path.GetDirectoryName(objPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(objPath, objBuilder.ToString());
            File.WriteAllText(mtlPath, mtlBuilder.ToString());
        }

        private static string FaceVertex(int index, bool hasUv, bool hasNormals)
        {
            if (hasUv && hasNormals)
                return $"{index}/{index}/{index}";
            if (hasUv)
                return $"{index}/{index}";
            if (hasNormals)
                return $"{index}//{index}";
            return index.ToString(CultureInfo.InvariantCulture);
        }

        // OBJ is right-handed (Y-up); Unity is left-handed (Y-up, Z-forward).
        // Flipping X converts between the two conventions without distorting the mesh.
        private static string FormatVector(string prefix, Vector3 v) => $"{prefix} {F(-v.x)} {F(v.y)} {F(v.z)}";

        private static string F(float value) => value.ToString("0.000000", CultureInfo.InvariantCulture);

        private static string GetOrWriteMaterial(MeshFilter meshFilter, StringBuilder mtlBuilder, HashSet<string> writtenMaterials)
        {
            var renderer = meshFilter.GetComponent<Renderer>();
            Color color = renderer != null && renderer.sharedMaterial != null ? renderer.sharedMaterial.color : Color.gray;
            string materialName = "mat_" + SanitizeName(meshFilter.gameObject.name);

            if (writtenMaterials.Add(materialName))
            {
                mtlBuilder.AppendLine($"newmtl {materialName}");
                mtlBuilder.AppendLine($"Kd {F(color.r)} {F(color.g)} {F(color.b)}");
                mtlBuilder.AppendLine("d 1.0");
                mtlBuilder.AppendLine("illum 2");
                mtlBuilder.AppendLine();
            }

            return materialName;
        }

        private static string SanitizeName(string name)
        {
            var builder = new StringBuilder(name.Length);
            foreach (char c in name)
                builder.Append(char.IsLetterOrDigit(c) ? c : '_');
            return builder.ToString();
        }
    }
}
