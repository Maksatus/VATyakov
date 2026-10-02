using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatDriftCodec
    {
        public static (VatHalf3 Hi, VatHalf3 Lo) Encode(Vector3 drift)
        {
            var hi = new VatHalf3(drift);
            return (hi, new VatHalf3(drift - hi.ToVector3()));
        }

        public static Vector3 Decode(VatHalf3 hi, VatHalf3 lo)
        {
            return VatMath.Drift(hi.ToVector3(), lo.ToVector3());
        }
    }
}
