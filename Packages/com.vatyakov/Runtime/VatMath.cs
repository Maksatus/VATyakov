using System;
using UnityEngine;

namespace VATyakov
{
    public static class VatMath
    {
        public const int MaxTextureSize = 4096;

        public const int DriftWidth = 2;

        public static int BlockCount(int elementCount)
        {
            if (elementCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(elementCount), elementCount, "Element count must be positive.");
            }

            return (elementCount + MaxTextureSize - 1) / MaxTextureSize;
        }

        public static int TextureWidth(int elementCount)
        {
            var blocks = BlockCount(elementCount);
            return (elementCount + blocks - 1) / blocks;
        }

        public static Vector2Int Texel(int element, int width, int totalRows, int row)
        {
            var block = element / width;
            return new Vector2Int(element - block * width, block * totalRows + row);
        }

        public static Vector2Int DriftTexel(int part, int row)
        {
            return new Vector2Int(part, row);
        }

        public static Vector3 Drift(Vector3 hi, Vector3 lo)
        {
            return hi + lo;
        }

        public static Color32 RotationBytes(Vector4 texel)
        {
            return new Color32(Byte(texel.x), Byte(texel.y), Byte(texel.z), Byte(texel.w));
        }

        public static (int A, int B, int C, int Index) RotationFields(Color32 b)
        {
            return (b.r | (b.a & 3) << 8, b.g | (b.a >> 2 & 3) << 8, b.b | (b.a >> 4 & 3) << 8, b.a >> 6);
        }

        public static Vector4 DecodeRotation(Vector4 texel)
        {
            var f = RotationFields(RotationBytes(texel));
            var abc = new Vector3(Component(f.A), Component(f.B), Component(f.C));
            var m = Mathf.Sqrt(Mathf.Clamp01(1f - Vector3.Dot(abc, abc)));
            return f.Index == 0 ? new Vector4(m, abc.x, abc.y, abc.z)
                : f.Index == 1 ? new Vector4(abc.x, m, abc.y, abc.z)
                : f.Index == 2 ? new Vector4(abc.x, abc.y, m, abc.z)
                : new Vector4(abc.x, abc.y, abc.z, m);
        }

        public static Vector4 Nlerp(Vector4 q0, Vector4 q1, float t)
        {
            q1 = Vector4.Dot(q0, q1) < 0f ? -q1 : q1;
            return Vector4.Lerp(q0, q1, t).normalized;
        }

        public static Vector3 FrameNormal(Vector4 q)
        {
            return new Vector3(
                2f * (q.x * q.z + q.w * q.y), 2f * (q.y * q.z - q.w * q.x), 1f - 2f * (q.x * q.x + q.y * q.y));
        }

        public static Vector3 FrameTangent(Vector4 q)
        {
            return new Vector3(
                1f - 2f * (q.y * q.y + q.z * q.z), 2f * (q.x * q.y + q.w * q.z), 2f * (q.x * q.z - q.w * q.y));
        }

        private static float Component(int n)
        {
            return (n * (2f / 1023f) - 1f) * 0.70710678f;
        }

        private static byte Byte(float value)
        {
            return (byte)Mathf.RoundToInt(value * 255f);
        }
    }
}
