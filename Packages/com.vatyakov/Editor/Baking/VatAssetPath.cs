using System.IO;
using UnityEditor;

namespace VATyakov.Editor
{
    internal static class VatAssetPath
    {
        public const string AssetSuffix = "_vat";
        public const string MeshSuffix = "_mesh";
        public const string PositionSuffix = "_pos";
        public const string RotationSuffix = "_rot";
        public const string BoneSuffix = "_bone";
        public const string PieceSuffix = "_piece";

        public static string Resolve(VatBakeProfile profile, string requested)
        {
            if (profile.Asset != null)
            {
                return AssetDatabase.GetAssetPath(profile.Asset);
            }

            return requested ?? NextToProfile(profile);
        }

        public static string MeshName(string assetName)
        {
            return $"{assetName}{MeshSuffix}";
        }

        public static string ExtraMeshName(string assetName, string rendererName)
        {
            return $"{MeshName(assetName)}_{rendererName.Replace(' ', '_').ToLowerInvariant()}";
        }

        private static string NextToProfile(VatBakeProfile profile)
        {
            var profilePath = AssetDatabase.GetAssetPath(profile);
            if (string.IsNullOrEmpty(profilePath))
            {
                throw new VatBakeException("The profile is not saved as an asset: pass a path for the VatAsset.");
            }

            var folder = Path.GetDirectoryName(profilePath)?.Replace('\\', '/');
            return AssetDatabase.GenerateUniqueAssetPath($"{folder}/{profile.name}{AssetSuffix}.asset");
        }
    }
}
