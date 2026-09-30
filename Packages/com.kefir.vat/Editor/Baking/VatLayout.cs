using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Kefir.Vat.Editor
{
    /// <summary>
    /// Texture and mesh layout of a bake (§1.1, §1.7, §1.9): where every value goes. Encoders fill it,
    /// tests compare built meshes and textures against it.
    /// </summary>
    sealed class VatLayout
    {
        public const GraphicsFormat PositionFormat = GraphicsFormat.R16G16B16A16_SFloat;

        /// <summary>Vertex mode mesh: stream 0 — position (IDVS on Mali), stream 1 — everything else. No TexCoord4.</summary>
        public static readonly VertexAttributeDescriptor[] VertexModeAttributes =
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float16, 4, 1),
            new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float16, 4, 1),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2, 1),
        };

        /// <summary>Byte strides of the Vertex mode streams.</summary>
        public static readonly int[] VertexModeStrides = { 12, 20 };

        public readonly VatLayoutInfo Info;
        public readonly VatClip[] Clips;

        VatLayout(VatLayoutInfo info, VatClip[] clips)
        {
            Info = info;
            Clips = clips;
        }

        public readonly struct ClipRequest
        {
            public readonly string Name;
            public readonly float Length;
            public readonly float Fps;

            public ClipRequest(string name, float length, float fps)
            {
                Name = name;
                Length = length;
                Fps = fps;
            }
        }

        /// <summary>
        /// Vertex mode: element = vertex, rows are frames only, the first clip starts at row 0 (§1.9).
        /// Loop layout (§1.3); one-shot clips arrive in 1.2.
        /// </summary>
        public static VatLayout ForVertex(int vertexCount, IReadOnlyList<ClipRequest> clips)
        {
            if (vertexCount < 1)
                throw new VatBakeException("У меша нет вертексов.");
            if (clips.Count == 0)
                throw new VatBakeException("Нет клипов для бейка.");

            var result = new VatClip[clips.Count];
            long rows = 0;
            for (int i = 0; i < clips.Count; i++)
            {
                var request = clips[i];
                int frames;
                try
                {
                    frames = VatMath.LoopFrameCount(request.Length, request.Fps);
                }
                catch (ArgumentOutOfRangeException e)
                {
                    throw new VatBakeException($"Клип '{request.Name}': {e.Message}");
                }

                if (frames > VatMath.MaxTextureSize || rows + frames > VatMath.MaxTextureSize)
                    throw HeightError(VatMath.BlockCount(vertexCount), rows + frames);

                result[i] = new VatClip(request.Name, (int)rows, frames, request.Length,
                    VatMath.LoopFrameRate(frames, request.Length), loop: true);
                rows += frames;
            }

            int blocks = VatMath.BlockCount(vertexCount);
            int width = VatMath.TextureWidth(vertexCount);
            if ((long)blocks * rows > VatMath.MaxTextureSize)
                throw HeightError(blocks, rows);

            var info = new VatLayoutInfo(VatMode.Vertex, vertexCount, 1, width, blocks, (int)rows,
                pivotRow: false, drift: false, PositionFormat);
            return new VatLayout(info, result);
        }

        /// <summary>Checks a built Vertex mode mesh and position texture against the layout (bake step 1).</summary>
        public void Verify(Mesh mesh, Texture2D position)
        {
            var problems = new List<string>();
            if (!mesh.GetVertexAttributes().SequenceEqual(VertexModeAttributes))
                problems.Add("вертексные атрибуты");
            for (int stream = 0; stream < VertexModeStrides.Length; stream++)
                if (mesh.GetVertexBufferStride(stream) != VertexModeStrides[stream])
                    problems.Add($"stride потока {stream}");
            if (mesh.vertexCount != Info.Elements)
                problems.Add("число вертексов");
            for (int i = 0; i < mesh.subMeshCount; i++)
                if (mesh.GetSubMesh(i).baseVertex != 0)
                    problems.Add($"baseVertex сабмеша {i}");
            if (position.width != Info.Width || position.height != Info.Height)
                problems.Add("размер _VatPosTex");
            if (position.graphicsFormat != PositionFormat || position.mipmapCount != 1)
                problems.Add("формат _VatPosTex");

            if (problems.Count > 0)
                throw new VatBakeException("Собранные данные не совпадают с раскладкой: " + string.Join(", ", problems) + ".");
        }

        static VatBakeException HeightError(int blocks, long rows) => new VatBakeException(
            $"Высота VAT-текстуры {blocks} × {rows} = {blocks * rows} строк, лимит {VatMath.MaxTextureSize}. " +
            "Уменьшите fps или длину клипов либо разнесите клипы по разным VatAsset.");
    }

    /// <summary>A bake that cannot proceed; the message is meant for the user. Existing assets stay untouched.</summary>
    public sealed class VatBakeException : Exception
    {
        public VatBakeException(string message) : base(message)
        {
        }
    }
}
