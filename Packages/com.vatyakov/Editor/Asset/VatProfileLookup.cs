using UnityEditor;

namespace VATyakov.Editor
{
    static class VatProfileLookup
    {
        public static VatBakeProfile Find(VatAsset asset)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(VatBakeProfile)))
            {
                var profile = Load(guid);
                if (profile != null && profile.Asset == asset)
                    return profile;
            }
            return null;
        }

        static VatBakeProfile Load(string guid) => AssetDatabase.LoadAssetAtPath<VatBakeProfile>(AssetDatabase.GUIDToAssetPath(guid));
    }
}
