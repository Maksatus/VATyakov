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
                AppendAlembic(ref hash, profile);
            }
            else
            {
                AppendSkinned(ref hash, profile);
            }

            hash.Append(profile.Fps);
            hash.Append(profile.IsLooping ? 1 : 0);
            if (!profile.IsRigid)
            {
                hash.Append(profile.MaxPositionError);
            }

            return hash.ToString();
        }

        public static bool IsOutdated(VatBakeProfile profile, VatAsset asset)
        {
            return asset.TryValidate(out _) && VatBakeValidator.Validate(profile).Count == 0 && Compute(profile) != asset.SourceHash;
        }

        private static void AppendAlembic(ref Hash128 hash, VatBakeProfile profile)
        {
            if (profile.IsRigid)
            {
                hash.Append((int)VatMode.Rigid);
            }

            hash.Append(DependencyHash(profile.Alembic));
        }

        private static void AppendSkinned(ref Hash128 hash, VatBakeProfile profile)
        {
            if (profile.IsBone)
            {
                hash.Append((int)VatMode.Bone);
            }

            hash.Append(DependencyHash(profile.Source));
            hash.Append(AnimationUtility.CalculateTransformPath(profile.Source.transform, profile.Source.transform.root));
            hash.Append(profile.Clips.Count);
            foreach (var clip in profile.Clips)
            {
                hash.Append(DependencyHash(clip));
                hash.Append(clip.name);
            }

            if (profile.ExtraRenderers.Count > 0)
            {
                AppendExtraRenderers(ref hash, profile);
            }
        }

        private static void AppendExtraRenderers(ref Hash128 hash, VatBakeProfile profile)
        {
            hash.Append(profile.ExtraRenderers.Count);
            foreach (var extra in profile.ExtraRenderers)
            {
                hash.Append(AnimationUtility.CalculateTransformPath(extra.Renderer.transform, extra.Renderer.transform.root));
                hash.Append(extra.Renderer.GetType().Name);
            }
        }

        private static string DependencyHash(Object target)
        {
            var path = AssetDatabase.GetAssetPath(target);
            return string.IsNullOrEmpty(path) ? $"{ScenePrefix}{target.name}" : AssetDatabase.GetAssetDependencyHash(path).ToString();
        }
    }
}
