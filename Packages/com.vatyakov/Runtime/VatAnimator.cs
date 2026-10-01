using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace VATyakov
{
    // §1.6: per-unit materials and playback. Copies exist only in Play mode and in builds: in edit mode the templates
    // are untouched and show the frame chosen in the inspectors. Pooling through SetActive keeps the copies.
    [DisallowMultipleComponent]
    public sealed class VatAnimator : MonoBehaviour
    {
        static readonly ProfilerMarker WriteMarker = new ProfilerMarker("VatAnimator.Write");

        [SerializeField] VatAsset _asset;
        [Tooltip("Clip Play On Enable starts. Empty: the default clip of the VAT asset.")]
        [SerializeField] string _clip = string.Empty;
        [Tooltip("Start the clip from its beginning on every OnEnable, so a unit taken from a pool starts fresh.")]
        [SerializeField] bool _playOnEnable = true;
        [Tooltip("Negative plays backwards: a loop wraps, a one-shot stops on frame 0.")]
        [SerializeField] float _speed = 1f;

        [NonSerialized] VatMaterialCopies _copies;
        [NonSerialized] VatPlayer _player;
        [NonSerialized] Vector4 _written;
        [NonSerialized] bool _hasWritten;
        [NonSerialized] bool _destroyed;
        readonly VatPropertyBlockCheck _blockCheck = new VatPropertyBlockCheck();

        // A one-shot reached the end it plays towards; once per Play and direction.
        public event Action<VatClip> ClipFinished;

        public VatAsset Asset
        {
            get => _asset;
            internal set => _asset = value;
        }

        // Empty outside Play mode.
        public IReadOnlyList<Material> Materials => Copies?.Materials ?? (IReadOnlyList<Material>)Array.Empty<Material>();

        public VatClip CurrentClip => _player?.Clip;

        public float Speed
        {
            get => _speed;
            set
            {
                _speed = value;
                Player.SetSpeed(Now, value);
            }
        }

        public bool IsPaused => Player.IsPaused;

        VatPlayer Player => _player ??= new VatPlayer(_speed);

        VatMaterialCopies Copies =>
            _copies ??= Application.isPlaying && !_destroyed ? new VatMaterialCopies(GetComponentsInChildren<Renderer>(true), name) : null;

        static double Now => Time.timeAsDouble;

        // Always from the start; a one-shot played backwards starts from its last frame.
        public void Play(string clipName) => Play(Find(clipName));

        public void Play(string clipName, float normalizedTime) => Play(Find(clipName), normalizedTime);

        public void Play(int clipIndex)
        {
            if (IsPlayable(clipIndex))
                Player.Play(_asset.Clips[clipIndex], Now);
        }

        public void Play(int clipIndex, float normalizedTime)
        {
            if (IsPlayable(clipIndex))
                Player.Play(_asset.Clips[clipIndex], Now, normalizedTime);
        }

        public void Pause() => Player.Pause(Now);

        public void Resume() => Player.Resume(Now);

        public float GetNormalizedTime() => Player.NormalizedTime(Now);

        public void SetFloat(int id, float value)
        {
            foreach (var material in Materials)
                material.SetFloat(id, value);
        }

        public void SetFloat(string propertyName, float value) => SetFloat(Shader.PropertyToID(propertyName), value);

        public void SetColor(int id, Color value)
        {
            foreach (var material in Materials)
                material.SetColor(id, value);
        }

        public void SetColor(string propertyName, Color value) => SetColor(Shader.PropertyToID(propertyName), value);

        public void SetVector(int id, Vector4 value)
        {
            foreach (var material in Materials)
                material.SetVector(id, value);
        }

        public void SetVector(string propertyName, Vector4 value) => SetVector(Shader.PropertyToID(propertyName), value);

        void OnEnable()
        {
            if (Copies != null && _playOnEnable)
                Play(StartClip());
        }

        void LateUpdate()
        {
            if (_player?.Clip == null || Copies == null)
                return;

            bool finished = Write();
            if (finished)
                ClipFinished?.Invoke(_player.Clip);
        }

        void OnDestroy()
        {
            _destroyed = true;
            _copies?.Dispose();
            _copies = null;
        }

        // §1.4: one SetVector per copy, skipped while the frame stands (pause, a finished one-shot).
        bool Write()
        {
            using var _ = WriteMarker.Auto();
            bool finished = _player.Evaluate(Now, out var frame);
            if (!_hasWritten || !frame.Equals(_written))
            {
                foreach (var material in _copies.Materials)
                    material.SetVector(VatShaderIds.Frame, frame);
                _written = frame;
                _hasWritten = true;
            }

            _blockCheck.Run(_copies.Renderers);
            return finished;
        }

        int StartClip()
        {
            if (_asset == null || string.IsNullOrEmpty(_clip))
                return _asset != null ? _asset.DefaultClipIndex : -1;
            int index = _asset.FindClip(_clip);
            return index >= 0 ? index : LogMissing(_clip, _asset.DefaultClipIndex);
        }

        // A missing name is reported here; int.MinValue keeps IsPlayable from reporting it again.
        int Find(string clipName)
        {
            if (_asset == null)
                return -1;
            int index = _asset.FindClip(clipName);
            return index >= 0 ? index : LogMissing(clipName, int.MinValue);
        }

        int LogMissing(string clipName, int fallback)
        {
            Debug.LogError($"{name}: VAT asset '{_asset.name}' has no clip named '{clipName}'.", this);
            return fallback;
        }

        bool IsPlayable(int clipIndex)
        {
            if (_asset == null)
                Debug.LogError($"{name}: VatAnimator has no VAT asset.", this);
            else if (!_asset.TryValidate(out var error))
                Debug.LogError($"{name}: {error}", this);
            else if ((uint)clipIndex < (uint)_asset.Clips.Count)
                return true;
            else if (clipIndex != int.MinValue)
                Debug.LogError($"{name}: VAT asset '{_asset.name}' has {_asset.Clips.Count} clip(s), no clip {clipIndex}.", this);
            return false;
        }
    }
}
