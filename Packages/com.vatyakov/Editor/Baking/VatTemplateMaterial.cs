using System.IO;
using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatTemplateMaterial
    {
        private static readonly int _baseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int _bumpMapId = Shader.PropertyToID("_BumpMap");

        public static Shader ResolveShader(VatBakeProfile profile)
        {
            return profile.Shader != null ? profile.Shader : Shader.Find(VatBaker.DefaultShaderName);
        }

        public static string ShownClip(VatBakeProfile profile)
        {
            var asset = profile.Asset;
            if (profile.Material == null || asset == null || !asset.TryValidate(out _))
            {
                return null;
            }

            return VatClipLookup.TryFind(asset, profile.Material.GetVector(VatShaderIds.Frame), out var clipIndex) ? asset.Clips[clipIndex].Name : null;
        }

        public static void Apply(VatBakeProfile profile, VatAsset asset, string assetPath, string clipName)
        {
            var material = profile.Material != null ? profile.Material : Create(profile, assetPath);
            material.enableInstancing = false;
            asset.ApplyTo(material, asset.TryFindClip(clipName, out var clipIndex) ? clipIndex : 0);
            EditorUtility.SetDirty(material);
        }

        private static Material Create(VatBakeProfile profile, string assetPath)
        {
            var material = new Material(ResolveShader(profile));
            CopyMaps(SourceMaterial(profile), material);
            AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(assetPath, ".mat")));
            profile.Material = material;
            return material;
        }

        private static Material SourceMaterial(VatBakeProfile profile)
        {
            if (profile.Kind == VatSourceKind.Skinned)
            {
                return profile.Source.sharedMaterial;
            }

            var renderer = profile.Alembic.GetComponentInChildren<MeshRenderer>(true);
            return renderer != null ? renderer.sharedMaterial : null;
        }

        private static void CopyMaps(Material source, Material material)
        {
            if (source == null)
            {
                return;
            }

            if (source.mainTexture != null && material.HasTexture(_baseMapId))
            {
                material.SetTexture(_baseMapId, source.mainTexture);
            }

            if (source.HasTexture(_bumpMapId) && material.HasTexture(_bumpMapId))
            {
                material.SetTexture(_bumpMapId, source.GetTexture(_bumpMapId));
            }
        }
    }
}
