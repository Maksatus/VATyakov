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

        // Drift is decided after sampling (§2.2); rows and sizes stay the same.
        public VatLayout WithDrift(bool drift) => new VatLayout(Info.WithDrift(drift), Clips);

        static void RequireInput(int vertexCount, IReadOnlyList<VatClipRequest> requests)
        {
            if (vertexCount < 1)
                throw new VatBakeException("The mesh has no vertices.");
            if (requests.Count == 0)
                throw new VatBakeException("No clips to bake.");
        }

        // §1.1: clips one under another in request order, no padding rows.
        static VatClip[] StackClips(IReadOnlyList<VatClipRequest> requests, int blocks)
        {
            var frames = new int[requests.Count];
            long rows = 0;
            for (int i = 0; i < frames.Length; i++)
                rows += frames[i] = FrameCount(requests[i]);
            RequireHeight(blocks, rows, frames.Length);

            var clips = new VatClip[requests.Count];
            for (int i = 0, row = 0; i < clips.Length; row += frames[i], i++)
                clips[i] = Clip(requests[i], row, frames[i]);
            return clips;
        }

        static VatClip Clip(VatClipRequest request, int startRow, int frames)
        {
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
                throw new VatBakeException($"Clip '{request.Name}': {e.Message}");
            }
        }

        static void RequireHeight(int blocks, long rows, int clips)
        {
            if (blocks * rows > VatMath.MaxTextureSize)
                throw new VatBakeException(
                    $"VAT texture height {Blocks(blocks)} × {rows} frames of {VatText.Clips(clips)} = {blocks * rows} rows, " +
                    $"the limit is {VatMath.MaxTextureSize}. Lower the fps or the clip length, or split the clips into several " +
                    "VatAssets (transitions work only between clips of one asset).");
        }

        static string Blocks(int blocks) => blocks == 1 ? "1 block" : $"{blocks} blocks";

        static int RowCount(VatClip[] clips)
        {
            var last = clips[clips.Length - 1];
            return last.StartRow + last.FrameCount;
        }

        static VatLayoutInfo VertexInfo(int vertexCount, int blocks, int rows) =>
            new VatLayoutInfo(VatMode.Vertex, vertexCount, 1, VatMath.TextureWidth(vertexCount), blocks, rows,
                pivotRow: false, drift: false, VatVertexFormat.Position, VatVertexFormat.Rotation,
                VatVertexFormat.Drift);
    }
}
