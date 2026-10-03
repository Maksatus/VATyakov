using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatDriftRows
    {
        public readonly Vector3[] Rows;

        public float MaxDistance { get; private set; }

        public VatDriftRows(VatLayoutInfo info)
        {
            Rows = new Vector3[info.TotalRows];
        }

        public void Write(int row, Vector3 drift)
        {
            Rows[row] = drift;
            MaxDistance = Mathf.Max(MaxDistance, drift.magnitude);
        }
    }
}
