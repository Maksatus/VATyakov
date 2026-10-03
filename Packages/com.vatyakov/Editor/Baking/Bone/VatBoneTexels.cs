using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatBoneTexels
    {
        private static readonly Vector4 _identity = new(0f, 0f, 0f, 1f);

        private readonly VatLayoutInfo _info;
        private readonly ushort[] _texels;

        public VatBoneTexels(VatLayoutInfo info)
        {
            _info = info;
            _texels = new ushort[info.Width * info.Height * VatBoneFormat.ChannelCount];
        }

        public void WritePivot(int bone, Vector3 pivot)
        {
            var texel = bone * VatMath.TexelsPerBone;
            var row = _info.TotalRows - VatBoneFormat.PivotRows;
            Write(texel, row, pivot);
            Write(texel + 1, row, _identity);
        }

        public void WritePose(int bone, int row, Vector4 offsetScale, Vector4 rotation, out Vector4 storedOffsetScale, out Vector4 storedRotation)
        {
            var texel = bone * VatMath.TexelsPerBone;
            storedOffsetScale = Write(texel, row, offsetScale);
            storedRotation = Write(texel + 1, row, rotation);
        }

        public Texture2D Build(string name)
        {
            return VatTexture.Create(_info, VatBoneFormat.Texture, name, _texels);
        }

        private Vector4 Write(int texel, int row, Vector4 value)
        {
            var offset = (row * _info.Width + texel) * VatBoneFormat.ChannelCount;
            var stored = Vector4.zero;
            for (var channel = 0; channel < VatBoneFormat.ChannelCount; channel++)
            {
                _texels[offset + channel] = Mathf.FloatToHalf(value[channel]);
                stored[channel] = Mathf.HalfToFloat(_texels[offset + channel]);
            }

            return stored;
        }
    }
}
