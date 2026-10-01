using UnityEngine;

namespace VATyakov.Editor
{
    // §1.9: smallest-three, 2 + 10 + 10 + 10 bits in RGBA8. Decoding is VatMath.DecodeRotation, the shader mirror.
    static class VatSmallestThree
    {
        const float HalfSqrt2 = 0.70710678f;

        public static Color32 Encode(Vector4 rotation)
        {
            var q = rotation.normalized;
            int index = LargestIndex(q);
            if (q[index] < 0f)
                q = -q;
            var (a, b, c) = Others(q, index);
            return Pack(Quantize(a), Quantize(b), Quantize(c), index);
        }

        public static Color32 Pack(int a, int b, int c, int index) => new Color32(
            (byte)(a & 255), (byte)(b & 255), (byte)(c & 255), (byte)(index << 6 | (c >> 8) << 4 | (b >> 8) << 2 | a >> 8));

        // Ties go to the lower index, so equal components encode the same way every time.
        static int LargestIndex(Vector4 q)
        {
            int index = 0;
            for (int i = 1; i < 4; i++)
                if (Mathf.Abs(q[i]) > Mathf.Abs(q[index]))
                    index = i;
            return index;
        }

        static (float, float, float) Others(Vector4 q, int index) => index switch
        {
            0 => (q.y, q.z, q.w),
            1 => (q.x, q.z, q.w),
            2 => (q.x, q.y, q.w),
            _ => (q.x, q.y, q.z),
        };

        // |c| ≤ 1/√2 for the three smallest; the clamp only absorbs float rounding at ±1/√2.
        static int Quantize(float c) => Mathf.Clamp(Mathf.RoundToInt((c * HalfSqrt2 + 0.5f) * 1023f), 0, 1023);
    }
}
