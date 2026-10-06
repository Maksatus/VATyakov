using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatRigidFitBlend
    {
        public static Matrix4x4[] Fill(Matrix4x4[] fits, Vector3 center, double[] times, bool[] isFitted)
        {
            var known = new List<int>();
            for (var sample = 0; sample < fits.Length; sample++)
            {
                if (isFitted[sample])
                {
                    known.Add(sample);
                }
            }

            known.Sort((a, b) => times[a].CompareTo(times[b]));
            var filled = (Matrix4x4[])fits.Clone();
            for (var sample = 0; sample < filled.Length; sample++)
            {
                if (!isFitted[sample])
                {
                    filled[sample] = Between(fits, center, times, known, times[sample]);
                }
            }

            return filled;
        }

        private static Matrix4x4 Between(Matrix4x4[] fits, Vector3 center, double[] times, List<int> known, double time)
        {
            var next = 0;
            var end = known.Count;
            while (next < end)
            {
                var middle = (next + end) / 2;
                if (times[known[middle]] < time)
                {
                    next = middle + 1;
                }
                else
                {
                    end = middle;
                }
            }

            if (known.Count == 0)
            {
                return Matrix4x4.identity;
            }

            if (next == 0 || next == known.Count)
            {
                return fits[known[Mathf.Min(next, known.Count - 1)]];
            }

            var a = known[next - 1];
            var b = known[next];
            var fraction = (float)((time - times[a]) / (times[b] - times[a]));
            var rotation = Quaternion.Slerp(fits[a].rotation, fits[b].rotation, fraction);
            var position = Vector3.Lerp(fits[a].MultiplyPoint3x4(center), fits[b].MultiplyPoint3x4(center), fraction);
            return Matrix4x4.TRS(position - rotation * center, rotation, Vector3.one);
        }
    }
}
