using System;
using UnityEngine;

namespace Kefir.Vat
{
    /// <summary>One baked clip: its rows in the VAT textures and its timing (§1.3).</summary>
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

        /// <summary>First row of the clip inside a block (§1.1).</summary>
        public int StartRow => _startRow;

        /// <summary>F — number of baked frames.</summary>
        public int FrameCount => _frameCount;

        /// <summary>L — source clip length in seconds.</summary>
        public float Length => _length;

        /// <summary>fps_eff — baked frames per second of clip time (F / L for loops).</summary>
        public float FrameRate => _frameRate;

        public bool Loop => _loop;

        /// <summary>_VatClip*.x for this clip (§1.4).</summary>
        public float Packed => VatMath.PackClip(_startRow, _frameCount, _loop);

        /// <summary>Material state (§1.4): (±(startRow·4096 + F), rate, t0, offset), rate = fps_eff·speed.</summary>
        public Vector4 State(float t0, float speed = 1f, float offset = 0f) => new Vector4(Packed, _frameRate * speed, t0, offset);
    }
}
