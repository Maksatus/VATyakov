using System.Linq;

namespace VATyakov.Editor
{
    internal static class VatFrameSources
    {
        public static IVatFrameSource Open(VatBakeProfile profile)
        {
            return profile.Kind == VatSourceKind.Alembic
                ? VatAlembic.Open(profile.Alembic)
                : new VatSkinnedFrameSource(profile.Source, profile.Clips, profile.ExtraRenderers.Select(extra => extra.Renderer).ToArray());
        }
    }
}
