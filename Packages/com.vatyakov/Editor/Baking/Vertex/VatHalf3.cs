using UnityEngine;

namespace VATyakov.Editor
{
    readonly struct VatHalf3
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

        public Vector3 ToVector3() => new Vector3(Mathf.HalfToFloat(X), Mathf.HalfToFloat(Y), Mathf.HalfToFloat(Z));
    }
}
