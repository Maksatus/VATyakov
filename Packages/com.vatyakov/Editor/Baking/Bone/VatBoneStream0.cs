using System.Runtime.InteropServices;
using UnityEngine;

namespace VATyakov.Editor
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct VatBoneStream0
    {
        public Vector3 Position;
        public byte Bone0;
        public byte Bone1;
        public byte WeightHigh;
        public byte WeightLow;

        public static VatBoneStream0[] Build(VatBoneSkin skin)
        {
            var data = new VatBoneStream0[skin.Influences.Length];
            for (var vertex = 0; vertex < data.Length; vertex++)
            {
                data[vertex] = From(skin.Rest.Positions[vertex], skin.Influences[vertex]);
            }

            return data;
        }

        private static VatBoneStream0 From(Vector3 position, VatBoneInfluence influence)
        {
            return new VatBoneStream0
            {
                Position = position,
                Bone0 = (byte)influence.Bone0,
                Bone1 = (byte)influence.Bone1,
                WeightHigh = influence.WeightHigh,
                WeightLow = influence.WeightLow,
            };
        }
    }
}
