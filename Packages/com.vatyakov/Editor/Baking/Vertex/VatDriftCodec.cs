using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatDriftCodec
    {
        public static (VatHalf3 High, VatHalf3 Low) Encode(Vector3 drift)
        {
            var high = new VatHalf3(drift);
            return (high, new VatHalf3(drift - high.ToVector3()));
        }

        public static Vector3 Decode(VatHalf3 high, VatHalf3 low)
        {
            return VatMath.Drift(high.ToVector3(), low.ToVector3());
        }
    }
}
