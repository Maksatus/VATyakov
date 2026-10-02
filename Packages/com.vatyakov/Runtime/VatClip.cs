using System;
using UnityEngine;

namespace VATyakov
{
    [Serializable]
    public sealed class VatClip
    {
        [SerializeField]
        private string _name;
        [SerializeField]
        private int _startRow;
        [SerializeField]
        private int _frameCount;
        [SerializeField]
        private float _length;
        [SerializeField]
        private float _frameRate;
        [SerializeField]
        private bool _loop;

        public string Name => _name;
        public int StartRow => _startRow;
        public int FrameCount => _frameCount;
        public float Length => _length;
        public float FrameRate => _frameRate;
        public bool Loop => _loop;

        public VatClip(string name, int startRow, int frameCount, float length, float frameRate, bool loop)
        {
            _name = name;
            _startRow = startRow;
            _frameCount = frameCount;
            _length = length;
            _frameRate = frameRate;
            _loop = loop;
        }

        public double FrameTime(int frame)
        {
            return VatTiming.FrameTime(frame, _frameCount, _length, _loop);
        }

        public double Wrap(double position)
        {
            if (!_loop)
            {
                return Math.Min(Math.Max(position, 0.0), _frameCount - 1);
            }

            var u = position - _frameCount * Math.Floor(position / _frameCount);
            return u < _frameCount ? u : 0.0;
        }

        public float NormalizedTime(double position)
        {
            var u = Wrap(position);
            if (!_loop)
            {
                return _frameCount > 1 ? (float)(u / (_frameCount - 1)) : 1f;
            }

            var phase = (float)(u / _frameCount);
            return phase < 1f ? phase : 0f;
        }

        public double Position(float normalizedTime)
        {
            return normalizedTime * (double)(_loop ? _frameCount : _frameCount - 1);
        }

        public Vector4 Frame(double position)
        {
            var u = Wrap(position);
            var f0 = Math.Min((int)u, _loop ? _frameCount - 1 : Math.Max(_frameCount - 2, 0));
            var f1 = f0 + 1 < _frameCount ? f0 + 1 : _loop ? 0 : f0;
            return new Vector4(_startRow + f0, _startRow + f1, (float)(u - f0), 0f);
        }
    }
}
