using UnityEngine;

namespace VATyakov
{
    internal sealed class VatMixer
    {
        private const float DominantWeight = 0.5f;

        private readonly VatWeightRamp _weight = new();

        private VatPlayer _source;
        private VatPlayer _target;

        public VatClip Clip => Current.Clip;
        public VatClip SourceClip => _source.Clip;
        public VatClip TargetClip => _target.Clip;
        public bool IsPaused => _source.IsPaused;

        private bool HasTarget => _target.Clip != null;
        private VatPlayer Current => HasTarget ? _target : _source;

        public VatMixer(float speed)
        {
            _source = new VatPlayer(speed);
            _target = new VatPlayer(speed);
        }

        public float Weight(double time)
        {
            return HasTarget ? _weight.Value(time) : 0f;
        }

        public void Play(VatClip clip, double time)
        {
            _source.Play(clip, time);
            ClearTarget();
        }

        public void Play(VatClip clip, double time, float normalizedTime)
        {
            _source.Play(clip, time, normalizedTime);
            ClearTarget();
        }

        public void CrossFade(VatClip clip, double time, float duration)
        {
            if (_source.Clip == null)
            {
                Play(clip, time);
                return;
            }

            if (HasTarget && _weight.Value(time) >= DominantWeight)
            {
                (_source, _target) = (_target, _source);
            }

            _target.Play(clip, time);
            _weight.Start(time, 0f, 1f, duration);
        }

        public void SetWeight(double time, float weight, float duration)
        {
            if (!HasTarget)
            {
                return;
            }

            _weight.Start(time, _weight.Value(time), weight > 0f ? Mathf.Min(weight, 1f) : 0f, duration);
        }

        public void SetSpeed(double time, float speed)
        {
            _source.SetSpeed(time, speed);
            _target.SetSpeed(time, speed);
        }

        public void Pause(double time)
        {
            _source.Pause(time);
            _target.Pause(time);
            _weight.SetPaused(time, true);
        }

        public void Resume(double time)
        {
            _source.Resume(time);
            _target.Resume(time);
            _weight.SetPaused(time, false);
        }

        public float NormalizedTime(double time)
        {
            return Current.NormalizedTime(time);
        }

        public bool Evaluate(double time, out Vector4 frame, out Vector4 frameB)
        {
            var isSourceFinished = _source.Evaluate(time, out frame);
            if (!HasTarget)
            {
                frameB = Vector4.zero;
                return isSourceFinished;
            }

            var isTargetFinished = _target.Evaluate(time, out frameB);
            frameB.w = _weight.Value(time);
            return isTargetFinished;
        }

        private void ClearTarget()
        {
            _target.Stop();
            _weight.Set(0f);
        }
    }
}
