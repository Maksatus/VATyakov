using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatDriftTexels
    {
        private readonly int _rows;
        private readonly ushort[] _texels;

        public float MaxDistance { get; private set; }

        public VatDriftTexels(VatLayoutInfo info)
        {
            _rows = info.TotalRows;
            _texels = Buffer(info);
        }

        public Vector3 Write(int row, Vector3 drift)
        {
            var (hi, lo) = VatDriftCodec.Encode(drift);
            hi.WriteTo(_texels, Offset(0, row));
            lo.WriteTo(_texels, Offset(1, row));
            MaxDistance = Mathf.Max(MaxDistance, drift.magnitude);
            return VatDriftCodec.Decode(hi, lo);
        }

        public Texture2D Build(string name)
        {
            return VatTexture.Create(VatMath.DriftWidth, _rows, VatVertexFormat.Drift, name, _texels);
        }

        private static ushort[] Buffer(VatLayoutInfo info)
        {
            return new ushort[VatMath.DriftWidth * info.TotalRows * 4];
        }

        private static int Offset(int part, int row)
        {
            var texel = VatMath.DriftTexel(part, row);
            return (texel.y * VatMath.DriftWidth + texel.x) * 4;
        }
    }
}
