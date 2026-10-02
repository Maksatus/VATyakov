using UnityEngine;

namespace VATyakov
{
    internal sealed class VatPlayer
    {
        private readonly VatEndLatch _endLatch = new();

        private VatPlayback _playback;
        private bool _isPaused;

        public VatClip Clip => _playback?.Clip;
        public float Speed { get; private set; }
        public bool IsPaused => _isPaused;

        private float EffectiveSpeed => _isPaused ? 0f : Speed;

        public VatPlayer(float speed)
        {
            Speed = speed;
        }

        public void Play(VatClip clip, double time)
        {
            Play(clip, time, Speed < 0f && !clip.IsLooping ? 1f : 0f);
        }

        public void Play(VatClip clip, double time, float normalizedTime)
        {
            _playback = new VatPlayback(clip, time, EffectiveSpeed, clip.Position(normalizedTime));
            _endLatch.Reset();
        }

        public void SetSpeed(double time, float speed)
        {
            Speed = speed;
            Apply(time);
        }

        public void Pause(double time)
        {
            _isPaused = true;
            Apply(time);
        }

        public void Resume(double time)
        {
            _isPaused = false;
            Apply(time);
        }

        public float NormalizedTime(double time)
        {
            return _playback != null ? Clip.NormalizedTime(_playback.Position(time)) : 0f;
        }

        public bool Evaluate(double time, out Vector4 frame)
        {
            var position = _playback.Position(time);
            frame = Clip.Frame(position);
            return _endLatch.Update(Clip, position, _playback.Speed);
        }

        private void Apply(double time)
        {
            _playback?.SetSpeed(time, EffectiveSpeed);
        }
    }
}
