using System.IO;
using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatTemplateMaterial
    {
        public static Shader ResolveShader(VatBakeProfile profile)
        {
            return profile.Shader != null ? profile.Shader : Shader.Find(VatBaker.DefaultShaderName);
        }

        public static string ShownClip(VatAsset asset, Material material)
        {
            if (material == null || asset == null || !asset.TryValidate(out _))
            {
                return null;
            }

            return VatClipLookup.TryFind(asset, material.GetVector(VatShaderIds.Frame), out var clipIndex) ? asset.Clips[clipIndex].Name : null;
        }

        public static void Apply(VatBakeProfile profile, VatAsset asset, string assetPath, string clipName)
        {
            Fit(profile.Material != null ? profile.Material : Create(profile, asset.Mode, assetPath), asset, clipName);
        }

        public static void Fit(Material material, VatAsset asset, string clipName)
        {
            material.enableInstancing = false;
            if (VatTemplateShader.TryFit(material, asset.Mode, out var previous))
            {
                Debug.Log($"VAT '{asset.name}': {ShaderSwitch(material, previous, asset.Mode)}", material);
            }

            asset.ApplyTo(material, Mathf.Max(asset.IndexOf(clipName), 0));
            EditorUtility.SetDirty(material);
        }

        private static Material Create(VatBakeProfile profile, VatMode mode, string assetPath)
        {
            var material = new Material(ResolveShader(profile));
            VatTemplateShader.TryFit(material, mode, out _);
            AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(assetPath, ".mat")));
            profile.Material = material;
            return material;
        }

        private static string ShaderSwitch(Material material, Shader previous, VatMode mode)
        {
            return $"the shader '{previous.name}' of '{material.name}' does not play {mode} assets, the template now uses '{material.shader.name}'.";
        }
    }
}
