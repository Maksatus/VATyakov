using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace VATyakov.Editor
{
    internal static class VatVertexFormat
    {
        public const GraphicsFormat Position = GraphicsFormat.R16G16B16A16_SFloat;
        public const GraphicsFormat Rotation = GraphicsFormat.R8G8B8A8_UNorm;
        public const GraphicsFormat Drift = GraphicsFormat.R16G16B16A16_SFloat;

        public static readonly VertexAttributeDescriptor[] Attributes =
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float16, 4, 1),
            new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float16, 4, 1),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2, 1),
        };

        public static readonly int[] Strides = { 12, 20 };
    }
}
