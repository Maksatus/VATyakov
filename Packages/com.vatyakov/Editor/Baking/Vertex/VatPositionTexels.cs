using System;
using UnityEngine;

namespace VATyakov.Editor
{
    // _VatPosTex, §1.9: RGBAHalf (Δ.x, Δ.y, Δ.z, 0).
    sealed class VatPositionTexels
    {
        readonly VatLayoutInfo _info;
        readonly ushort[] _texels;
        readonly bool[] _rowWritten;

        public VatPositionTexels(VatLayoutInfo info)
        {
            _info = info;
            _texels = new ushort[info.Width * info.Height * 4];
            _rowWritten = new bool[info.TotalRows];
        }

        // Returns Δ as the shader reads it back.
        public Vector3 Write(int element, int row, Vector3 delta)
        {
            var half = new VatHalf3(delta);
            half.WriteTo(_texels, Offset(element, row));
            return half.ToVector3();
        }

        public void MarkRow(int row) => _rowWritten[row] = true;

        public void RequireComplete()
        {
            int missing = Array.IndexOf(_rowWritten, false);
            if (missing >= 0)
                throw new InvalidOperationException($"Row {missing} of the position texture was never written.");
        }

        public Texture2D Build(string name)
        {
            RequireComplete();
            return VatTexture.Create(_info, VatVertexFormat.Position, name, _texels);
        }

        int Offset(int element, int row)
        {
            var texel = VatMath.Texel(element, _info.Width, _info.TotalRows, row);
            return (texel.y * _info.Width + texel.x) * 4;
        }
    }
}
