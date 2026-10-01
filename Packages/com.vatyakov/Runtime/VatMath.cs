using System;
using UnityEngine;

namespace VATyakov
{
    // CPU mirror of Shaders/VatCore.hlsl — change both together.
    public static class VatMath
    {
        // Texture size limit (§1.1).
        public const int MaxTextureSize = 4096;

        // §1.1: blocks = ceil(E / 4096).
        public static int BlockCount(int elementCount)
        {
            if (elementCount < 1)
                throw new ArgumentOutOfRangeException(nameof(elementCount), elementCount, "Element count must be positive.");
            return (elementCount + MaxTextureSize - 1) / MaxTextureSize;
        }

        // §1.1: W = ceil(E / blocks), not a power of two.
        public static int TextureWidth(int elementCount)
        {
            int blocks = BlockCount(elementCount);
            return (elementCount + blocks - 1) / blocks;
        }

        // §1.1: b = id / W, x = id − b·W, y = b·totalRows + row.
        public static Vector2Int Texel(int element, int width, int totalRows, int row)
        {
            int block = element / width;
            return new Vector2Int(element - block * width, block * totalRows + row);
        }

        // §1.9: RGBA8 texel (0..1 per channel) → bytes.
        public static Color32 RotationBytes(Vector4 texel) => new Color32(Byte(texel.x), Byte(texel.y), Byte(texel.z), Byte(texel.w));

        // §1.9: smallest-three fields.
        public static (int A, int B, int C, int Index) RotationFields(Color32 b) =>
            (b.r | (b.a & 3) << 8, b.g | (b.a >> 2 & 3) << 8, b.b | (b.a >> 4 & 3) << 8, b.a >> 6);

        // §1.9: unit quaternion (x, y, z, w); the dropped largest component is never negative.
        public static Vector4 DecodeRotation(Vector4 texel)
        {
            var f = RotationFields(RotationBytes(texel));
            var abc = new Vector3(Component(f.A), Component(f.B), Component(f.C));
            float m = Mathf.Sqrt(Mathf.Clamp01(1f - Vector3.Dot(abc, abc)));
            return f.Index == 0 ? new Vector4(m, abc.x, abc.y, abc.z)
                : f.Index == 1 ? new Vector4(abc.x, m, abc.y, abc.z)
                : f.Index == 2 ? new Vector4(abc.x, abc.y, m, abc.z)
                : new Vector4(abc.x, abc.y, abc.z, m);
        }

        // §2.2: sign alignment, then nlerp.
        public static Vector4 Nlerp(Vector4 q0, Vector4 q1, float t)
        {
            q1 = Vector4.Dot(q0, q1) < 0f ? -q1 : q1;
            return Vector4.Lerp(q0, q1, t).normalized;
        }

        // Frame (T, N×T, N): N = rot(q, (0, 0, 1)).
        public static Vector3 FrameNormal(Vector4 q) => new Vector3(
            2f * (q.x * q.z + q.w * q.y), 2f * (q.y * q.z - q.w * q.x), 1f - 2f * (q.x * q.x + q.y * q.y));

        // T = rot(q, (1, 0, 0)).
        public static Vector3 FrameTangent(Vector4 q) => new Vector3(
            1f - 2f * (q.y * q.y + q.z * q.z), 2f * (q.x * q.y + q.w * q.z), 2f * (q.x * q.z - q.w * q.y));

        static float Component(int n) => (n * (2f / 1023f) - 1f) * 0.70710678f;

        static byte Byte(float value) => (byte)Mathf.RoundToInt(value * 255f);
    }
}
