using UnityEngine;

namespace VATyakov.Editor
{
    // _VatDriftTex, §1.9: RGBAHalf, VatMath.DriftWidth × ΣF without blocks; x = 0 — hi, x = 1 — lo.
    sealed class VatDriftTexels
    {
        readonly int _rows;
        readonly ushort[] _texels;

        public VatDriftTexels(VatLayoutInfo info)
        {
            _rows = info.TotalRows;
            _texels = Buffer(info);
        }

        public float MaxDistance { get; private set; }

        // Returns d as the shader reads it back.
        public Vector3 Write(int row, Vector3 drift)
        {
            var (hi, lo) = VatDriftCodec.Encode(drift);
            hi.WriteTo(_texels, Offset(0, row));
            lo.WriteTo(_texels, Offset(1, row));
            MaxDistance = Mathf.Max(MaxDistance, drift.magnitude);
            return VatDriftCodec.Decode(hi, lo);
        }

        public Texture2D Build(string name) => Create(_rows, name, _texels);

        // Drift off: the shader still adds d, so the texture is there and holds zeros.
        public static Texture2D Zero(VatLayoutInfo info, string name) => Create(info.TotalRows, name, Buffer(info));

        static ushort[] Buffer(VatLayoutInfo info) => new ushort[VatMath.DriftWidth * info.TotalRows * 4];

        static Texture2D Create(int rows, string name, ushort[] texels) =>
            VatTexture.Create(VatMath.DriftWidth, rows, VatVertexFormat.Drift, name, texels);

        static int Offset(int part, int row)
        {
            var texel = VatMath.DriftTexel(part, row);
            return (texel.y * VatMath.DriftWidth + texel.x) * 4;
        }
    }
}
