using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatRotationTexels
    {
        private readonly VatLayoutInfo _info;
        private readonly Color32[] _texels;

        public VatRotationTexels(VatLayoutInfo info)
        {
            _info = info;
            _texels = new Color32[info.Width * info.Height];
        }

        public Vector4 Write(int element, int row, Vector4 rotation)
        {
            var bytes = VatRotationCodec.Encode(rotation);
            var texel = VatMath.Texel(element, _info.Width, _info.TotalRows, row);
            _texels[texel.y * _info.Width + texel.x] = bytes;
            return VatMath.DecodeRotation((Color)bytes);
        }

        public Texture2D Build(string name)
        {
            return VatTexture.Create(_info, VatVertexFormat.Rotation, name, _texels);
        }
    }
}
