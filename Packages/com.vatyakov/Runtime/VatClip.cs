using System;
using UnityEngine;

namespace VATyakov
{
    [Serializable]
    public sealed class VatClip
    {
        [SerializeField] string _name;
        [SerializeField] int _startRow;
        [SerializeField] int _frameCount;
        [SerializeField] float _length;
        [SerializeField] float _frameRate;
        [SerializeField] bool _loop;

        public VatClip(string name, int startRow, int frameCount, float length, float frameRate, bool loop)
        {
            _name = name;
            _startRow = startRow;
            _frameCount = frameCount;
            _length = length;
            _frameRate = frameRate;
            _loop = loop;
        }

        public string Name => _name;

        // Inside a block (§1.1).
        public int StartRow => _startRow;

        public int FrameCount => _frameCount;

        // Source clip length, seconds.
        public float Length => _length;

        // fps_eff (§1.3).
        public float FrameRate => _frameRate;

        public bool Loop => _loop;

        // Source time of baked frame k (§1.3).
        public double FrameTime(int frame) => VatTiming.FrameTime(frame, _frameCount, _length, _loop);

        // Shown frame position: a loop wraps into [0, F), a one-shot stops at 0 and F − 1.
        public double Wrap(double position)
        {
            if (!_loop)
                return Math.Min(Math.Max(position, 0.0), _frameCount - 1);
            double u = position - _frameCount * Math.Floor(position / _frameCount);
            return u < _frameCount ? u : 0.0;
        }

        // A loop: the phase in [0, 1). A one-shot: 0 at frame 0, 1 at the last frame.
        public float NormalizedTime(double position)
        {
            double u = Wrap(position);
            if (!_loop)
                return _frameCount > 1 ? (float)(u / (_frameCount - 1)) : 1f;
            float phase = (float)(u / _frameCount);
            return phase < 1f ? phase : 0f; // rounded up to 1 in float: the next cycle starts
        }

        public double Position(float normalizedTime) => normalizedTime * (double)(_loop ? _frameCount : _frameCount - 1);

        // §1.4: _VatFrame = (row0, row1, frac, 0). A one-shot reaches its last frame as f0 = F − 2 with frac = 1.
        public Vector4 Frame(double position)
        {
            double u = Wrap(position);
            int f0 = Math.Min((int)u, _loop ? _frameCount - 1 : Math.Max(_frameCount - 2, 0));
            int f1 = f0 + 1 < _frameCount ? f0 + 1 : _loop ? 0 : f0;
            return new Vector4(_startRow + f0, _startRow + f1, (float)(u - f0), 0f);
        }
    }
}
