using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatBakePipeline
    {
        private const string FallbackHint = "Bone mode takes only rotation and uniform scale of bones, so the asset is baked as Vertex.";
        private const string ExtraRenderersHint =
            "Bone mode takes only rotation and uniform scale of bones, and Extra Renderers bake only in Bone mode: fix the clip or remove the Extra Renderers.";

        public static VatBakeResult Run(VatBakeProfile profile, string name)
        {
            if (profile.IsRigid)
            {
                using var rigid = VatAlembic.OpenRigid(profile.Alembic);
                return VatRigidPipeline.Run(rigid, profile.Fps, profile.IsLooping, name);
            }

            using var source = VatFrameSources.Open(profile);
            var requests = VatClipRequest.From(source.Clips, profile.Fps, profile.IsLooping);
            string fallback = null;
            if (profile.IsBone && source is VatSkinnedFrameSource skinned)
            {
                var bone = VatBonePipeline.Run(skinned, requests, name, out var problem);
                if (bone != null)
                {
                    return bone;
                }

                if (profile.ExtraRenderers.Count > 0)
                {
                    throw new VatBakeException($"{problem} {ExtraRenderersHint}");
                }

                fallback = $"{problem} {FallbackHint}";
            }

            return RunVertex(source, requests, profile.MaxPositionError, name, fallback);
        }

        private static VatBakeResult RunVertex(IVatFrameSource source, VatClipRequest[] requests, float maxPositionError, string name, string fallback)
        {
            var layout = VatLayout.ForVertex(source.Mesh.VertexCount, requests);
            var encoder = new VatVertexEncoder(layout, source.Mesh, maxPositionError);
            SampleAll(source, layout, encoder);
            return Build(encoder, name, source.Warnings, fallback);
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

        private static VatBakeResult Build(VatVertexEncoder encoder, string name, IReadOnlyList<string> warnings, string fallback)
        {
            var mesh = encoder.BuildMesh(VatAssetPath.MeshName(name));
            try
            {
                return new VatBakeResult(encoder, mesh, VatBakeTextures.Build(encoder, name), warnings, fallback);
            }
            catch
            {
                Object.DestroyImmediate(mesh);
                throw;
            }
        }
    }
}
