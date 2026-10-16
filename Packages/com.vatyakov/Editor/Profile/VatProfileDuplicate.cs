using UnityEditor;

namespace VATyakov.Editor
{
    internal static class VatProfileDuplicate
    {
        public static void Detach(VatBakeProfile profile)
        {
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(profile, out var guid, out long _) || profile.OwnerGuid == guid)
            {
                return;
            }

            if (!string.IsNullOrEmpty(profile.OwnerGuid))
            {
                profile.Asset = null;
                profile.Material = null;
            }

            profile.OwnerGuid = guid;
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
        }
    }
}
