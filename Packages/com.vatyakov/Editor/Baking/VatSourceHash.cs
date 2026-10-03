using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatSourceHash
    {
        private const string ScenePrefix = "scene:";

        public static string Compute(VatBakeProfile profile)
        {
            var hash = new Hash128();
            hash.Append(VatAsset.CurrentFormatVersion);
            if (profile.Kind == VatSourceKind.Alembic)
            {
                hash.Append(DependencyHash(profile.Alembic));
            }
            else
            {
                AppendSkinned(ref hash, profile);
            }

            hash.Append(profile.Fps);
            hash.Append(profile.IsLooping ? 1 : 0);
            hash.Append(profile.MaxPositionError);
            return hash.ToString();
        }

        public static bool IsOutdated(VatBakeProfile profile, VatAsset asset)
        {
            return asset.TryValidate(out _) && VatBakeValidator.Validate(profile).Count == 0 && Compute(profile) != asset.SourceHash;
        }

        private static void AppendSkinned(ref Hash128 hash, VatBakeProfile profile)
        {
            hash.Append(DependencyHash(profile.Source));
            hash.Append(AnimationUtility.CalculateTransformPath(profile.Source.transform, profile.Source.transform.root));
            hash.Append(profile.Clips.Count);
            foreach (var clip in profile.Clips)
            {
                hash.Append(DependencyHash(clip));
                hash.Append(clip.name);
            }
        }

        private static string DependencyHash(Object target)
        {
            var path = AssetDatabase.GetAssetPath(target);
            return string.IsNullOrEmpty(path) ? $"{ScenePrefix}{target.name}" : AssetDatabase.GetAssetDependencyHash(path).ToString();
        }
    }
}
