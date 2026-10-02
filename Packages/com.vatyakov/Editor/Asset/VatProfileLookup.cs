using UnityEditor;

namespace VATyakov.Editor
{
    internal static class VatProfileLookup
    {
        public static VatBakeProfile Find(VatAsset asset)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(VatBakeProfile)))
            {
                var profile = Load(guid);
                if (profile != null && profile.Asset == asset)
                {
                    return profile;
                }
            }

            return null;
        }

        private static VatBakeProfile Load(string guid)
        {
            return AssetDatabase.LoadAssetAtPath<VatBakeProfile>(AssetDatabase.GUIDToAssetPath(guid));
        }
    }
}
