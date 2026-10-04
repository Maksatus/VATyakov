using System;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatBoneCheck
    {
        public const double MaxResidual = 1e-3;
        public const float MaxError = 5e-4f;

        private const float MillimetersPerMeter = 1000f;

        public static string Problem(VatSimilarity similarity, VatBoneRig rig, int bone)
        {
            var name = rig.Bones[bone].name;
            if (!similarity.IsProper)
            {
                return FormattableString.Invariant($"bone '{name}' mirrors or collapses the mesh (determinant {similarity.Determinant:0.###})");
            }

            var error = SkinError(similarity, rig, bone);
            if (similarity.Residual <= MaxResidual && error <= MaxError)
            {
                return null;
            }

            return FormattableString.Invariant(
                $"bone '{name}' scales non-uniformly (residual {similarity.Residual:0.####}, error {error * MillimetersPerMeter:0.###} mm)");
        }

        private static float SkinError(VatSimilarity similarity, VatBoneRig rig, int bone)
        {
            var pivot = rig.Pivots[bone];
            var error = 0f;
            foreach (var point in rig.BonePoints[bone])
            {
                error = Mathf.Max(error, similarity.Error(point - pivot));
            }

            return error;
        }
    }
}
