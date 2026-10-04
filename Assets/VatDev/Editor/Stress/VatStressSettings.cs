using System;
using UnityEngine;

namespace VATyakov.Dev
{
    [Serializable]
    internal sealed class VatStressSettings
    {
        [Min(0f)]
        [SerializeField]
        private float _warmupSeconds = 10f;

        [Min(1)]
        [SerializeField]
        private int _captureFrames = 400;

        public float WarmupSeconds => _warmupSeconds;
        public int CaptureFrames => _captureFrames;
    }
}
