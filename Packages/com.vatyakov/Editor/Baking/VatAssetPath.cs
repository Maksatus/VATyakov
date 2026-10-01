using System.IO;
using UnityEditor;

namespace VATyakov.Editor
{
    static class VatAssetPath
    {
        public static string Resolve(VatBakeProfile profile, string requested)
        {
            if (profile.Asset != null)
                return AssetDatabase.GetAssetPath(profile.Asset);
            return requested ?? NextToProfile(profile);
        }

        static string NextToProfile(VatBakeProfile profile)
        {
            string profilePath = AssetDatabase.GetAssetPath(profile);
            if (string.IsNullOrEmpty(profilePath))
                throw new VatBakeException("The profile is not saved as an asset: pass a path for the VatAsset.");
            string folder = Path.GetDirectoryName(profilePath)?.Replace('\\', '/');
            return AssetDatabase.GenerateUniqueAssetPath($"{folder}/{profile.name}_Vat.asset");
        }
    }
}
