using System;
using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    static class VatAlembic
    {
        public const string MissingPackage = "Package com.unity.formats.alembic 2.4.5 or newer is not installed: the Alembic source is unavailable.";

        static readonly IVatAlembicSupport Support = Find();

        public static bool IsInstalled => Support != null;

        public static IVatFrameSource Open(GameObject alembic)
        {
            if (Support == null)
                throw new VatBakeException(MissingPackage);
            return Support.Open(alembic);
        }

        static IVatAlembicSupport Find()
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<IVatAlembicSupport>())
                if (!type.IsAbstract)
                    return (IVatAlembicSupport)Activator.CreateInstance(type);
            return null;
        }
    }
}
