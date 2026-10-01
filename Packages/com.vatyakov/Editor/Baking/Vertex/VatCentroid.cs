using UnityEngine;

namespace VATyakov.Editor
{
    static class VatCentroid
    {
        // Double sums: a far body averages thousands of large coordinates.
        public static Vector3 Of(Vector3[] positions)
        {
            double x = 0, y = 0, z = 0;
            foreach (var p in positions)
            {
                x += p.x;
                y += p.y;
                z += p.z;
            }
            int n = positions.Length;
            return new Vector3((float)(x / n), (float)(y / n), (float)(z / n));
        }
    }
}
