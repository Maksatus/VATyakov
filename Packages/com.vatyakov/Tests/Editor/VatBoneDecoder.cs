using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VATyakov.Tests
{
    internal sealed class VatBoneDecoder
    {
        private const int ChannelCount = 4;
        private const int BoneChannel = 6;

        private readonly ushort[] _texels;
        private readonly int _width;
        private readonly int _pivotRow;

        public VatBoneDecoder(Texture2D texture)
        {
            _texels = texture.GetPixelData<ushort>(0).ToArray();
            _width = texture.width;
            _pivotRow = texture.height - VatMath.BonePivotRows;
        }

        public static Vector4[] BoneUvs(Mesh mesh)
        {
            var uv = new List<Vector4>();
            mesh.GetUVs(BoneChannel, uv);
            return uv.ToArray();
        }

        public static Vector3Int Influence(Vector4 boneUv)
        {
            var weight = Mathf.RoundToInt(VatMath.BoneWeight(boneUv.z, boneUv.w) * VatMath.BoneWeightMax);
            return new Vector3Int(VatMath.BoneIndex(boneUv.x), VatMath.BoneIndex(boneUv.y), weight);
        }

        public Vector3 Position(Vector4 boneUv, Vector3 rest, Vector4 frame)
        {
            var weight = VatMath.BoneWeight(boneUv.z, boneUv.w);
            return Vector3.LerpUnclamped(Position(VatMath.BoneIndex(boneUv.y), rest, frame), Position(VatMath.BoneIndex(boneUv.x), rest, frame), weight);
        }

        public Vector3 Direction(Vector4 boneUv, Vector3 direction, Vector4 frame)
        {
            var weight = VatMath.BoneWeight(boneUv.z, boneUv.w);
            var direction1 = Direction(VatMath.BoneIndex(boneUv.y), direction, frame);
            return Vector3.LerpUnclamped(direction1, Direction(VatMath.BoneIndex(boneUv.x), direction, frame), weight).normalized;
        }

        private Vector3 Position(int bone, Vector3 rest, Vector4 frame)
        {
            var texel = bone * VatMath.TexelsPerBone;
            return VatMath.BonePoint(Lerp(texel, frame), Lerp(texel + 1, frame), Texel(texel, _pivotRow), rest);
        }

        private Vector3 Direction(int bone, Vector3 direction, Vector4 frame)
        {
            return VatMath.BoneDirection(Lerp(bone * VatMath.TexelsPerBone + 1, frame), direction);
        }

        private Vector4 Lerp(int texel, Vector4 frame)
        {
            return Vector4.LerpUnclamped(Texel(texel, (int)frame.x), Texel(texel, (int)frame.y), frame.z);
        }

        private Vector4 Texel(int texel, int row)
        {
            var offset = (row * _width + texel) * ChannelCount;
            return new Vector4(Half(offset), Half(offset + 1), Half(offset + 2), Half(offset + 3));
        }

        private float Half(int offset)
        {
            return Mathf.HalfToFloat(_texels[offset]);
        }
    }
}
