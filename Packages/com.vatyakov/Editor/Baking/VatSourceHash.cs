using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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
            return IsOutdated(profile, asset, VatBakeValidator.Validate(profile).Count > 0);
        }

        public static bool IsOutdated(VatBakeProfile profile, VatAsset asset, bool hasProblems)
        {
            return !hasProblems && asset.TryValidate(out _) && Compute(profile) != asset.SourceHash;
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

            var source = profile.Source;
            hash.Append(DependencyHash(source));
            hash.Append(DependencyHash(source.sharedMesh));
            var animator = VatBakeCopy.AnimatorWithAvatar(source);
            if (animator != null)
            {
                hash.Append(DependencyHash(animator.avatar));
            }

            hash.Append(AnimationUtility.CalculateTransformPath(source.transform, source.transform.root));
            hash.Append(Array.IndexOf(source.transform.root.GetComponentsInChildren<SkinnedMeshRenderer>(true), source));
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
