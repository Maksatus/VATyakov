using UnityEngine;

namespace VATyakov.Editor
{
    // Meters and degrees. The inspector shows the fp16 error (1.10).
    sealed class VatQuantizationStats
    {
        public float MaxOffset { get; private set; }

        public float MaxError { get; private set; }

        // Angle between the source N or T and the frame decoded from _VatRotTex.
        public float MaxRotationError { get; private set; }

        // Vertex-frames whose tangent was carried over or rebuilt from N (§2.2).
        public int DegenerateTangents { get; private set; }

        public void Add(Vector3 delta, Vector3 error)
        {
            MaxOffset = Mathf.Max(MaxOffset, delta.magnitude);
            MaxError = Mathf.Max(MaxError, error.magnitude);
        }

        public void AddRotation(float normalError, float tangentError) =>
            MaxRotationError = Mathf.Max(MaxRotationError, Mathf.Max(normalError, tangentError));

        public void AddDegenerate(int count) => DegenerateTangents += count;
    }
}
