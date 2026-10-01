using System;
using System.Collections.Generic;

namespace VATyakov.Editor
{
    // SkinnedFrameSource and AlembicFrameSource (1.4, VATyakov.Editor.Alembic).
    interface IVatFrameSource : IDisposable
    {
        VatSourceMesh Mesh { get; }

        IReadOnlyList<VatSourceClip> Clips { get; }

        // Found while sampling; the bake log shows them.
        IReadOnlyList<string> Warnings { get; }

        // Time in seconds from the clip start; data in prefab-root space.
        void Sample(int clip, double time, VatFrame frame);
    }
}
