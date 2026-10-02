using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatDriftTexels
    {
        private const int HighPart = 0;
        private const int LowPart = 1;

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
            var (high, low) = VatDriftCodec.Encode(drift);
            high.WriteTo(_texels, Offset(HighPart, row));
            low.WriteTo(_texels, Offset(LowPart, row));
            MaxDistance = Mathf.Max(MaxDistance, drift.magnitude);
            return VatDriftCodec.Decode(high, low);
        }

        public Texture2D Build(string name)
        {
            return VatTexture.Create(VatMath.DriftWidth, _rows, VatVertexFormat.Drift, name, _texels);
        }

        private static ushort[] Buffer(VatLayoutInfo info)
        {
            return new ushort[VatMath.DriftWidth * info.TotalRows * VatVertexFormat.ChannelCount];
        }

        private static int Offset(int part, int row)
        {
            var texel = VatMath.DriftTexel(part, row);
            return (texel.y * VatMath.DriftWidth + texel.x) * VatVertexFormat.ChannelCount;
        }
    }
}
