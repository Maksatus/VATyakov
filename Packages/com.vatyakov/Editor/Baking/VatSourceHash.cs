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
            if (profile.Kind == VatSourceKind.Alembic)
                hash.Append(DependencyHash(profile.Alembic));
            else
                AppendSkinned(ref hash, profile);
            hash.Append(profile.Fps);
            hash.Append(profile.Loop ? 1 : 0);
            hash.Append(profile.NoDrift ? 1 : 0);
            return hash.ToString();
        }

        static void AppendSkinned(ref Hash128 hash, VatBakeProfile profile)
        {
            hash.Append(DependencyHash(profile.Source));
            hash.Append(AnimationUtility.CalculateTransformPath(profile.Source.transform, profile.Source.transform.root));
            hash.Append(profile.Clips.Count);
            foreach (var clip in profile.Clips) // the order matters: it sets the rows
            {
                hash.Append(DependencyHash(clip));
                hash.Append(clip.name);
            }
        }

        static string DependencyHash(Object target)
        {
            string path = AssetDatabase.GetAssetPath(target);
            return string.IsNullOrEmpty(path) ? "scene:" + target.name : AssetDatabase.GetAssetDependencyHash(path).ToString();
        }
    }
}
