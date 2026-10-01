using System;
using UnityEngine;

namespace VATyakov
{
    // Bake-time position error after fp16 sampling, meters (§1.2); the baker measures it with drift and without (§2.2).
    [Serializable]
    public struct VatPrecision
    {
        [SerializeField] float _maxDrift;
        [SerializeField] float _errorWithoutDrift;
        [SerializeField] float _errorWithDrift;

        public VatPrecision(float maxDrift, float errorWithoutDrift, float errorWithDrift)
        {
            _maxDrift = maxDrift;
            _errorWithoutDrift = errorWithoutDrift;
            _errorWithDrift = errorWithDrift;
        }

        // Farthest distance of a frame centroid from the rest centroid.
        public float MaxDrift => _maxDrift;

        public float ErrorWithoutDrift => _errorWithoutDrift;

        public float ErrorWithDrift => _errorWithDrift;

        public float Error(bool drift) => drift ? _errorWithDrift : _errorWithoutDrift;
    }
}
