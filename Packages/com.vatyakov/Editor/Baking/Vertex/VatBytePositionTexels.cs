using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatBytePositionTexels : IVatPositionTexels
    {
        private readonly VatLayoutInfo _info;
        private readonly VatPositionRange _range;
        private readonly Color32[] _texels;

        public VatBytePositionTexels(VatLayoutInfo info, VatPositionRange range)
        {
            _info = info;
            _range = range;
            _texels = new Color32[info.Width * info.Height];
        }

        public Vector3 Write(int element, int row, Vector3 delta)
        {
            var texel = VatBytePositions.Encode(delta, _range);
            var position = VatMath.Texel(element, _info.Width, _info.TotalRows, row);
            _texels[position.y * _info.Width + position.x] = texel;
            return VatBytePositions.Decode(texel, _range);
        }

        public Texture2D Build(string name)
        {
            return VatTexture.Create(_info, VatVertexFormat.BytePosition, name, _texels);
        }
    }
}
