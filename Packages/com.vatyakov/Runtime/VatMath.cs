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
    }
}
