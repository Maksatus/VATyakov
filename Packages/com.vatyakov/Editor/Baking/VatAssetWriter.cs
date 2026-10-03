using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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
            SetData(asset, result, result.Mesh, textures.Position, textures.Rotation, textures.Bone, sourceHash);
            AssetDatabase.CreateAsset(asset, path);
            foreach (var part in Parts(asset))
            {
                AssetDatabase.AddObjectToAsset(part, asset);
            }

            Finish(asset);
            return asset;
        }

        private static VatAsset Update(VatAsset asset, VatBakeResult result, string sourceHash)
        {
            var mesh = Adopt(asset, asset.Mesh, result.Mesh);
            var position = Adopt(asset, asset.PositionTexture, result.Textures.Position);
            var rotation = Adopt(asset, asset.RotationTexture, result.Textures.Rotation);
            var bone = Adopt(asset, asset.BoneTexture, result.Textures.Bone);
            SetData(asset, result, mesh, position, rotation, bone, sourceHash);
            RemoveStale(asset);
            Finish(asset);
            return asset;
        }

        private static void SetData(VatAsset asset, VatBakeResult result, Mesh mesh, Texture2D position, Texture2D rotation, Texture2D bone, string sourceHash)
        {
            var layout = result.Layout;
            if (result.Mode == VatMode.Bone)
            {
                asset.SetBoneData(layout.Info, mesh, bone, layout.Clips, result.Precision, sourceHash);
                return;
            }

            asset.SetData(layout.Info, mesh, position, rotation, result.PositionFormat, result.PositionRange, result.Drift, layout.Clips, result.Precision,
                sourceHash);
            asset.SetFallback(result.Fallback);
        }

        private static T Adopt<T>(VatAsset asset, T existing, T built) where T : Object
        {
            if (built == null)
            {
                return null;
            }

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
            var parts = Parts(asset);
            foreach (var part in AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(asset)))
            {
                if (Array.IndexOf(parts, part) < 0)
                {
                    AssetDatabase.RemoveObjectFromAsset(part);
                    Object.DestroyImmediate(part, true);
                }
            }
        }

        private static void Finish(VatAsset asset)
        {
            foreach (var part in Parts(asset))
            {
                MakeNonReadable(part);
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }

        private static Object[] Parts(VatAsset asset)
        {
            return asset.Mode == VatMode.Bone
                ? new Object[] { asset.Mesh, asset.BoneTexture }
                : new Object[] { asset.Mesh, asset.PositionTexture, asset.RotationTexture };
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
