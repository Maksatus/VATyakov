using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatTemplateShader
    {
        public static bool Fits(Material material, VatMode mode)
        {
            return material.HasTexture(mode == VatMode.Bone ? VatShaderIds.BoneTexture : VatShaderIds.PositionTexture);
        }

        public static string DefaultName(VatMode mode, bool canBlend)
        {
            if (mode == VatMode.Bone)
            {
                return canBlend ? VatBaker.BoneBlendShaderName : VatBaker.BoneShaderName;
            }

            return canBlend ? VatBaker.BlendShaderName : VatBaker.DefaultShaderName;
        }

        public static bool TryFit(Material material, VatMode mode, out Shader previous)
        {
            previous = material.shader;
            if (Fits(material, mode))
            {
                return false;
            }

            material.shader = Shader.Find(DefaultName(mode, material.HasVector(VatShaderIds.FrameB)));
            return true;
        }
    }
}
