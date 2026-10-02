using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatSmallestThree
    {
        private const float HalfSqrt2 = 0.70710678f;
        private const float Center = 0.5f;
        private const int ComponentCount = 4;
        private const int MaxQuantized = 1023;
        private const int LowByteBits = 8;
        private const int LowByteMask = 255;
        private const int IndexShift = 6;
        private const int ThirdHighShift = 4;
        private const int SecondHighShift = 2;

        public static Color32 Encode(Vector4 rotation)
        {
            var unit = rotation.normalized;
            var index = LargestIndex(unit);
            if (unit[index] < 0f)
            {
                unit = -unit;
            }

            var (first, second, third) = Others(unit, index);
            return Pack(Quantize(first), Quantize(second), Quantize(third), index);
        }

        public static Color32 Pack(int first, int second, int third, int index)
        {
            var high = index << IndexShift | (third >> LowByteBits) << ThirdHighShift | (second >> LowByteBits) << SecondHighShift | first >> LowByteBits;
            return new Color32(LowByte(first), LowByte(second), LowByte(third), (byte)high);
        }

        private static byte LowByte(int value)
        {
            return (byte)(value & LowByteMask);
        }

        private static int LargestIndex(Vector4 rotation)
        {
            var index = 0;
            for (var i = 1; i < ComponentCount; i++)
            {
                if (Mathf.Abs(rotation[i]) > Mathf.Abs(rotation[index]))
                {
                    index = i;
                }
            }

            return index;
        }

        private static (float, float, float) Others(Vector4 rotation, int index)
        {
            return index switch
            {
                0 => (rotation.y, rotation.z, rotation.w),
                1 => (rotation.x, rotation.z, rotation.w),
                2 => (rotation.x, rotation.y, rotation.w),
                _ => (rotation.x, rotation.y, rotation.z),
            };
        }

        private static int Quantize(float component)
        {
            return Mathf.Clamp(Mathf.RoundToInt((component * HalfSqrt2 + Center) * MaxQuantized), 0, MaxQuantized);
        }
    }
}
