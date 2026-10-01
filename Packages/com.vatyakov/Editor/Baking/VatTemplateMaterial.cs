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
            CopyBaseMap(profile, material);
            AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(assetPath, ".mat")));
            profile.Material = material;
            return material;
        }

        static void CopyBaseMap(VatBakeProfile profile, Material material)
        {
            var source = profile.Source.sharedMaterial;
            if (source != null && source.mainTexture != null && material.HasTexture("_BaseMap"))
                material.SetTexture("_BaseMap", source.mainTexture);
        }
    }
}
