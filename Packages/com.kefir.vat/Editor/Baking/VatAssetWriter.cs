using System.IO;
using UnityEditor;
using UnityEngine;

namespace Kefir.Vat.Editor
{
    /// <summary>
    /// Writes a VatAsset with its Mesh and Texture2D sub-assets (§1.8). A rebake copies the new data into the existing
    /// sub-asset objects, so fileIDs and references from materials and prefabs survive, even when sizes change.
    /// </summary>
    static class VatAssetWriter
    {
        /// <summary>
        /// Step 1 — building and checking the new data in memory — is the caller's job; until it has passed,
        /// nothing on disk is touched. The built objects are consumed.
        /// </summary>
        public static VatAsset Write(VatAsset existing, string path, VatLayout layout, Mesh mesh, Texture2D position, string sourceHash)
        {
            if (existing == null)
            {
                var asset = ScriptableObject.CreateInstance<VatAsset>();
                asset.name = Path.GetFileNameWithoutExtension(path);
                asset.SetData(layout.Info, mesh, position, layout.Clips, sourceHash);
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.AddObjectToAsset(mesh, asset);
                AssetDatabase.AddObjectToAsset(position, asset);
                Finish(asset, mesh, position);
                return asset;
            }

            // Step 2: copy into the existing sub-assets.
            var targetMesh = Adopt(existing, existing.Mesh, mesh);
            var targetPosition = Adopt(existing, existing.PositionTexture, position);
            existing.SetData(layout.Info, targetMesh, targetPosition, layout.Clips, sourceHash);
            Finish(existing, targetMesh, targetPosition);
            return existing;
        }

        static T Adopt<T>(VatAsset asset, T existing, T built) where T : Object
        {
            if (existing == null)
            {
                AssetDatabase.AddObjectToAsset(built, asset);
                return built;
            }

            built.name = existing.name;
            EditorUtility.CopySerialized(built, existing); // Texture2D.Reinitialize throws on non-readable textures
            Object.DestroyImmediate(built);
            return existing;
        }

        // Step 3: data is not readable in builds, then save.
        static void Finish(VatAsset asset, Mesh mesh, Texture2D position)
        {
            SetReadable(mesh, false);
            SetReadable(position, false);
            EditorUtility.SetDirty(mesh);
            EditorUtility.SetDirty(position);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }

        static void SetReadable(Object target, bool readable)
        {
            using var serialized = new SerializedObject(target);
            serialized.FindProperty("m_IsReadable").boolValue = readable;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
