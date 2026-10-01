using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace VATyakov.Tests
{
    static class VatTestUtil
    {
        // Works for non-readable textures.
        public static byte[] ReadGpu(Texture2D texture)
        {
            var descriptor = new RenderTextureDescriptor(texture.width, texture.height, texture.graphicsFormat, GraphicsFormat.None)
            {
                mipCount = 1,
                autoGenerateMips = false,
            };
            var target = new RenderTexture(descriptor);
            try
            {
                target.Create();
                Graphics.CopyTexture(texture, 0, 0, target, 0, 0);
                var request = AsyncGPUReadback.Request(target, 0);
                request.WaitForCompletion();
                Assert.IsFalse(request.hasError, "GPU readback failed");
                return request.GetData<byte>().ToArray();
            }
            finally
            {
                target.Release();
                Object.DestroyImmediate(target);
            }
        }

        public static Vector3 DecodeOffset(byte[] texels, VatLayoutInfo layout, int element, int row)
        {
            var texel = VatMath.Texel(element, layout.Width, layout.TotalRows, row);
            int offset = (texel.y * layout.Width + texel.x) * 8;
            return new Vector3(Half(texels, offset), Half(texels, offset + 2), Half(texels, offset + 4));
        }

        // _VatDriftTex as the GPU returns it: hi + lo (§1.9).
        public static Vector3 DecodeDrift(byte[] texels, int row) =>
            VatMath.Drift(DriftPart(texels, 0, row), DriftPart(texels, 1, row));

        static Vector3 DriftPart(byte[] texels, int part, int row)
        {
            var texel = VatMath.DriftTexel(part, row);
            int offset = (texel.y * VatMath.DriftWidth + texel.x) * 8;
            return new Vector3(Half(texels, offset), Half(texels, offset + 2), Half(texels, offset + 4));
        }

        // RGBA8 bytes as the GPU returns them; decoded like VatCore.hlsl.
        public static Vector4 DecodeRotation(byte[] texels, VatLayoutInfo layout, int element, int row)
        {
            var texel = VatMath.Texel(element, layout.Width, layout.TotalRows, row);
            int offset = (texel.y * layout.Width + texel.x) * 4;
            return VatMath.DecodeRotation((Color)new Color32(texels[offset], texels[offset + 1], texels[offset + 2], texels[offset + 3]));
        }

        static float Half(byte[] bytes, int offset) => Mathf.HalfToFloat(BitConverter.ToUInt16(bytes, offset));
    }
}
