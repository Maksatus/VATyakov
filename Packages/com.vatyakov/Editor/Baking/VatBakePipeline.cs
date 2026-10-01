using UnityEngine;

namespace VATyakov.Editor
{
    static class VatBakePipeline
    {
        public static VatBakeResult Run(VatBakeProfile profile, string name)
        {
            using var source = new SkinnedFrameSource(profile.Source, new[] { profile.Clip });
            var layout = VatLayout.ForVertex(source.Mesh.VertexCount, VatClipRequest.From(source.Clips, profile.Fps, profile.Loop));
            var encoder = new VertexEncoder(layout, source.Mesh);
            SampleAll(source, layout, encoder);
            return Build(layout, encoder, name);
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

        static VatBakeResult Build(VatLayout layout, VertexEncoder encoder, string name)
        {
            Mesh mesh = null;
            Texture2D position = null;
            try
            {
                mesh = encoder.BuildMesh(name + "_Mesh");
                position = encoder.BuildPositionTexture(name + "_Pos");
                VatLayoutVerifier.Verify(layout, mesh, position);
                return new VatBakeResult(layout, mesh, position, encoder.Stats);
            }
            catch
            {
                Destroy(mesh);
                Destroy(position);
                throw;
            }
        }

        static void Destroy(Object target)
        {
            if (target != null)
                Object.DestroyImmediate(target);
        }
    }
}
