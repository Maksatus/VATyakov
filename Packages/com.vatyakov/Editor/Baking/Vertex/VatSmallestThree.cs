using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatSmallestThree
    {
        private const float HalfSqrt2 = 0.70710678f;

        public static Color32 Encode(Vector4 rotation)
        {
            var q = rotation.normalized;
            var index = LargestIndex(q);
            if (q[index] < 0f)
            {
                q = -q;
            }

            var (a, b, c) = Others(q, index);
            return Pack(Quantize(a), Quantize(b), Quantize(c), index);
        }

        public static Color32 Pack(int a, int b, int c, int index)
        {
            return new Color32(
                (byte)(a & 255), (byte)(b & 255), (byte)(c & 255), (byte)(index << 6 | (c >> 8) << 4 | (b >> 8) << 2 | a >> 8));
        }

        private static int LargestIndex(Vector4 q)
        {
            var index = 0;
            for (var i = 1; i < 4; i++)
            {
                if (Mathf.Abs(q[i]) > Mathf.Abs(q[index]))
                {
                    index = i;
                }
            }

            return index;
        }

        private static (float, float, float) Others(Vector4 q, int index)
        {
            return index switch
            {
                0 => (q.y, q.z, q.w),
                1 => (q.x, q.z, q.w),
                2 => (q.x, q.y, q.w),
                _ => (q.x, q.y, q.z),
            };
        }

        private static int Quantize(float c)
        {
            return Mathf.Clamp(Mathf.RoundToInt((c * HalfSqrt2 + 0.5f) * 1023f), 0, 1023);
        }
    }
}
