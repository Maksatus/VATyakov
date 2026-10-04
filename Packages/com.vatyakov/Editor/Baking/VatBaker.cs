using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace VATyakov.Editor
{
    public static class VatBaker
    {
        public const string DefaultShaderName = "VATyakov/vat_lit_vertex";
        public const string TriplanarShaderName = "VATyakov/vat_lit_vertex_triplanar";
        public const string BlendShaderName = "VATyakov/vat_lit_vertex_blend";
        public const string BoneShaderName = "VATyakov/vat_lit_bone";
        public const string BoneBlendShaderName = "VATyakov/vat_lit_bone_blend";

        public static List<string> Validate(VatBakeProfile profile)
        {
            return VatBakeValidator.Validate(profile);
        }

        public static VatAsset Bake(VatBakeProfile profile, string assetPath = null)
        {
            VatBakeValidator.ThrowIfInvalid(profile);
            var path = VatAssetPath.Resolve(profile, assetPath);
            var result = VatBakePipeline.Run(profile, Path.GetFileNameWithoutExtension(path));
            var clip = VatTemplateMaterial.ShownClip(profile.Asset, profile.Material);
            var extraClips = VatExtraMaterials.ShownClips(profile);
            var asset = VatAssetWriter.Write(profile.Asset, path, result, VatSourceHash.Compute(profile));
            VatTemplateMaterial.Apply(profile, asset, path, clip);
            VatExtraMaterials.Apply(profile, asset, extraClips);
            Save(profile, asset);
            VatBakeLog.Baked(asset, result);
            return asset;
        }

        private static void Save(VatBakeProfile profile, VatAsset asset)
        {
            profile.Asset = asset;
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }
    }
}
