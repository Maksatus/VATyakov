using System;
using System.Collections.Generic;

namespace VATyakov.Editor
{
    // SkinnedFrameSource today; AlembicFrameSource (1.4) implements the same contract.
    interface IVatFrameSource : IDisposable
    {
        VatSourceMesh Mesh { get; }

        IReadOnlyList<VatSourceClip> Clips { get; }

        // Time in seconds from the clip start; data in prefab-root space.
        void Sample(int clip, double time, VatFrame frame);
    }
}
