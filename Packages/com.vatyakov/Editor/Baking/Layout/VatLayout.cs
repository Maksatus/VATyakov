using System;
using System.Collections.Generic;

namespace VATyakov.Editor
{
    // Where every value of a bake goes (§1.1, §1.9). Computed before sampling, so size errors come first.
    sealed class VatLayout
    {
        public readonly VatLayoutInfo Info;
        public readonly VatClip[] Clips;

        VatLayout(VatLayoutInfo info, VatClip[] clips)
        {
            Info = info;
            Clips = clips;
        }

        // Vertex mode: element = vertex, rows are frames only, the first clip starts at row 0.
        public static VatLayout ForVertex(int vertexCount, IReadOnlyList<VatClipRequest> requests)
        {
            RequireInput(vertexCount, requests);
            int blocks = VatMath.BlockCount(vertexCount);
            var clips = StackClips(requests, blocks);
            return new VatLayout(VertexInfo(vertexCount, blocks, RowCount(clips)), clips);
        }

        static void RequireInput(int vertexCount, IReadOnlyList<VatClipRequest> requests)
        {
            if (vertexCount < 1)
                throw new VatBakeException("У меша нет вертексов.");
            if (requests.Count == 0)
                throw new VatBakeException("Нет клипов для бейка.");
        }

        static VatClip[] StackClips(IReadOnlyList<VatClipRequest> requests, int blocks)
        {
            var clips = new VatClip[requests.Count];
            long row = 0;
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i] = Clip(requests[i], (int)row);
                row += clips[i].FrameCount;
                RequireHeight(blocks, row);
            }
            return clips;
        }

        static VatClip Clip(VatClipRequest request, int startRow)
        {
            int frames = FrameCount(request);
            float rate = VatTiming.FrameRate(frames, request.Length, request.Loop);
            return new VatClip(request.Name, startRow, frames, request.Length, rate, request.Loop);
        }

        static int FrameCount(VatClipRequest request)
        {
            try
            {
                return VatTiming.FrameCount(request.Length, request.Fps, request.Loop);
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new VatBakeException($"Клип '{request.Name}': {e.Message}");
            }
        }

        static void RequireHeight(int blocks, long rows)
        {
            if (blocks * rows > VatMath.MaxTextureSize)
                throw new VatBakeException(
                    $"Высота VAT-текстуры {blocks} × {rows} = {blocks * rows} строк, лимит {VatMath.MaxTextureSize}. " +
                    "Уменьшите fps или длину клипов либо разнесите клипы по разным VatAsset.");
        }

        static int RowCount(VatClip[] clips)
        {
            var last = clips[clips.Length - 1];
            return last.StartRow + last.FrameCount;
        }

        static VatLayoutInfo VertexInfo(int vertexCount, int blocks, int rows) =>
            new VatLayoutInfo(VatMode.Vertex, vertexCount, 1, VatMath.TextureWidth(vertexCount), blocks, rows,
                pivotRow: false, drift: false, VatVertexFormat.Position, VatVertexFormat.Rotation);
    }
}
