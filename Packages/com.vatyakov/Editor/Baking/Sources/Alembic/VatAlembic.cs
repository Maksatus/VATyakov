using System;
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

        public static IVatRigidSource OpenRigid(GameObject alembic)
        {
            return new VatRigidPieceExtractor(alembic);
        }
#else
        public static bool IsInstalled => false;

        public static IVatFrameSource Open(GameObject alembic)
        {
            throw new VatBakeException(MissingPackage);
        }

        public static IVatRigidSource OpenRigid(GameObject alembic)
        {
            throw new VatBakeException(MissingPackage);
        }
#endif

        public static int PieceCount(GameObject alembic)
        {
            return Array.FindAll(alembic.GetComponentsInChildren<MeshFilter>(true), node => node.sharedMesh != null).Length;
        }
    }
}
