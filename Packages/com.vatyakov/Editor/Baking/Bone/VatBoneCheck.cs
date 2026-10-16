using System;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatBoneCheck
    {
        public const float MaxError = 5e-4f;

        private const float MillimetersPerMeter = 1000f;

        public static string Problem(VatSimilarity similarity, VatBoneRig rig, int bone)
        {
            return Problem(similarity, $"bone '{rig.Bones[bone].name}'", rig.BonePoints[bone], rig.Pivots[bone]);
        }

        public static string Problem(VatSimilarity similarity, string element, Vector3[] points, Vector3 pivot)
        {
            if (!similarity.IsProper)
            {
                return FormattableString.Invariant($"{element} mirrors or collapses the mesh (determinant {similarity.Determinant:0.###})");
            }

            var error = SkinError(similarity, points, pivot);
            return error <= MaxError ? null : FormattableString.Invariant($"{element} scales non-uniformly (error {error * MillimetersPerMeter:0.###} mm)");
        }

        private static float SkinError(VatSimilarity similarity, Vector3[] points, Vector3 pivot)
        {
            var error = 0f;
            foreach (var point in points)
            {
                error = Mathf.Max(error, similarity.Error(point - pivot));
            }

            return error;
        }
    }
}
