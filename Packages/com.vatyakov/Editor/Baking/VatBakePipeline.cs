using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    static class VatBakePipeline
    {
        public static VatBakeResult Run(VatBakeProfile profile, string name)
        {
            using var source = VatFrameSources.Open(profile);
            var layout = VatLayout.ForVertex(source.Mesh.VertexCount, VatClipRequest.From(source.Clips, profile.Fps, profile.Loop));
            var encoder = new VertexEncoder(layout, source.Mesh, !profile.NoDrift);
            SampleAll(source, layout, encoder);
            return Build(encoder, name, source.Warnings);
        }

        static void SampleAll(IVatFrameSource source, VatLayout layout, VertexEncoder encoder)
        {
            var frame = new VatFrame(source.Mesh.VertexCount);
            try
            {
                for (int c = 0; c < layout.Clips.Length; c++)
                    SampleClip(source, c, layout.Clips[c], encoder, frame);
            }
            finally
            {
                VatBakeProgress.Clear();
            }
        }

        static void SampleClip(IVatFrameSource source, int index, VatClip clip, VertexEncoder encoder, VatFrame frame)
        {
            for (int k = 0; k < clip.FrameCount; k++)
            {
                VatBakeProgress.Report(clip, k);
                source.Sample(index, clip.FrameTime(k), frame);
                encoder.AddFrame(index, k, frame);
            }
        }

        static VatBakeResult Build(VertexEncoder encoder, string name, IReadOnlyList<string> warnings)
        {
            var layout = encoder.Layout;
            var mesh = encoder.BuildMesh(name + "_Mesh");
            VatBakeTextures textures = null;
            try
            {
                textures = VatBakeTextures.Build(encoder, name);
                VatLayoutVerifier.Verify(layout, mesh, textures);
                return new VatBakeResult(layout, mesh, textures, encoder, warnings);
            }
            catch
            {
                Object.DestroyImmediate(mesh);
                textures?.Destroy();
                throw;
            }
        }
    }
}
