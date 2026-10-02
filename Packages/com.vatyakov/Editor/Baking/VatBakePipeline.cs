using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatBakePipeline
    {
        public static VatBakeResult Run(VatBakeProfile profile, string name)
        {
            using var source = VatFrameSources.Open(profile);
            var layout = VatLayout.ForVertex(source.Mesh.VertexCount, VatClipRequest.From(source.Clips, profile.Fps, profile.IsLooping));
            var encoder = new VatVertexEncoder(layout, source.Mesh);
            SampleAll(source, layout, encoder);
            return Build(encoder, name, source.Warnings);
        }

        private static void SampleAll(IVatFrameSource source, VatLayout layout, VatVertexEncoder encoder)
        {
            var frame = new VatFrame(source.Mesh.VertexCount);
            try
            {
                for (var clipIndex = 0; clipIndex < layout.Clips.Length; clipIndex++)
                {
                    SampleClip(source, clipIndex, layout.Clips[clipIndex], encoder, frame);
                }
            }
            finally
            {
                VatBakeProgress.Clear();
            }
        }

        private static void SampleClip(IVatFrameSource source, int clipIndex, VatClip clip, VatVertexEncoder encoder, VatFrame frame)
        {
            for (var frameIndex = 0; frameIndex < clip.FrameCount; frameIndex++)
            {
                VatBakeProgress.Report(clip, frameIndex);
                source.Sample(clipIndex, clip.FrameTime(frameIndex), frame);
                encoder.AddFrame(clipIndex, frameIndex, frame);
            }
        }

        private static VatBakeResult Build(VatVertexEncoder encoder, string name, IReadOnlyList<string> warnings)
        {
            var mesh = encoder.BuildMesh($"{name}{VatAssetPath.MeshSuffix}");
            try
            {
                return new VatBakeResult(encoder.Layout, mesh, VatBakeTextures.Build(encoder, name), encoder, warnings);
            }
            catch
            {
                Object.DestroyImmediate(mesh);
                throw;
            }
        }
    }
}
