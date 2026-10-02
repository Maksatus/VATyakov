using UnityEngine;

namespace VATyakov
{
    public sealed class VatPlayback
    {
        private double _startTime;
        private double _offset;

        public VatClip Clip { get; }
        public float Speed { get; private set; }

        public VatPlayback(VatClip clip, double time, float speed = 1f, double offset = 0.0)
        {
            Clip = clip;
            _startTime = time;
            Speed = speed;
            _offset = offset;
        }

        public double Position(double time)
        {
            return (time - _startTime) * Clip.FrameRate * Speed + _offset;
        }

        public Vector4 Frame(double time)
        {
            return Clip.Frame(Position(time));
        }

        public void SetSpeed(double time, float speed)
        {
            _offset = Clip.Wrap(Position(time));
            _startTime = time;
            Speed = speed;
        }
    }
}
