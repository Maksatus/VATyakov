using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace VATyakov
{
    [DisallowMultipleComponent]
    public sealed class VatAnimator : MonoBehaviour
    {
        private static readonly ProfilerMarker _writeMarker = new("VatAnimator.Write");

        [SerializeField]
        private VatAsset _asset;

        [Tooltip("Clip Play On Enable starts. Empty: the first clip of the VAT asset.")]
        [SerializeField]
        private string _clip = string.Empty;

        [Tooltip("Start the clip from its beginning on every OnEnable, so a unit taken from a pool starts fresh.")]
        [SerializeField]
        private bool _playOnEnable = true;

        [Tooltip("Negative plays backwards: a loop wraps, a one-shot stops on frame 0.")]
        [SerializeField]
        private float _speed = 1f;

        [NonSerialized]
        private VatMaterialCopies _copies;

        [NonSerialized]
        private VatMixer _mixer;

        [NonSerialized]
        private Vector4 _writtenFrame;

        [NonSerialized]
        private Vector4 _writtenFrameB;

        [NonSerialized]
        private bool _hasWrittenFrame;

        [NonSerialized]
        private bool _isDestroyed;

        public event Action<VatClip> ClipFinished;

        public VatAsset Asset
        {
            get => _asset;
            internal set => _asset = value;
        }

        public IReadOnlyList<Material> Materials => Copies?.Materials ?? (IReadOnlyList<Material>)Array.Empty<Material>();
        public VatClip CurrentClip => _mixer?.Clip;
        public float TransitionWeight => _mixer?.Weight(Now) ?? 0f;
        public bool IsPaused => Mixer.IsPaused;

        public float Speed
        {
            get => _speed;
            set
            {
                _speed = value;
                Mixer.SetSpeed(Now, value);
            }
        }

        private static double Now => Time.timeAsDouble;
        private VatMixer Mixer => _mixer ??= new VatMixer(_speed);
        private VatMaterialCopies Copies => _copies ??= CreateCopies();

        public void Play(string clipName)
        {
            if (TryGetClip(clipName, out var clip))
            {
                Mixer.Play(clip, Now);
            }
        }

        public void Play(string clipName, float normalizedTime)
        {
            if (TryGetClip(clipName, out var clip))
            {
                Mixer.Play(clip, Now, normalizedTime);
            }
        }

        public void Play(int clipIndex)
        {
            if (TryGetClip(clipIndex, out var clip))
            {
                Mixer.Play(clip, Now);
            }
        }

        public void Play(int clipIndex, float normalizedTime)
        {
            if (TryGetClip(clipIndex, out var clip))
            {
                Mixer.Play(clip, Now, normalizedTime);
            }
        }

        public void CrossFade(string clipName, float duration)
        {
            if (TryGetClip(clipName, out var clip))
            {
                CrossFade(clip, duration);
            }
        }

        public void CrossFade(int clipIndex, float duration)
        {
            if (TryGetClip(clipIndex, out var clip))
            {
                CrossFade(clip, duration);
            }
        }

        public void SetWeight(float weight, float duration = 0f)
        {
            Mixer.SetWeight(Now, weight, duration);
        }

        public void Pause()
        {
            Mixer.Pause(Now);
        }

        public void Resume()
        {
            Mixer.Resume(Now);
        }

        public float GetNormalizedTime()
        {
            return Mixer.NormalizedTime(Now);
        }

        private void OnEnable()
        {
            if (Copies != null && _playOnEnable)
            {
                PlayStartClip();
            }
        }

        private void LateUpdate()
        {
            if (_mixer?.Clip == null || Copies == null)
            {
                return;
            }

            if (WriteFrame())
            {
                ClipFinished?.Invoke(_mixer.Clip);
            }
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
            _copies?.Dispose();
            _copies = null;
        }

        private VatMaterialCopies CreateCopies()
        {
            if (!Application.isPlaying || _isDestroyed)
            {
                return null;
            }

            return new VatMaterialCopies(GetComponentsInChildren<Renderer>(true), name);
        }

        private void PlayStartClip()
        {
            if (!IsAssetPlayable())
            {
                return;
            }

            var index = 0;
            if (!string.IsNullOrEmpty(_clip) && !_asset.TryFindClip(_clip, out index))
            {
                LogMissingClip(_clip);
                index = 0;
            }

            Mixer.Play(_asset.Clips[index], Now);
        }

        private void CrossFade(VatClip clip, float duration)
        {
            if (Copies != null && !Copies.CanBlend)
            {
                Mixer.Play(clip, Now);
                return;
            }

            Mixer.CrossFade(clip, Now, duration);
        }

        private bool WriteFrame()
        {
            using var _ = _writeMarker.Auto();
            var isFinished = _mixer.Evaluate(Now, out var frame, out var frameB);
            if (!_hasWrittenFrame || !frame.Equals(_writtenFrame) || !frameB.Equals(_writtenFrameB))
            {
                Write(frame, frameB);
                _writtenFrame = frame;
                _writtenFrameB = frameB;
                _hasWrittenFrame = true;
            }

            return isFinished;
        }

        private void Write(Vector4 frame, Vector4 frameB)
        {
            var hasDrift = _asset.HasDrift;
            var drift = hasDrift ? _asset.Drift(frame, frameB) : Vector4.zero;
            var canBlend = _copies.CanBlend;
            foreach (var material in _copies.Materials)
            {
                material.SetVector(VatShaderIds.Frame, frame);
                if (hasDrift)
                {
                    material.SetVector(VatShaderIds.Drift, drift);
                }

                if (canBlend)
                {
                    material.SetVector(VatShaderIds.FrameB, frameB);
                }
            }
        }

        private bool TryGetClip(string clipName, out VatClip clip)
        {
            clip = null;
            if (!IsAssetPlayable())
            {
                return false;
            }

            if (!_asset.TryFindClip(clipName, out var index))
            {
                LogMissingClip(clipName);
                return false;
            }

            clip = _asset.Clips[index];
            return true;
        }

        private bool TryGetClip(int clipIndex, out VatClip clip)
        {
            clip = null;
            if (!IsAssetPlayable())
            {
                return false;
            }

            if ((uint)clipIndex >= (uint)_asset.Clips.Count)
            {
                Debug.LogError(FormattableString.Invariant($"{name}: VAT asset '{_asset.name}' has {_asset.Clips.Count} clip(s), no clip {clipIndex}."), this);
                return false;
            }

            clip = _asset.Clips[clipIndex];
            return true;
        }

        private bool IsAssetPlayable()
        {
            if (_asset == null)
            {
                Debug.LogError($"{name}: VatAnimator has no VAT asset.", this);
                return false;
            }

            if (!_asset.TryValidate(out var error))
            {
                Debug.LogError($"{name}: {error}", this);
                return false;
            }

            return true;
        }

        private void LogMissingClip(string clipName)
        {
            Debug.LogError($"{name}: VAT asset '{_asset.name}' has no clip named '{clipName}'.", this);
        }
    }
}
