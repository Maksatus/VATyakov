using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    public static class VatBaker
    {
        public const string DefaultShaderName = "VATyakov/vat_lit_vertex";
        public const string TriplanarShaderName = "VATyakov/vat_lit_vertex_triplanar";

        public static List<string> Validate(VatBakeProfile profile)
        {
            return VatBakeValidator.Validate(profile);
        }

        public static VatAsset Bake(VatBakeProfile profile, string assetPath = null)
        {
            VatBakeValidator.ThrowIfInvalid(profile);
            var path = VatAssetPath.Resolve(profile, assetPath);
            var result = VatBakePipeline.Run(profile, Path.GetFileNameWithoutExtension(path));
            var clip = VatTemplateMaterial.ShownClip(profile);
            var asset = VatAssetWriter.Write(profile.Asset, path, result, VatSourceHash.Compute(profile));
            VatTemplateMaterial.Apply(profile, asset, path, clip);
            Save(profile, asset);
            VatBakeLog.Baked(asset, result);
            return asset;
        }

        public static GameObject CreatePrefab(VatBakeProfile profile)
        {
            return VatTestPrefab.CreateOrUpdate(profile);
        }

        private static void Save(VatBakeProfile profile, VatAsset asset)
        {
            profile.Asset = asset;
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }
    }
}
