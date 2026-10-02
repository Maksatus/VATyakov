using System;
using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("_loop")]
        [SerializeField]
        private bool _isLooping;

        public string Name => _name;
        public int StartRow => _startRow;
        public int FrameCount => _frameCount;
        public float Length => _length;
        public float FrameRate => _frameRate;
        public bool IsLooping => _isLooping;

        public VatClip(string name, int startRow, int frameCount, float length, float frameRate, bool isLooping)
        {
            _name = name;
            _startRow = startRow;
            _frameCount = frameCount;
            _length = length;
            _frameRate = frameRate;
            _isLooping = isLooping;
        }

        public double FrameTime(int frame)
        {
            return VatTiming.FrameTime(frame, _frameCount, _length, _isLooping);
        }

        public double Wrap(double position)
        {
            if (!_isLooping)
            {
                return Math.Min(Math.Max(position, 0.0), _frameCount - 1);
            }

            var wrapped = position - _frameCount * Math.Floor(position / _frameCount);
            return wrapped < _frameCount ? wrapped : 0.0;
        }

        public float NormalizedTime(double position)
        {
            var wrapped = Wrap(position);
            if (!_isLooping)
            {
                return _frameCount > 1 ? (float)(wrapped / (_frameCount - 1)) : 1f;
            }

            var phase = (float)(wrapped / _frameCount);
            return phase < 1f ? phase : 0f;
        }

        public double Position(float normalizedTime)
        {
            return normalizedTime * (double)(_isLooping ? _frameCount : _frameCount - 1);
        }

        public Vector4 Frame(double position)
        {
            var wrapped = Wrap(position);
            var frame0 = Math.Min((int)wrapped, _isLooping ? _frameCount - 1 : Math.Max(_frameCount - 2, 0));
            var frame1 = frame0 + 1 < _frameCount ? frame0 + 1 : _isLooping ? 0 : frame0;
            return new Vector4(_startRow + frame0, _startRow + frame1, (float)(wrapped - frame0), 0f);
        }
    }
}
