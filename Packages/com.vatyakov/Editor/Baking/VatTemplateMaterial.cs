using System.IO;
using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    static class VatTemplateMaterial
    {
        public static Shader ResolveShader(VatBakeProfile profile) =>
            profile.Shader != null ? profile.Shader : Shader.Find(VatBaker.DefaultShaderName);

        public static void Apply(VatBakeProfile profile, VatAsset asset, string assetPath)
        {
            var material = profile.Material != null ? profile.Material : Create(profile, assetPath);
            material.enableInstancing = false; // §1.6
            asset.ApplyTo(material, asset.DefaultClipIndex);
            EditorUtility.SetDirty(material);
        }

        static Material Create(VatBakeProfile profile, string assetPath)
        {
            var material = new Material(ResolveShader(profile));
            CopyMaps(SourceMaterial(profile), material);
            AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(assetPath, ".mat")));
            profile.Material = material;
            return material;
        }

        static Material SourceMaterial(VatBakeProfile profile)
        {
            if (profile.Kind == VatSourceKind.Skinned)
                return profile.Source.sharedMaterial;
            var renderer = profile.Alembic.GetComponentInChildren<MeshRenderer>(true);
            return renderer != null ? renderer.sharedMaterial : null;
        }

        static void CopyMaps(Material source, Material material)
        {
            if (source == null)
                return;
            if (source.mainTexture != null && material.HasTexture("_BaseMap"))
                material.SetTexture("_BaseMap", source.mainTexture);
            if (source.HasTexture("_BumpMap") && material.HasTexture("_BumpMap"))
                material.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));
        }
    }
}
