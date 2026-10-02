using UnityEngine;

namespace VATyakov.Editor
{
    internal readonly struct VatHalf3
    {
        public readonly ushort X;
        public readonly ushort Y;
        public readonly ushort Z;

        public VatHalf3(Vector3 value)
        {
            X = Mathf.FloatToHalf(value.x);
            Y = Mathf.FloatToHalf(value.y);
            Z = Mathf.FloatToHalf(value.z);
        }

        public Vector3 ToVector3()
        {
            return new Vector3(Mathf.HalfToFloat(X), Mathf.HalfToFloat(Y), Mathf.HalfToFloat(Z));
        }

        public void WriteTo(ushort[] texels, int offset)
        {
            texels[offset] = X;
            texels[offset + 1] = Y;
            texels[offset + 2] = Z;
            texels[offset + 3] = 0;
        }
    }
}
