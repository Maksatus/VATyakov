using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace VATyakov.Editor
{
    // §1.7, §1.9: stream 0 — position only (IDVS on Mali), stream 1 — the rest. No TexCoord4 in Vertex mode.
    static class VatVertexFormat
    {
        public const GraphicsFormat Position = GraphicsFormat.R16G16B16A16_SFloat;

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
