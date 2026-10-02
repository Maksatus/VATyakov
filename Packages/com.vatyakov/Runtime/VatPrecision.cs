using System;
using UnityEngine;

namespace VATyakov
{
    [Serializable]
    public struct VatPrecision
    {
        [SerializeField]
        private float _maxDrift;
        [SerializeField]
        private float _error;

        public float MaxDrift => _maxDrift;
        public float Error => _error;

        public VatPrecision(float maxDrift, float error)
        {
            _maxDrift = maxDrift;
            _error = error;
        }
    }
}
