using System;
using System.Collections.Generic;

namespace VATyakov.Editor
{
    internal interface IVatRigidSource : IDisposable
    {
        VatSourceClip Clip { get; }
        int PieceCount { get; }
        IReadOnlyList<double> SampleTimes { get; }
        IReadOnlyList<string> Warnings { get; }

        VatRigidTrack[] Extract(VatClip clip, VatRigidInnerTimes inner);
    }
}
