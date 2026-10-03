using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VATyakov.Editor
{
    internal static class VatBonePipeline
    {
        private const string FallbackHint = "Bone mode takes only rotation and uniform scale of bones, so the asset is baked as Vertex.";

        public static VatBakeResult Run(VatSkinnedFrameSource source, IReadOnlyList<VatClipRequest> requests, string name, out string fallback)
        {
            var rig = new VatBoneRig(source.Copy);
            var encoder = new VatBoneEncoder(VatLayout.ForBone(rig.BoneCount, requests), rig, source.Mesh);
            fallback = SampleAll(source, encoder);
            if (fallback == null)
            {
                return Build(encoder, name, source.Warnings);
            }

            source.Rewind();
            return null;
        }

        private static string SampleAll(VatSkinnedFrameSource source, VatBoneEncoder encoder)
        {
            var skin = new Matrix4x4[encoder.Rig.BoneCount];
            try
            {
                for (var clipIndex = 0; clipIndex < encoder.Layout.Clips.Length; clipIndex++)
                {
                    var problem = SampleClip(source, encoder, clipIndex, skin);
                    if (problem != null)
                    {
                        return problem;
                    }
                }

                return null;
            }
            finally
            {
                VatBakeProgress.Clear();
            }
        }

        private static string SampleClip(VatSkinnedFrameSource source, VatBoneEncoder encoder, int clipIndex, Matrix4x4[] skin)
        {
            var clip = encoder.Layout.Clips[clipIndex];
            for (var frame = 0; frame < clip.FrameCount; frame++)
            {
                VatBakeProgress.Report(clip, frame);
                source.Pose(clipIndex, clip.FrameTime(frame));
                encoder.Rig.ReadSkin(source.Copy.Root.transform, skin);
                var problem = VatBlendShapeCheck.Problem(source.Copy.Renderer) ?? encoder.AddFrame(clipIndex, frame, skin);
                if (problem != null)
                {
                    return FormattableString.Invariant($"Clip '{clip.Name}', frame {frame}: {problem}. {FallbackHint}");
                }
            }

            return null;
        }

        private static VatBakeResult Build(VatBoneEncoder encoder, string name, IReadOnlyList<string> warnings)
        {
            var mesh = encoder.BuildMesh($"{name}{VatAssetPath.MeshSuffix}");
            try
            {
                return new VatBakeResult(encoder, mesh, VatBakeTextures.Build(encoder, name), warnings);
            }
            catch
            {
                Object.DestroyImmediate(mesh);
                throw;
            }
        }
    }
}
