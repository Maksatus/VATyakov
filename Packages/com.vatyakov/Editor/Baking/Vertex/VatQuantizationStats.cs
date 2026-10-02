using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatQuantizationStats
    {
        public float MaxRotationError { get; private set; }
        public int DegenerateTangents { get; private set; }

        public void AddRotation(float normalError, float tangentError)
        {
            MaxRotationError = Mathf.Max(MaxRotationError, Mathf.Max(normalError, tangentError));
        }

        public void AddDegenerate(int count)
        {
            DegenerateTangents += count;
        }
    }
}
