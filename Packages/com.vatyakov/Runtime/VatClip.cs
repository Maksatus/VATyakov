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

        public float Packed => VatMath.PackClip(_startRow, _frameCount, _loop);

        // §1.4: (±(startRow·4096 + F), rate, t0, offset), rate = fps_eff·speed.
        public Vector4 State(float t0, float speed = 1f, float offset = 0f) => new Vector4(Packed, _frameRate * speed, t0, offset);
    }
}
