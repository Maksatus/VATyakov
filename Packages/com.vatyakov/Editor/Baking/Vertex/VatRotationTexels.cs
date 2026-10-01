using UnityEngine;

namespace VATyakov.Editor
{
    // _VatRotTex, §1.9: RGBA8 smallest-three of the frame quaternion, same texels as _VatPosTex.
    sealed class VatRotationTexels
    {
        readonly VatLayoutInfo _info;
        readonly Color32[] _texels;

        public VatRotationTexels(VatLayoutInfo info)
        {
            _info = info;
            _texels = new Color32[info.Width * info.Height];
        }

        // Returns q as the shader decodes it.
        public Vector4 Write(int element, int row, Vector4 rotation)
        {
            var bytes = VatSmallestThree.Encode(rotation);
            var texel = VatMath.Texel(element, _info.Width, _info.TotalRows, row);
            _texels[texel.y * _info.Width + texel.x] = bytes;
            return VatMath.DecodeRotation((Color)bytes);
        }

        public Texture2D Build(string name) => VatTexture.Create(_info, VatVertexFormat.Rotation, name, _texels);
    }
}
