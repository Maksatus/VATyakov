using System;
using UnityEngine;

namespace VATyakov
{
    public static class VatMath
    {
        public const int MaxTextureSize = 4096;
        public const float RotationUnit = 127f;
        public const float RotationZero = 128f;

        private const float ByteMax = 255f;

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

        public static Vector4 DecodeRotation(Vector4 texel)
        {
            return texel * (ByteMax / RotationUnit) - Vector4.one * (RotationZero / RotationUnit);
        }

        public static Vector3 FrameNormal(Vector4 q)
        {
            return new Vector3(2f * (q.x * q.z + q.w * q.y), 2f * (q.y * q.z - q.w * q.x), q.w * q.w - q.x * q.x - q.y * q.y + q.z * q.z);
        }

        public static Vector3 FrameTangent(Vector4 q)
        {
            return new Vector3(q.w * q.w + q.x * q.x - q.y * q.y - q.z * q.z, 2f * (q.x * q.y + q.w * q.z), 2f * (q.x * q.z - q.w * q.y));
        }
    }
}
