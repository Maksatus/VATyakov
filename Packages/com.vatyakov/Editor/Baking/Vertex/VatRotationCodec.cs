using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatRotationCodec
    {
        public static Color32 Encode(Vector4 rotation)
        {
            return new Color32(Byte(rotation.x), Byte(rotation.y), Byte(rotation.z), Byte(rotation.w));
        }

        private static byte Byte(float component)
        {
            return (byte)Mathf.RoundToInt(component * VatMath.RotationUnit + VatMath.RotationZero);
        }
    }
}
