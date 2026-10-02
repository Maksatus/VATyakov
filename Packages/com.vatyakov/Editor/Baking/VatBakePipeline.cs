using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatBakePipeline
    {
        public static VatBakeResult Run(VatBakeProfile profile, string name)
        {
            using var source = VatFrameSources.Open(profile);
            var layout = VatLayout.ForVertex(source.Mesh.VertexCount, VatClipRequest.From(source.Clips, profile.Fps, profile.Loop));
            var encoder = new VertexEncoder(layout, source.Mesh);
            SampleAll(source, layout, encoder);
            return Build(encoder, name, source.Warnings);
        }

        private static void SampleAll(IVatFrameSource source, VatLayout layout, VertexEncoder encoder)
        {
            var frame = new VatFrame(source.Mesh.VertexCount);
            try
            {
                for (var c = 0; c < layout.Clips.Length; c++)
                {
                    SampleClip(source, c, layout.Clips[c], encoder, frame);
                }
            }
            finally
            {
                VatBakeProgress.Clear();
            }
        }

        private static void SampleClip(IVatFrameSource source, int index, VatClip clip, VertexEncoder encoder, VatFrame frame)
        {
            for (var k = 0; k < clip.FrameCount; k++)
            {
                VatBakeProgress.Report(clip, k);
                source.Sample(index, clip.FrameTime(k), frame);
                encoder.AddFrame(index, k, frame);
            }
        }

        private static VatBakeResult Build(VertexEncoder encoder, string name, IReadOnlyList<string> warnings)
        {
            var mesh = encoder.BuildMesh(name + "_mesh");
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
