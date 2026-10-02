using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace VATyakov.Editor
{
    internal static class VatVertexFormat
    {
        public const GraphicsFormat Position = GraphicsFormat.R16G16B16A16_SFloat;
        public const GraphicsFormat Rotation = GraphicsFormat.R8G8B8A8_UNorm;
        public const GraphicsFormat Drift = GraphicsFormat.R16G16B16A16_SFloat;
        public const int ChannelCount = 4;

        public static readonly VertexAttributeDescriptor[] Attributes =
        {
            new(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
            new(VertexAttribute.Normal, VertexAttributeFormat.Float16, 4, 1),
            new(VertexAttribute.Tangent, VertexAttributeFormat.Float16, 4, 1),
            new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2, 1),
        };

        public static readonly int[] Strides = { Marshal.SizeOf<Vector3>(), Marshal.SizeOf<VatVertexStream1>() };
    }
}
