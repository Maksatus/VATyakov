using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace VATyakov.Tests
{
    internal static class VatTestUtil
    {
        private const int HalfSize = 2;
        private const int HalfTexelSize = 4 * HalfSize;
        private const int ByteTexelSize = 4;
        private const float MinRandomSqrMagnitude = 1e-2f;
        private const float FractionTolerance = 1e-5f;

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
            return Half3(texels, (texel.y * layout.Width + texel.x) * HalfTexelSize);
        }

        public static Vector4 Row(int row)
        {
            return new Vector4(row, row, 0f, 0f);
        }

        public static Vector4 DecodeRotation(byte[] texels, VatLayoutInfo layout, int element, int row)
        {
            var texel = VatMath.Texel(element, layout.Width, layout.TotalRows, row);
            var offset = (texel.y * layout.Width + texel.x) * ByteTexelSize;
            return VatMath.DecodeRotation((Color)new Color32(texels[offset], texels[offset + 1], texels[offset + 2], texels[offset + 3]));
        }

        public static void AssertFrame(Vector4 frame, int startRow, int frame0, int frame1, float fraction)
        {
            Assert.AreEqual(startRow + frame0, frame.x, $"row0 of {frame}");
            Assert.AreEqual(startRow + frame1, frame.y, $"row1 of {frame}");
            Assert.AreEqual(fraction, frame.z, FractionTolerance, $"frac of {frame}");
            Assert.AreEqual(0f, frame.w, $"w of {frame}");
        }

        public static Vector3 NextSignedVector(Random random)
        {
            return new Vector3(NextSigned(random), NextSigned(random), NextSigned(random));
        }

        public static Vector3 RandomDirection(Random random)
        {
            Vector3 direction;
            do
            {
                direction = NextSignedVector(random);
            } while (direction.sqrMagnitude < MinRandomSqrMagnitude || direction.sqrMagnitude > 1f);

            return direction.normalized;
        }

        public static Vector4 RandomRotation(Random random)
        {
            Vector4 rotation;
            do
            {
                rotation = new Vector4(NextSigned(random), NextSigned(random), NextSigned(random), NextSigned(random));
            } while (rotation.sqrMagnitude < MinRandomSqrMagnitude || rotation.sqrMagnitude > 1f);

            return rotation.normalized;
        }

        private static float NextSigned(Random random)
        {
            return (float)(random.NextDouble() * 2.0 - 1.0);
        }

        private static Vector3 Half3(byte[] texels, int offset)
        {
            return new Vector3(Half(texels, offset), Half(texels, offset + HalfSize), Half(texels, offset + 2 * HalfSize));
        }

        private static float Half(byte[] bytes, int offset)
        {
            return Mathf.HalfToFloat(BitConverter.ToUInt16(bytes, offset));
        }
    }
}
