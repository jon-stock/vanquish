using UnityEditor;
using UnityEngine;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Phase 3B: reserves a named Unity Layer by editing ProjectSettings/
    /// TagManager.asset directly via SerializedObject — the standard (if slightly
    /// obscure) Editor-scripting technique for defining a Layer from code, since
    /// there's no public runtime API to create one. Needed for
    /// MainMenuSceneBuilder... no — for the Workshop's live 3D design preview
    /// (WorkshopPreviewStage): the preview camera must render *only* the preview
    /// model and nothing else in the scene, which in Unity means culling by Layer
    /// (LayerMask), not by distance/tag/name. Idempotent: safe to call every time a
    /// scene builder runs, same convention as EnsureSceneInBuildSettings.
    /// </summary>
    public static class EditorLayerSetup
    {
        /// <summary>
        /// Ensures `layerName` exists at `preferredIndex` (a user layer slot, 8-31).
        /// If `layerName` is already assigned anywhere, leaves it alone. If
        /// `preferredIndex` is already taken by a *different* name, falls back to the
        /// first free user layer slot instead of overwriting someone else's layer.
        /// </summary>
        public static void EnsureLayer(string layerName, int preferredIndex)
        {
            var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets == null || tagManagerAssets.Length == 0)
            {
                Debug.LogError("[EditorLayerSetup] Could not load ProjectSettings/TagManager.asset.");
                return;
            }

            var tagManager = new SerializedObject(tagManagerAssets[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");
            if (layersProp == null || !layersProp.isArray)
            {
                Debug.LogError("[EditorLayerSetup] TagManager.asset has no 'layers' array — Unity version mismatch?");
                return;
            }

            // Already assigned somewhere — nothing to do.
            for (int i = 0; i < layersProp.arraySize; i++)
            {
                if (layersProp.GetArrayElementAtIndex(i).stringValue == layerName)
                    return;
            }

            int targetIndex = preferredIndex;
            if (targetIndex < 8 || targetIndex >= layersProp.arraySize || !string.IsNullOrEmpty(layersProp.GetArrayElementAtIndex(targetIndex).stringValue))
            {
                targetIndex = -1;
                for (int i = 8; i < layersProp.arraySize; i++)
                {
                    if (string.IsNullOrEmpty(layersProp.GetArrayElementAtIndex(i).stringValue))
                    {
                        targetIndex = i;
                        break;
                    }
                }
            }

            if (targetIndex < 0)
            {
                Debug.LogError($"[EditorLayerSetup] No free user layer slot available to assign '{layerName}'.");
                return;
            }

            layersProp.GetArrayElementAtIndex(targetIndex).stringValue = layerName;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"[EditorLayerSetup] Assigned layer '{layerName}' to index {targetIndex}.");
        }
    }
}
