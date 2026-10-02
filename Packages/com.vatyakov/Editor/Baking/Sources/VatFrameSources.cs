namespace VATyakov.Editor
{
    internal static class VatFrameSources
    {
        public static IVatFrameSource Open(VatBakeProfile profile)
        {
            return profile.Kind == VatSourceKind.Alembic
                ? VatAlembic.Open(profile.Alembic)
                : new SkinnedFrameSource(profile.Source, profile.Clips);
        }
    }
}
