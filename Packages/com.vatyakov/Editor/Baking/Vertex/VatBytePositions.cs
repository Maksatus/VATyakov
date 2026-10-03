using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatBytePositions
    {
        private const float ByteMax = 255f;

        public static VatPositionRange Range(Vector3[] deltas)
        {
            var min = Vector3.positiveInfinity;
            var max = Vector3.negativeInfinity;
            foreach (var delta in deltas)
            {
                min = Vector3.Min(min, delta);
                max = Vector3.Max(max, delta);
            }

            return new VatPositionRange(min, max - min);
        }

        public static float MaxError(Vector3[] deltas, VatPositionRange range)
        {
            var error = 0f;
            foreach (var delta in deltas)
            {
                error = Mathf.Max(error, (Decode(Encode(delta, range), range) - delta).magnitude);
            }

            return error;
        }

        public static Color32 Encode(Vector3 delta, VatPositionRange range)
        {
            return new Color32(Byte(delta.x, range.Min.x, range.Size.x), Byte(delta.y, range.Min.y, range.Size.y), Byte(delta.z, range.Min.z, range.Size.z), 0);
        }

        public static Vector3 Decode(Color32 texel, VatPositionRange range)
        {
            return Vector3.Scale(new Vector3(Sample(texel.r), Sample(texel.g), Sample(texel.b)), range.Size) + range.Min;
        }

        private static byte Byte(float value, float min, float size)
        {
            return size > 0f ? (byte)Mathf.RoundToInt((value - min) / size * ByteMax) : (byte)0;
        }

        private static float Sample(byte value)
        {
            return Mathf.HalfToFloat(Mathf.FloatToHalf(value / ByteMax));
        }
    }
}
