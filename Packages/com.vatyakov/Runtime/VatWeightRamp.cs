using UnityEngine;

namespace VATyakov
{
    internal sealed class VatWeightRamp
    {
        private float _from;
        private float _to;
        private double _startTime;
        private double _duration;
        private double _startProgress = 1.0;
        private bool _isPaused;

        public float Value(double time)
        {
            return Mathf.Lerp(_from, _to, (float)Progress(time));
        }

        public void Start(double time, float from, float to, float duration)
        {
            _from = from;
            _to = to;
            _startTime = time;
            _duration = duration > 0f ? duration : 0.0;
            _startProgress = 0.0;
        }

        public void Set(float weight)
        {
            _from = weight;
            _to = weight;
            _startProgress = 1.0;
        }

        public void SetPaused(double time, bool isPaused)
        {
            _startProgress = Progress(time);
            _startTime = time;
            _isPaused = isPaused;
        }

        private double Progress(double time)
        {
            if (_duration <= 0.0)
            {
                return 1.0;
            }

            return _isPaused ? _startProgress : _startProgress + (time - _startTime) / _duration;
        }
    }
}
