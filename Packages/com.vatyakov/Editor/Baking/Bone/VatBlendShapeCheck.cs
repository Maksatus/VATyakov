using System;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatBlendShapeCheck
    {
        public static string Problem(VatBakeCopy copy)
        {
            var problem = Problem(copy.Renderer);
            foreach (var extra in copy.Extras)
            {
                problem ??= extra is SkinnedMeshRenderer skinned ? Problem(skinned) : null;
            }

            return problem;
        }

        private static string Problem(SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh;
            for (var shape = 0; shape < mesh.blendShapeCount; shape++)
            {
                var weight = renderer.GetBlendShapeWeight(shape);
                if (weight != 0f)
                {
                    return FormattableString.Invariant($"blend shape '{mesh.GetBlendShapeName(shape)}' of '{renderer.name}' has weight {weight:0.###}");
                }
            }

            return null;
        }
    }
}
