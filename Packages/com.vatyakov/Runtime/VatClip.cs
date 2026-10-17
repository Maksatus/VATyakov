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
        private bool _isLooping;

        public string Name => _name;
        public int StartRow => _startRow;
        public int FrameCount => _frameCount;
        public float Length => _length;
        public float FrameRate => _frameRate;
        public bool IsLooping => _isLooping;

        private int Intervals => _frameCount - 1;

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
            return VatTiming.FrameTime(frame, _frameCount, _length);
        }

        public double Wrap(double position)
        {
            if (!_isLooping)
            {
                return Math.Min(Math.Max(position, 0.0), Intervals);
            }

            var wrapped = position - Intervals * Math.Floor(position / Intervals);
            return wrapped < Intervals ? wrapped : 0.0;
        }

        public float NormalizedTime(double position)
        {
            return (float)(Wrap(position) / Intervals);
        }

        public double Position(float normalizedTime)
        {
            return normalizedTime * (double)Intervals;
        }

        public Vector4 Frame(double position)
        {
            var wrapped = Wrap(position);
            var frame0 = Math.Min((int)wrapped, Intervals - 1);
            return new Vector4(_startRow + frame0, _startRow + frame0 + 1, (float)(wrapped - frame0), 0f);
        }
    }
}
