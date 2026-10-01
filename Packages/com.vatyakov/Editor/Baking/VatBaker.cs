using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    // §4: VatBakeProfile → IVatFrameSource → VatLayout → encoder → VatAssetWriter.
    public static class VatBaker
    {
        public const string DefaultShaderName = "VATyakov/VAT_Lit_Vertex";

        public static List<string> Validate(VatBakeProfile profile) => VatBakeValidator.Validate(profile);

        // Throws VatBakeException for the user; existing assets are untouched then.
        // assetPath is used only when the profile has no asset yet.
        public static VatAsset Bake(VatBakeProfile profile, string assetPath = null)
        {
            VatBakeValidator.ThrowIfInvalid(profile);
            string path = VatAssetPath.Resolve(profile, assetPath);
            var result = VatBakePipeline.Run(profile, Path.GetFileNameWithoutExtension(path));
            var asset = VatAssetWriter.Write(profile.Asset, path, result, VatSourceHash.Compute(profile));
            VatTemplateMaterial.Apply(profile, asset, path);
            Save(profile, asset);
            VatBakeLog.Baked(asset, result);
            return asset;
        }

        public static GameObject CreatePrefab(VatBakeProfile profile) => VatTestPrefab.CreateOrUpdate(profile);

        static void Save(VatBakeProfile profile, VatAsset asset)
        {
            profile.Asset = asset;
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }
    }
}
