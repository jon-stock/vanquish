using UnityEditor;
using UnityEngine;
using Vanquish.Combat.Play;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Experimental first step in moving specific assets beyond this project's
    /// original all-procedural convention (see AGENTS.md/PLAN.md): builds the
    /// existing procedural quadcopter geometry (<see cref="DroneVisualBuilder"/> —
    /// completely unchanged as the source geometry) and exports it as a plain
    /// Wavefront OBJ/MTL "wireframe" via <see cref="ObjExporter"/>, written
    /// straight into a <c>Resources</c> folder so it's simultaneously (a) the file
    /// to hand to an external tool/artist/AI to generate real textured imagery
    /// from, and (b) the actual live asset <see cref="DroneVisualBuilder"/> now
    /// instantiates in-game instead of the raw primitives, whenever it's present
    /// (see <c>DroneVisualBuilder.TryBuildImportedQuadcopterBody</c>). Replace
    /// this file directly (keeping the same path) once real textured art exists —
    /// no code changes needed for that swap.
    /// </summary>
    public static class QuadcopterWireframeExporter
    {
        private const string ExportPath = "Assets/_Project/Art/Resources/Models/Quadcopter.obj";

        [MenuItem("Vanquish/Export Quadcopter Wireframe (OBJ)")]
        public static void ExportQuadcopter()
        {
            var root = new GameObject("Quadcopter_ExportTemp");
            try
            {
                // The static airframe only (no motors/rotor blades — those are
                // always kept procedural in-game so rotors actually spin; see
                // DroneVisualBuilder.Build's doc comment).
                DroneVisualBuilder.BuildStaticAirframeForExport(root.transform, Color.white, DroneRotorConfiguration.Quadcopter);
                ObjExporter.Export(root, ExportPath);

                AssetDatabase.Refresh();
                Debug.Log($"[QuadcopterWireframeExporter] Exported quadcopter wireframe to {ExportPath} (plus a .mtl alongside it with each part's current placeholder color, as a rough paint-by-numbers reference). This is also the live asset the game now renders for quadcopters.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
