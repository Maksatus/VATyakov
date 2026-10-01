using UnityEngine;

namespace VATyakov.Editor
{
    // Found by VatAlembic through TypeCache.
    sealed class VatAlembicSupport : IVatAlembicSupport
    {
        public IVatFrameSource Open(GameObject alembic) => new AlembicFrameSource(alembic);
    }
}
