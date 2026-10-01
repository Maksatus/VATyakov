namespace VATyakov.Editor
{
    static class VatFrameSources
    {
        public static IVatFrameSource Open(VatBakeProfile profile) => profile.Kind == VatSourceKind.Alembic
            ? VatAlembic.Open(profile.Alembic)
            : new SkinnedFrameSource(profile.Source, profile.Clips);
    }
}
