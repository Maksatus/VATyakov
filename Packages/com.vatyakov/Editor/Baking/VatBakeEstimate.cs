using System.Collections.Generic;

namespace VATyakov.Editor
{
    internal sealed class VatBakeEstimate
    {
        private static readonly VatBakeEstimate _empty = new(VatMode.Vertex, null, null);

        public readonly VatMode Mode;
        public readonly VatLayout Layout;
        public readonly string Error;

        public bool IsEmpty => Layout == null && Error == null;
        public bool IsWithinLimits => Error == null;

        private VatBakeEstimate(VatMode mode, VatLayout layout, string error)
        {
            Mode = mode;
            Layout = layout;
            Error = error;
        }

        public static VatBakeEstimate For(VatBakeProfile profile)
        {
            return profile.Fps > 0f && TryGetSource(profile, out var elementCount, out var clips) ? Compute(profile, elementCount, clips) : _empty;
        }

        private static bool TryGetSource(VatBakeProfile profile, out int elementCount, out List<VatSourceClip> clips)
        {
            return profile.Kind == VatSourceKind.Alembic
                ? TryGetAlembic(profile, out elementCount, out clips)
                : TryGetSkinned(profile, out elementCount, out clips);
        }

        private static bool TryGetSkinned(VatBakeProfile profile, out int elementCount, out List<VatSourceClip> clips)
        {
            clips = new List<VatSourceClip>();
            foreach (var clip in profile.Clips)
            {
                if (clip != null && clip.length > 0f)
                {
                    clips.Add(new VatSourceClip(clip.name, clip.length));
                }
            }

            var hasSource = profile.Source != null && profile.Source.sharedMesh != null && clips.Count > 0;
            elementCount = hasSource ? ElementCount(profile) : 0;
            return hasSource;
        }

        private static int ElementCount(VatBakeProfile profile)
        {
            return profile.IsBone ? profile.Source.bones.Length : profile.Source.sharedMesh.vertexCount;
        }

        private static bool TryGetAlembic(VatBakeProfile profile, out int elementCount, out List<VatSourceClip> clips)
        {
            var probe = VatAlembic.IsInstalled && profile.Alembic != null ? VatAlembicProbe.For(profile.Alembic) : null;
            var hasSource = probe != null && probe.Problem == null;
            elementCount = !hasSource ? 0 : profile.IsRigid ? VatAlembic.PieceCount(profile.Alembic) : probe.VertexCount;
            clips = hasSource ? new List<VatSourceClip> { probe.Clip } : null;
            return hasSource;
        }

        private static VatBakeEstimate Compute(VatBakeProfile profile, int elementCount, List<VatSourceClip> clips)
        {
            var mode = profile.IsBone ? VatMode.Bone : profile.IsRigid ? VatMode.Rigid : VatMode.Vertex;
            try
            {
                var requests = VatClipRequest.From(clips, profile.Fps, profile.IsLooping);
                return new VatBakeEstimate(mode, BuildLayout(mode, elementCount, requests), null);
            }
            catch (VatBakeException e)
            {
                return new VatBakeEstimate(mode, null, e.Message);
            }
        }

        private static VatLayout BuildLayout(VatMode mode, int elementCount, VatClipRequest[] requests)
        {
            return mode switch
            {
                VatMode.Bone => VatLayout.ForBone(elementCount, requests),
                VatMode.Rigid => VatLayout.ForRigid(elementCount, requests),
                _ => VatLayout.ForVertex(elementCount, requests),
            };
        }
    }
}
