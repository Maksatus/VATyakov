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

        public static VatBakeEstimate For(VatBakeProfile profile) => HasInput(profile) ? Compute(profile) : Empty;

        static bool HasInput(VatBakeProfile profile) =>
            profile.Source != null && profile.Source.sharedMesh != null && profile.Clip != null && profile.Fps > 0f;

        static VatBakeEstimate Compute(VatBakeProfile profile)
        {
            try
            {
                return new VatBakeEstimate(VatLayout.ForVertex(profile.Source.sharedMesh.vertexCount, Requests(profile)), null);
            }
            catch (VatBakeException e)
            {
                return new VatBakeEstimate(null, e.Message);
            }
        }

        static VatClipRequest[] Requests(VatBakeProfile profile) =>
            new[] { new VatClipRequest(profile.Clip.name, profile.Clip.length, profile.Fps) };
    }
}
