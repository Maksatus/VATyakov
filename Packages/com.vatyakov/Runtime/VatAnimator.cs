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

        private readonly VatPropertyBlockCheck _blockCheck = new();

        [NonSerialized]
        private VatMaterialCopies _copies;

        [NonSerialized]
        private VatPlayer _player;

        [NonSerialized]
        private Vector4 _writtenFrame;

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
        public VatClip CurrentClip => _player?.Clip;
        public bool IsPaused => Player.IsPaused;

        public float Speed
        {
            get => _speed;
            set
            {
                _speed = value;
                Player.SetSpeed(Now, value);
            }
        }

        private static double Now => Time.timeAsDouble;
        private VatPlayer Player => _player ??= new VatPlayer(_speed);
        private VatMaterialCopies Copies => _copies ??= CreateCopies();

        public void Play(string clipName)
        {
            if (TryGetClip(clipName, out var clip))
            {
                Player.Play(clip, Now);
            }
        }

        public void Play(string clipName, float normalizedTime)
        {
            if (TryGetClip(clipName, out var clip))
            {
                Player.Play(clip, Now, normalizedTime);
            }
        }

        public void Play(int clipIndex)
        {
            if (TryGetClip(clipIndex, out var clip))
            {
                Player.Play(clip, Now);
            }
        }

        public void Play(int clipIndex, float normalizedTime)
        {
            if (TryGetClip(clipIndex, out var clip))
            {
                Player.Play(clip, Now, normalizedTime);
            }
        }

        public void Pause()
        {
            Player.Pause(Now);
        }

        public void Resume()
        {
            Player.Resume(Now);
        }

        public float GetNormalizedTime()
        {
            return Player.NormalizedTime(Now);
        }

        public void SetFloat(int id, float value)
        {
            foreach (var material in Materials)
            {
                material.SetFloat(id, value);
            }
        }

        public void SetFloat(string propertyName, float value)
        {
            SetFloat(Shader.PropertyToID(propertyName), value);
        }

        public void SetColor(int id, Color value)
        {
            foreach (var material in Materials)
            {
                material.SetColor(id, value);
            }
        }

        public void SetColor(string propertyName, Color value)
        {
            SetColor(Shader.PropertyToID(propertyName), value);
        }

        public void SetVector(int id, Vector4 value)
        {
            foreach (var material in Materials)
            {
                material.SetVector(id, value);
            }
        }

        public void SetVector(string propertyName, Vector4 value)
        {
            SetVector(Shader.PropertyToID(propertyName), value);
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
            if (_player?.Clip == null || Copies == null)
            {
                return;
            }

            if (WriteFrame())
            {
                ClipFinished?.Invoke(_player.Clip);
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

            var index = string.IsNullOrEmpty(_clip) ? 0 : _asset.FindClip(_clip);
            if (index < 0)
            {
                LogMissingClip(_clip);
                index = 0;
            }

            Player.Play(_asset.Clips[index], Now);
        }

        private bool WriteFrame()
        {
            using var _ = _writeMarker.Auto();
            var isFinished = _player.Evaluate(Now, out var frame);
            if (!_hasWrittenFrame || !frame.Equals(_writtenFrame))
            {
                foreach (var material in _copies.Materials)
                {
                    material.SetVector(VatShaderIds.Frame, frame);
                }

                _writtenFrame = frame;
                _hasWrittenFrame = true;
            }

            _blockCheck.Run(_copies.Renderers);
            return isFinished;
        }

        private bool TryGetClip(string clipName, out VatClip clip)
        {
            clip = null;
            if (!IsAssetPlayable())
            {
                return false;
            }

            var index = _asset.FindClip(clipName);
            if (index < 0)
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
                Debug.LogError($"{name}: VAT asset '{_asset.name}' has {_asset.Clips.Count} clip(s), no clip {clipIndex}.", this);
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
