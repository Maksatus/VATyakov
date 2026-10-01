using UnityEngine;

namespace VATyakov
{
    // CPU state of one VatAnimator (§1.3): clip, speed, pause and the one-shot end. Time is passed in, so it is testable.
    sealed class VatPlayer
    {
        readonly VatEndLatch _end = new VatEndLatch();
        VatPlayback _playback;
        bool _paused;

        public VatPlayer(float speed)
        {
            Speed = speed;
        }

        public VatClip Clip => _playback?.Clip;

        public float Speed { get; private set; }

        public bool IsPaused => _paused;

        float EffectiveSpeed => _paused ? 0f : Speed;

        // From the start in the playback direction: a one-shot played backwards starts from its last frame.
        public void Play(VatClip clip, double time) => Play(clip, time, Speed < 0f && !clip.Loop ? 1f : 0f);

        public void Play(VatClip clip, double time, float normalizedTime)
        {
            _playback = new VatPlayback(clip, time, EffectiveSpeed, clip.Position(normalizedTime));
            _end.Reset();
        }

        public void SetSpeed(double time, float speed)
        {
            Speed = speed;
            Apply(time);
        }

        public void Pause(double time)
        {
            _paused = true;
            Apply(time);
        }

        public void Resume(double time)
        {
            _paused = false;
            Apply(time);
        }

        public float NormalizedTime(double time) => _playback != null ? Clip.NormalizedTime(_playback.Position(time)) : 0f;

        // True on the call where a one-shot reaches its end.
        public bool Evaluate(double time, out Vector4 frame)
        {
            double position = _playback.Position(time);
            frame = Clip.Frame(position);
            return _end.Update(Clip, position, _playback.Speed);
        }

        void Apply(double time) => _playback?.SetSpeed(time, EffectiveSpeed);
    }
}
