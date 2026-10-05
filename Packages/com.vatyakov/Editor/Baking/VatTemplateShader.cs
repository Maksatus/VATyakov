using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatTemplateShader
    {
        public static bool Fits(Material material, VatMode mode)
        {
            return material.HasTexture(VatShaderIds.DataTexture(mode));
        }

        public static string DefaultName(VatMode mode, bool canBlend)
        {
            if (mode == VatMode.Rigid)
            {
                return VatBaker.RigidShaderName;
            }

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
