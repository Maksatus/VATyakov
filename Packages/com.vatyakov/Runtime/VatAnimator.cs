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
            Play(_asset.IndexOf(clipName));
        }

        public void Play(string clipName, float normalizedTime)
        {
            Play(_asset.IndexOf(clipName), normalizedTime);
        }

        public void Play(int clipIndex)
        {
            Mixer.Play(_asset.Clips[clipIndex], Now);
        }

        public void Play(int clipIndex, float normalizedTime)
        {
            Mixer.Play(_asset.Clips[clipIndex], Now, normalizedTime);
        }

        public void CrossFade(string clipName, float duration)
        {
            CrossFade(_asset.IndexOf(clipName), duration);
        }

        public void CrossFade(int clipIndex, float duration)
        {
            CrossFade(_asset.Clips[clipIndex], duration);
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
                Play(string.IsNullOrEmpty(_clip) ? 0 : _asset.IndexOf(_clip));
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
    }
}
