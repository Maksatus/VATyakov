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
            asset.ApplyTo(material, 0);
            EditorUtility.SetDirty(material);
        }

        static Material Create(VatBakeProfile profile, string assetPath)
        {
            var material = new Material(ResolveShader(profile));
            CopyMaps(profile.Source.sharedMaterial, material);
            AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(assetPath, ".mat")));
            profile.Material = material;
            return material;
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
