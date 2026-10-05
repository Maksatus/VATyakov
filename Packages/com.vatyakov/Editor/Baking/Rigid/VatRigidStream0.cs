using System.Runtime.InteropServices;
using UnityEngine;

namespace VATyakov.Editor
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct VatRigidStream0
    {
        private const int ByteBits = 8;
        private const int ByteMask = 0xFF;

        public Vector3 Position;
        public byte PieceHigh;
        public byte PieceLow;
        public byte Unused0;
        public byte Unused1;

        public static VatRigidStream0[] Build(VatBoneSkin skin)
        {
            var data = new VatRigidStream0[skin.Influences.Length];
            for (var vertex = 0; vertex < data.Length; vertex++)
            {
                var piece = skin.Influences[vertex].Bone0;
                data[vertex] = new VatRigidStream0
                {
                    Position = skin.Rest.Positions[vertex],
                    PieceHigh = (byte)(piece >> ByteBits),
                    PieceLow = (byte)(piece & ByteMask),
                };
            }

            return data;
        }
    }
}
