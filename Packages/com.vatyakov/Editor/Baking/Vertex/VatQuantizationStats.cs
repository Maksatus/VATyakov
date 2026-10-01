using UnityEngine;

namespace VATyakov.Editor
{
    // Degrees. The position error is VatPrecision: with drift and without it (1.5).
    sealed class VatQuantizationStats
    {
        // Angle between the source N or T and the frame decoded from _VatRotTex.
        public float MaxRotationError { get; private set; }

        // Vertex-frames whose tangent was carried over or rebuilt from N (§2.2).
        public int DegenerateTangents { get; private set; }

        public void AddRotation(float normalError, float tangentError) =>
            MaxRotationError = Mathf.Max(MaxRotationError, Mathf.Max(normalError, tangentError));

        public void AddDegenerate(int count) => DegenerateTangents += count;
    }
}
