using System.IO;
using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatAssetWriter
    {
        private const string IsReadableProperty = "m_IsReadable";

        public static VatAsset Write(VatAsset existing, string path, VatBakeResult result, string sourceHash)
        {
            return existing == null ? Create(path, result, sourceHash) : Update(existing, result, sourceHash);
        }

        private static VatAsset Create(string path, VatBakeResult result, string sourceHash)
        {
            var asset = ScriptableObject.CreateInstance<VatAsset>();
            asset.name = Path.GetFileNameWithoutExtension(path);
            var textures = result.Textures;
            asset.SetData(result.Layout.Info, result.Mesh, textures.Position, textures.Rotation, result.PositionFormat, result.PositionRange, result.Drift,
                result.Layout.Clips, result.Precision, sourceHash);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.AddObjectToAsset(result.Mesh, asset);
            AssetDatabase.AddObjectToAsset(textures.Position, asset);
            AssetDatabase.AddObjectToAsset(textures.Rotation, asset);
            Finish(asset);
            return asset;
        }

        private static VatAsset Update(VatAsset asset, VatBakeResult result, string sourceHash)
        {
            var mesh = Adopt(asset, asset.Mesh, result.Mesh);
            var position = Adopt(asset, asset.PositionTexture, result.Textures.Position);
            var rotation = Adopt(asset, asset.RotationTexture, result.Textures.Rotation);
            asset.SetData(result.Layout.Info, mesh, position, rotation, result.PositionFormat, result.PositionRange, result.Drift, result.Layout.Clips,
                result.Precision, sourceHash);
            RemoveStale(asset);
            Finish(asset);
            return asset;
        }

        private static T Adopt<T>(VatAsset asset, T existing, T built) where T : Object
        {
            if (existing == null)
            {
                AssetDatabase.AddObjectToAsset(built, asset);
                return built;
            }

            EditorUtility.CopySerialized(built, existing);
            Object.DestroyImmediate(built);
            return existing;
        }

        private static void RemoveStale(VatAsset asset)
        {
            foreach (var part in AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(asset)))
            {
                if (part != asset.Mesh && part != asset.PositionTexture && part != asset.RotationTexture)
                {
                    AssetDatabase.RemoveObjectFromAsset(part);
                    Object.DestroyImmediate(part, true);
                }
            }
        }

        private static void Finish(VatAsset asset)
        {
            MakeNonReadable(asset.Mesh);
            MakeNonReadable(asset.PositionTexture);
            MakeNonReadable(asset.RotationTexture);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }

        private static void MakeNonReadable(Object target)
        {
            using var serialized = new SerializedObject(target);
            serialized.FindProperty(IsReadableProperty).boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
