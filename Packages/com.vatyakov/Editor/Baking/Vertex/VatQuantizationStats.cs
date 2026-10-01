using UnityEngine;

namespace VATyakov.Editor
{
    // Meters. The inspector shows the fp16 error (1.10).
    sealed class VatQuantizationStats
    {
        public float MaxOffset { get; private set; }

        public float MaxError { get; private set; }

        public void Add(Vector3 delta, Vector3 error)
        {
            MaxOffset = Mathf.Max(MaxOffset, delta.magnitude);
            MaxError = Mathf.Max(MaxError, error.magnitude);
        }
    }
}
