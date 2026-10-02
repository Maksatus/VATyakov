using System;
using System.Collections.Generic;

namespace VATyakov.Editor
{
    internal interface IVatFrameSource : IDisposable
    {
        VatSourceMesh Mesh { get; }

        IReadOnlyList<VatSourceClip> Clips { get; }

        IReadOnlyList<string> Warnings { get; }

        void Sample(int clip, double time, VatFrame frame);
    }
}
