using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace VATyakov.Editor
{
    internal static class VatBoneFormat
    {
        public const GraphicsFormat Texture = GraphicsFormat.R16G16B16A16_SFloat;
        public const int ChannelCount = 4;
        public const int MaxBones = 256;
        public const int PivotRows = VatMath.BonePivotRows;

        public static readonly VertexAttributeDescriptor[] Attributes =
        {
            new(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
            new(VertexAttribute.Normal, VertexAttributeFormat.Float16, 4, 1),
            new(VertexAttribute.Tangent, VertexAttributeFormat.Float16, 4, 1),
            new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2, 1),
            new(VertexAttribute.TexCoord6, VertexAttributeFormat.UNorm8, 4, 0),
        };

        public static readonly int[] Strides = { Marshal.SizeOf<VatBoneStream0>(), Marshal.SizeOf<VatVertexStream1>() };
    }
}
