using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatCentroid
    {
        public static Vector3 Of(Vector3[] positions)
        {
            var x = 0.0;
            var y = 0.0;
            var z = 0.0;
            foreach (var position in positions)
            {
                x += position.x;
                y += position.y;
                z += position.z;
            }

            var count = positions.Length;
            return new Vector3((float)(x / count), (float)(y / count), (float)(z / count));
        }
    }
}
