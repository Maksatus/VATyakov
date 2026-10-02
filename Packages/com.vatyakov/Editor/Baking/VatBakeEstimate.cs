using System.Collections.Generic;

namespace VATyakov.Editor
{
    internal sealed class VatBakeEstimate
    {
        private static readonly VatBakeEstimate _empty = new(null, null);

        public readonly VatLayout Layout;
        public readonly string Error;

        public bool IsEmpty => Layout == null && Error == null;
        public bool Fits => Error == null;

        private VatBakeEstimate(VatLayout layout, string error)
        {
            Layout = layout;
            Error = error;
        }

        public static VatBakeEstimate For(VatBakeProfile profile)
        {
            return profile.Fps > 0f && TryGetSource(profile, out var vertexCount, out var clips) ? Compute(profile, vertexCount, clips) : _empty;
        }

        private static bool TryGetSource(VatBakeProfile profile, out int vertexCount, out List<VatSourceClip> clips)
        {
            return profile.Kind == VatSourceKind.Alembic
                ? TryGetAlembic(profile, out vertexCount, out clips)
                : TryGetSkinned(profile, out vertexCount, out clips);
        }

        private static bool TryGetSkinned(VatBakeProfile profile, out int vertexCount, out List<VatSourceClip> clips)
        {
            clips = new List<VatSourceClip>();
            foreach (var clip in profile.Clips)
            {
                if (clip != null && clip.length > 0f)
                {
                    clips.Add(new VatSourceClip(clip.name, clip.length));
                }
            }

            var has = profile.Source != null && profile.Source.sharedMesh != null && clips.Count > 0;
            vertexCount = has ? profile.Source.sharedMesh.vertexCount : 0;
            return has;
        }

        private static bool TryGetAlembic(VatBakeProfile profile, out int vertexCount, out List<VatSourceClip> clips)
        {
            var probe = VatAlembic.IsInstalled && profile.Alembic != null ? VatAlembicProbe.For(profile.Alembic) : null;
            var has = probe != null && probe.Problem == null;
            vertexCount = has ? probe.VertexCount : 0;
            clips = has ? new List<VatSourceClip> { probe.Clip } : null;
            return has;
        }

        private static VatBakeEstimate Compute(VatBakeProfile profile, int vertexCount, List<VatSourceClip> clips)
        {
            try
            {
                return new VatBakeEstimate(VatLayout.ForVertex(vertexCount, VatClipRequest.From(clips, profile.Fps, profile.Loop)), null);
            }
            catch (VatBakeException e)
            {
                return new VatBakeEstimate(null, e.Message);
            }
        }
    }
}
