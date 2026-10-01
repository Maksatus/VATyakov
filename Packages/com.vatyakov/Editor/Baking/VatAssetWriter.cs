using System.IO;
using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    // §1.8. A rebake copies into the existing sub-asset objects, so fileIDs and references survive size changes.
    static class VatAssetWriter
    {
        public static VatAsset Write(VatAsset existing, string path, VatBakeResult result, string sourceHash) =>
            existing == null ? Create(path, result, sourceHash) : Update(existing, result, sourceHash);

        static VatAsset Create(string path, VatBakeResult result, string sourceHash)
        {
            var asset = ScriptableObject.CreateInstance<VatAsset>();
            asset.name = Path.GetFileNameWithoutExtension(path);
            asset.SetData(result.Layout.Info, result.Mesh, result.Position, result.Layout.Clips, sourceHash);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.AddObjectToAsset(result.Mesh, asset);
            AssetDatabase.AddObjectToAsset(result.Position, asset);
            Finish(asset);
            return asset;
        }

        static VatAsset Update(VatAsset asset, VatBakeResult result, string sourceHash)
        {
            var mesh = Adopt(asset, asset.Mesh, result.Mesh);
            var position = Adopt(asset, asset.PositionTexture, result.Position);
            asset.SetData(result.Layout.Info, mesh, position, result.Layout.Clips, sourceHash);
            Finish(asset);
            return asset;
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

        static void Finish(VatAsset asset)
        {
            MakeNonReadable(asset.Mesh);
            MakeNonReadable(asset.PositionTexture);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }

        static void MakeNonReadable(Object target)
        {
            using var serialized = new SerializedObject(target);
            serialized.FindProperty("m_IsReadable").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
