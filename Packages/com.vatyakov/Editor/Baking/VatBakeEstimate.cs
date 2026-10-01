using System.Collections.Generic;

namespace VATyakov.Editor
{
    // What a bake would produce, without sampling. Unset or empty clips are skipped: the validator reports them.
    sealed class VatBakeEstimate
    {
        static readonly VatBakeEstimate Empty = new VatBakeEstimate(null, null);

        public readonly VatLayout Layout;
        public readonly string Error;

        VatBakeEstimate(VatLayout layout, string error)
        {
            Layout = layout;
            Error = error;
        }

        public bool IsEmpty => Layout == null && Error == null;

        public bool Fits => Error == null;

        public static VatBakeEstimate For(VatBakeProfile profile) =>
            profile.Fps > 0f && TryGetSource(profile, out int vertexCount, out var clips) ? Compute(profile, vertexCount, clips) : Empty;

        static bool TryGetSource(VatBakeProfile profile, out int vertexCount, out List<VatSourceClip> clips) =>
            profile.Kind == VatSourceKind.Alembic
                ? TryGetAlembic(profile, out vertexCount, out clips)
                : TryGetSkinned(profile, out vertexCount, out clips);

        static bool TryGetSkinned(VatBakeProfile profile, out int vertexCount, out List<VatSourceClip> clips)
        {
            clips = new List<VatSourceClip>();
            foreach (var clip in profile.Clips)
                if (clip != null && clip.length > 0f)
                    clips.Add(new VatSourceClip(clip.name, clip.length));
            bool has = profile.Source != null && profile.Source.sharedMesh != null && clips.Count > 0;
            vertexCount = has ? profile.Source.sharedMesh.vertexCount : 0;
            return has;
        }

        static bool TryGetAlembic(VatBakeProfile profile, out int vertexCount, out List<VatSourceClip> clips)
        {
            var probe = VatAlembic.IsInstalled && profile.Alembic != null ? VatAlembicProbe.For(profile.Alembic) : null;
            bool has = probe != null && probe.Problem == null;
            vertexCount = has ? probe.VertexCount : 0;
            clips = has ? new List<VatSourceClip> { probe.Clip } : null;
            return has;
        }

        static VatBakeEstimate Compute(VatBakeProfile profile, int vertexCount, List<VatSourceClip> clips)
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
