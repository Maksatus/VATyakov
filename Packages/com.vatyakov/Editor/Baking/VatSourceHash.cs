using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    // Source and bake settings; the inspector compares it to flag stale bakes (1.10).
    static class VatSourceHash
    {
        public static string Compute(VatBakeProfile profile)
        {
            var hash = new Hash128();
            hash.Append(VatAsset.CurrentFormatVersion);
            hash.Append(DependencyHash(profile.Source));
            hash.Append(AnimationUtility.CalculateTransformPath(profile.Source.transform, profile.Source.transform.root));
            hash.Append(DependencyHash(profile.Clip));
            hash.Append(profile.Clip.name);
            hash.Append(profile.Fps);
            hash.Append(profile.Loop ? 1 : 0);
            return hash.ToString();
        }

        static string DependencyHash(Object target)
        {
            string path = AssetDatabase.GetAssetPath(target);
            return string.IsNullOrEmpty(path) ? "scene:" + target.name : AssetDatabase.GetAssetDependencyHash(path).ToString();
        }
    }
}
