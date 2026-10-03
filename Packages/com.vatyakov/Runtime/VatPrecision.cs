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
        [SerializeField]
        private float _byteError;

        public float MaxDrift => _maxDrift;
        public float Error => _error;
        public float ByteError => _byteError;

        public VatPrecision(float maxDrift, float error, float byteError)
        {
            _maxDrift = maxDrift;
            _error = error;
            _byteError = byteError;
        }
    }
}
