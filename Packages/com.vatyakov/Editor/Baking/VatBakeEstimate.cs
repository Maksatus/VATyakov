namespace VATyakov.Editor
{
    // What a bake would produce, without sampling.
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
            profile.Fps > 0f && TryGetSource(profile, out int vertexCount, out var clip) ? Compute(profile, vertexCount, clip) : Empty;

        static bool TryGetSource(VatBakeProfile profile, out int vertexCount, out VatSourceClip clip) =>
            profile.Kind == VatSourceKind.Alembic
                ? TryGetAlembic(profile, out vertexCount, out clip)
                : TryGetSkinned(profile, out vertexCount, out clip);

        static bool TryGetSkinned(VatBakeProfile profile, out int vertexCount, out VatSourceClip clip)
        {
            bool has = profile.Source != null && profile.Source.sharedMesh != null && profile.Clip != null;
            vertexCount = has ? profile.Source.sharedMesh.vertexCount : 0;
            clip = has ? new VatSourceClip(profile.Clip.name, profile.Clip.length) : default;
            return has;
        }

        static bool TryGetAlembic(VatBakeProfile profile, out int vertexCount, out VatSourceClip clip)
        {
            var probe = VatAlembic.IsInstalled && profile.Alembic != null ? VatAlembicProbe.For(profile.Alembic) : null;
            bool has = probe != null && probe.Problem == null;
            vertexCount = has ? probe.VertexCount : 0;
            clip = has ? probe.Clip : default;
            return has;
        }

        static VatBakeEstimate Compute(VatBakeProfile profile, int vertexCount, VatSourceClip clip)
        {
            try
            {
                var request = new VatClipRequest(clip.Name, clip.Length, profile.Fps, profile.Loop);
                return new VatBakeEstimate(VatLayout.ForVertex(vertexCount, new[] { request }), null);
            }
            catch (VatBakeException e)
            {
                return new VatBakeEstimate(null, e.Message);
            }
        }
    }
}
