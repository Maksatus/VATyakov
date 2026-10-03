using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatHalfPositionTexels : IVatPositionTexels
    {
        private readonly VatLayoutInfo _info;
        private readonly ushort[] _texels;

        public VatHalfPositionTexels(VatLayoutInfo info)
        {
            _info = info;
            _texels = new ushort[info.Width * info.Height * VatVertexFormat.ChannelCount];
        }

        public Vector3 Write(int element, int row, Vector3 delta)
        {
            var half = new VatHalf3(delta);
            half.WriteTo(_texels, Offset(element, row));
            return half.ToVector3();
        }

        public Texture2D Build(string name)
        {
            return VatTexture.Create(_info, VatVertexFormat.HalfPosition, name, _texels);
        }

        private int Offset(int element, int row)
        {
            var texel = VatMath.Texel(element, _info.Width, _info.TotalRows, row);
            return (texel.y * _info.Width + texel.x) * VatVertexFormat.ChannelCount;
        }
    }
}
