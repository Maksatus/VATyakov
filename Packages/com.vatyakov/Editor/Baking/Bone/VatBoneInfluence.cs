using UnityEngine;

namespace VATyakov.Editor
{
    internal readonly struct VatBoneInfluence
    {
        private const int ByteBits = 8;
        private const int ByteMask = 0xFF;
        private const float ByteMax = 255f;

        public readonly int Bone0;
        public readonly int Bone1;
        public readonly float Weight0;
        public readonly ushort Weight0Bits;

        public byte WeightHigh => (byte)(Weight0Bits >> ByteBits);
        public byte WeightLow => (byte)(Weight0Bits & ByteMask);
        public float StoredWeight0 => VatMath.BoneWeight(WeightHigh / ByteMax, WeightLow / ByteMax);

        private VatBoneInfluence(int bone0, int bone1, float weight0)
        {
            Bone0 = bone0;
            Bone1 = bone1;
            Weight0 = weight0;
            Weight0Bits = (ushort)Mathf.RoundToInt(weight0 * VatMath.BoneWeightMax);
        }

        public static VatBoneInfluence From(BoneWeight weight, int rootBone)
        {
            var sum = weight.weight0 + weight.weight1;
            if (!(sum > 0f))
            {
                return Root(rootBone);
            }

            var bone1 = weight.weight1 > 0f ? weight.boneIndex1 : weight.boneIndex0;
            return new VatBoneInfluence(weight.boneIndex0, bone1, weight.weight0 / sum);
        }

        private static VatBoneInfluence Root(int rootBone)
        {
            return new VatBoneInfluence(rootBone, rootBone, 1f);
        }
    }
}
