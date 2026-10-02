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
            foreach (var p in positions)
            {
                x += p.x;
                y += p.y;
                z += p.z;
            }

            var n = positions.Length;
            return new Vector3((float)(x / n), (float)(y / n), (float)(z / n));
        }
    }
}
