using System;
using System.Collections.Generic;

namespace VATyakov.Editor
{
    internal sealed class VatLayout
    {
        private const string HeightHint =
            "Lower the fps or the clip length, or split the clips into several VatAssets (transitions work only between clips of one asset).";

        public readonly VatLayoutInfo Info;
        public readonly VatClip[] Clips;

        private VatLayout(VatLayoutInfo info, VatClip[] clips)
        {
            Info = info;
            Clips = clips;
        }

        public static VatLayout ForVertex(int vertexCount, IReadOnlyList<VatClipRequest> requests)
        {
            RequireInput(vertexCount, requests);
            var blocks = VatMath.BlockCount(vertexCount);
            var clips = StackClips(requests, blocks);
            return new VatLayout(VertexInfo(vertexCount, blocks, RowCount(clips)), clips);
        }

        private static void RequireInput(int vertexCount, IReadOnlyList<VatClipRequest> requests)
        {
            if (vertexCount < 1)
            {
                throw new VatBakeException("The mesh has no vertices.");
            }

            if (requests.Count == 0)
            {
                throw new VatBakeException("No clips to bake.");
            }
        }

        private static VatClip[] StackClips(IReadOnlyList<VatClipRequest> requests, int blocks)
        {
            var frames = new int[requests.Count];
            var rows = 0L;
            for (var i = 0; i < frames.Length; i++)
            {
                frames[i] = FrameCount(requests[i]);
                rows += frames[i];
            }

            RequireHeight(blocks, rows, frames.Length);

            var clips = new VatClip[requests.Count];
            var row = 0;
            for (var i = 0; i < clips.Length; i++)
            {
                clips[i] = Clip(requests[i], row, frames[i]);
                row += frames[i];
            }

            return clips;
        }

        private static VatClip Clip(VatClipRequest request, int startRow, int frames)
        {
            var rate = VatTiming.FrameRate(frames, request.Length, request.IsLooping);
            return new VatClip(request.Name, startRow, frames, request.Length, rate, request.IsLooping);
        }

        private static int FrameCount(VatClipRequest request)
        {
            try
            {
                return VatTiming.FrameCount(request.Length, request.Fps, request.IsLooping);
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new VatBakeException($"Clip '{request.Name}': {e.Message}");
            }
        }

        private static void RequireHeight(int blocks, long rows, int clips)
        {
            if (blocks * rows > VatMath.MaxTextureSize)
            {
                var height = FormattableString.Invariant($"{Blocks(blocks)} × {rows} frames of {VatText.Clips(clips)} = {blocks * rows} rows");
                throw new VatBakeException(FormattableString.Invariant($"VAT texture height {height}, the limit is {VatMath.MaxTextureSize}. {HeightHint}"));
            }
        }

        private static string Blocks(int blocks)
        {
            return blocks == 1 ? "1 block" : FormattableString.Invariant($"{blocks} blocks");
        }

        private static int RowCount(VatClip[] clips)
        {
            var last = clips[clips.Length - 1];
            return last.StartRow + last.FrameCount;
        }

        private static VatLayoutInfo VertexInfo(int vertexCount, int blocks, int rows)
        {
            return new VatLayoutInfo(vertexCount, VatMath.TextureWidth(vertexCount), blocks, rows);
        }
    }
}
