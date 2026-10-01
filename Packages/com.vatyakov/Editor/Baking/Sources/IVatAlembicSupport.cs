using UnityEngine;

namespace VATyakov.Editor
{
    // §4: implemented by VATyakov.Editor.Alembic, which compiles only with com.unity.formats.alembic installed.
    interface IVatAlembicSupport
    {
        // Throws VatBakeException with a message for the user.
        IVatFrameSource Open(GameObject alembic);
    }
}
