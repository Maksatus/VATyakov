using System;
using UnityEngine;

namespace VATyakov
{
    public static class VatMath
    {
        public const int MaxTextureSize = 4096;

        public const int DriftWidth = 2;

        private const float ByteMax = 255f;
        private const float FieldMax = 1023f;
        private const float MaxComponent = 0.70710678f;
        private const int FieldLowBits = 8;
        private const int HighBitsMask = 3;
        private const int HighBitsAShift = 0;
        private const int HighBitsBShift = 2;
        private const int HighBitsCShift = 4;
        private const int IndexShift = 6;

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

        public static (int A, int B, int C, int Index) RotationFields(Color32 bytes)
        {
            return (RotationField(bytes.r, bytes.a, HighBitsAShift), RotationField(bytes.g, bytes.a, HighBitsBShift),
                RotationField(bytes.b, bytes.a, HighBitsCShift), bytes.a >> IndexShift);
        }

        public static Vector4 DecodeRotation(Vector4 texel)
        {
            var fields = RotationFields(RotationBytes(texel));
            var smallest = new Vector3(Component(fields.A), Component(fields.B), Component(fields.C));
            var largest = Mathf.Sqrt(1f - Vector3.Dot(smallest, smallest));
            return fields.Index == 0 ? new Vector4(largest, smallest.x, smallest.y, smallest.z)
                : fields.Index == 1 ? new Vector4(smallest.x, largest, smallest.y, smallest.z)
                : fields.Index == 2 ? new Vector4(smallest.x, smallest.y, largest, smallest.z)
                : new Vector4(smallest.x, smallest.y, smallest.z, largest);
        }

        public static Vector4 Nlerp(Vector4 q0, Vector4 q1, float t)
        {
            q1 = Vector4.Dot(q0, q1) < 0f ? -q1 : q1;
            return Vector4.Lerp(q0, q1, t).normalized;
        }

        public static Vector3 FrameNormal(Vector4 q)
        {
            return new Vector3(2f * (q.x * q.z + q.w * q.y), 2f * (q.y * q.z - q.w * q.x), 1f - 2f * (q.x * q.x + q.y * q.y));
        }

        public static Vector3 FrameTangent(Vector4 q)
        {
            return new Vector3(1f - 2f * (q.y * q.y + q.z * q.z), 2f * (q.x * q.y + q.w * q.z), 2f * (q.x * q.z - q.w * q.y));
        }

        private static int RotationField(int low, int highBits, int shift)
        {
            return low | (highBits >> shift & HighBitsMask) << FieldLowBits;
        }

        private static float Component(int field)
        {
            return (field * (2f / FieldMax) - 1f) * MaxComponent;
        }

        private static byte Byte(float value)
        {
            return (byte)Mathf.RoundToInt(value * ByteMax);
        }
    }
}
