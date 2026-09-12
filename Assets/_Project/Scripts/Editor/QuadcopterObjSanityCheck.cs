using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Temporary diagnostic-only tool: loads the exported Quadcopter.obj back
    /// through Unity's own model importer and reports vertex/triangle counts and
    /// bounds, as an independent sanity check of the exported geometry (Unity's
    /// importer is far stricter than many third-party OBJ viewers, so a
    /// successful load here is strong evidence the file itself is well-formed).
    /// Safe to delete once no longer needed.
    /// </summary>
    public static class QuadcopterObjSanityCheck
    {
        [MenuItem("Vanquish/Debug/Check Quadcopter OBJ")]
        public static void Check()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Resources/Models/Quadcopter.obj");
            Mesh[] meshes = assets.OfType<Mesh>().ToArray();
            Debug.Log($"Loaded {assets.Length} sub-assets, {meshes.Length} meshes.");

            var combined = new Bounds();
            bool first = true;
            int totalVerts = 0;
            int totalTris = 0;
            foreach (Mesh mesh in meshes)
            {
                totalVerts += mesh.vertexCount;
                totalTris += mesh.triangles.Length / 3;
                if (first)
                {
                    combined = mesh.bounds;
                    first = false;
                }
                else
                {
                    combined.Encapsulate(mesh.bounds);
                }
            }

            Debug.Log($"Total verts: {totalVerts}, total tris: {totalTris}");
            Debug.Log($"Combined local bounds: center={combined.center}, size={combined.size}");

            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Resources/Models/Quadcopter.obj");
            Debug.Log(root != null
                ? $"Root prefab loaded OK: {root.name}, children={root.transform.childCount}"
                : "FAILED to load root GameObject!");
        }
    }
}
