using System;
using UnityEngine;

namespace VATyakov.Tests
{
    internal sealed class VatSyntheticPiece
    {
        public readonly string Name;
        public readonly Func<double, Matrix4x4> Pose;
        public readonly Func<double, bool> IsVisible;

        public VatSyntheticPiece(string name, Func<double, Matrix4x4> pose, Func<double, bool> isVisible = null)
        {
            Name = name;
            Pose = pose;
            IsVisible = isVisible ?? (_ => true);
        }
    }
}
