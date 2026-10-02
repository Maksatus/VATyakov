using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatAlembic
    {
        public const string MissingPackage = "Package com.unity.formats.alembic 2.4.5 or newer is not installed: the Alembic source is unavailable.";

#if VAT_ALEMBIC
        public static bool IsInstalled => true;

        public static IVatFrameSource Open(GameObject alembic)
        {
            return new VatAlembicFrameSource(alembic);
        }
#else
        public static bool IsInstalled => false;

        public static IVatFrameSource Open(GameObject alembic)
        {
            throw new VatBakeException(MissingPackage);
        }
#endif
    }
}
