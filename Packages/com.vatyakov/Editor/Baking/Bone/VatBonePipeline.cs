using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VATyakov.Editor
{
    internal static class VatBonePipeline
    {
        public static VatBakeResult Run(VatSkinnedFrameSource source, IReadOnlyList<VatClipRequest> requests, string name, out string problem)
        {
            var rig = new VatBoneRig(source.Copy);
            var encoder = new VatBoneEncoder(VatLayout.ForBone(rig.BoneCount, requests), rig);
            var fps = new VatBoneFpsCheck(rig.BoneCount);
            problem = SampleAll(source, encoder, fps);
            if (problem == null)
            {
                return Build(encoder, name, Warnings(source, fps));
            }

            source.Rewind();
            return null;
        }

        private static string SampleAll(VatSkinnedFrameSource source, VatBoneEncoder encoder, VatBoneFpsCheck fps)
        {
            var skin = new Matrix4x4[encoder.Rig.BoneCount];
            try
            {
                for (var clipIndex = 0; clipIndex < encoder.Layout.Clips.Length; clipIndex++)
                {
                    var problem = SampleClip(source, encoder, clipIndex, skin, fps);
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

        private static string SampleClip(VatSkinnedFrameSource source, VatBoneEncoder encoder, int clipIndex, Matrix4x4[] skin, VatBoneFpsCheck fps)
        {
            var clip = encoder.Layout.Clips[clipIndex];
            var root = source.Copy.Root.transform;
            for (var frame = 0; frame < clip.FrameCount; frame++)
            {
                VatBakeProgress.Report(clip, frame);
                if (frame > 0)
                {
                    fps.SampleBefore(source, encoder.Rig, clipIndex, clip, frame);
                }

                source.Pose(clipIndex, clip.FrameTime(frame));
                encoder.Rig.ReadSkin(root, skin);
                var problem = VatBlendShapeCheck.Problem(source.Copy.Renderer) ?? encoder.AddFrame(clipIndex, frame, skin);
                if (problem != null)
                {
                    return FormattableString.Invariant($"Clip '{clip.Name}', frame {frame}: {problem}.");
                }

                if (frame > 0)
                {
                    fps.Measure(encoder, clip, frame);
                }
            }

            return null;
        }

        private static List<string> Warnings(VatSkinnedFrameSource source, VatBoneFpsCheck fps)
        {
            var warnings = new List<string>(source.Warnings);
            var warning = fps.Warning();
            if (warning != null)
            {
                warnings.Add(warning);
            }

            return warnings;
        }

        private static VatBakeResult Build(VatBoneEncoder encoder, string name, IReadOnlyList<string> warnings)
        {
            var mesh = encoder.BuildMesh(VatAssetPath.MeshName(name));
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
