using System;
using UnityEditor;
using Object = UnityEngine.Object;

namespace VATyakov.Editor
{
    internal static class VatProfileLookup
    {
        public static VatBakeProfile Find(VatAsset asset)
        {
            foreach (var path in ProfilePaths())
            {
                if (References(path, asset))
                {
                    return AssetDatabase.LoadAssetAtPath<VatBakeProfile>(path);
                }
            }

            return null;
        }

        public static VatBakeProfile Sharing(VatBakeProfile profile, out Object shared)
        {
            var own = AssetDatabase.GetAssetPath(profile);
            foreach (var path in ProfilePaths())
            {
                if (path == own)
                {
                    continue;
                }

                shared = SharedResult(path, profile);
                if (shared != null)
                {
                    return AssetDatabase.LoadAssetAtPath<VatBakeProfile>(path);
                }
            }

            shared = null;
            return null;
        }

        private static string[] ProfilePaths()
        {
            return Array.ConvertAll(AssetDatabase.FindAssets($"t:{nameof(VatBakeProfile)}"), AssetDatabase.GUIDToAssetPath);
        }

        private static Object SharedResult(string path, VatBakeProfile profile)
        {
            if (References(path, profile.Asset))
            {
                return profile.Asset;
            }

            return References(path, profile.Material) ? profile.Material : null;
        }

        private static bool References(string path, Object result)
        {
            if (result == null)
            {
                return false;
            }

            if (AssetDatabase.IsMainAssetAtPathLoaded(path))
            {
                var profile = AssetDatabase.LoadAssetAtPath<VatBakeProfile>(path);
                return profile.Asset == result || profile.Material == result;
            }

            return Array.IndexOf(AssetDatabase.GetDependencies(path, false), AssetDatabase.GetAssetPath(result)) >= 0;
        }
    }
}
