using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace VATyakov.Dev
{
    public sealed class VatCrowd : MonoBehaviour
    {
        private const float ManualRampTime = 0.1f;

        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly float[] _fadeDurations = { 0f, 0.25f, 1f };

        [SerializeField]
        private VatAnimator _prefab;

        [Min(1)]
        [SerializeField]
        private int _count = 100;

        [Min(0.1f)]
        [SerializeField]
        private float _spacing = 1.5f;

        [SerializeField]
        private Color _flashColor = new(1f, 0.15f, 0.1f);

        [Min(0f)]
        [SerializeField]
        private float _flashTime = 0.15f;

        private readonly List<VatAnimator> _units = new();
        private readonly Dictionary<VatAnimator, int> _finishedSincePlay = new();

        private Color _baseColor = Color.white;
        private float _flashEnd;
        private bool _isFlashing;
        private int _fadeIndex = 1;

        public int Count => _units.Count;
        public float FadeDuration => _fadeDurations[_fadeIndex];
        public float FirstUnitWeight => _units.Count > 0 ? _units[0].TransitionWeight : 0f;
        public bool IsActive { get; private set; } = true;
        public bool IsPaused { get; private set; }
        public bool IsManual { get; private set; }
        public float ManualWeight { get; private set; }
        public int FinishedCount { get; private set; }
        public int DoubledCount { get; private set; }

        public void NextFadeDuration()
        {
            _fadeIndex = (_fadeIndex + 1) % _fadeDurations.Length;
        }

        public void ToggleManual()
        {
            IsManual = !IsManual;
            foreach (var unit in _units)
            {
                if (IsManual)
                {
                    StartManual(unit);
                }
                else
                {
                    Advance(unit, NextClipIndex(unit, unit.CurrentClip));
                }
            }
        }

        public void SetManualWeight(float weight)
        {
            ManualWeight = weight;
            foreach (var unit in _units)
            {
                unit.SetWeight(weight, ManualRampTime);
            }
        }

        public void TogglePool()
        {
            IsActive = !IsActive;
            foreach (var unit in _units)
            {
                unit.gameObject.SetActive(IsActive);
                if (IsActive)
                {
                    PlayRandom(unit);
                }

                if (IsActive && IsManual)
                {
                    StartManual(unit);
                }
            }
        }

        public void Hit()
        {
            SetColor(_flashColor);
            _flashEnd = Time.time + _flashTime;
            _isFlashing = true;
        }

        public void Reverse()
        {
            foreach (var unit in _units)
            {
                unit.Speed = -unit.Speed;
            }
        }

        public void TogglePause()
        {
            IsPaused = !IsPaused;
            foreach (var unit in _units)
            {
                if (IsPaused)
                {
                    unit.Pause();
                }
                else
                {
                    unit.Resume();
                }
            }
        }

        private void Start()
        {
            if (_prefab == null || _prefab.Asset == null)
            {
                Debug.LogError($"{name}: VatCrowd needs a prefab with a VatAnimator and a VAT asset.", this);
                enabled = false;
                return;
            }

            var columns = Mathf.CeilToInt(Mathf.Sqrt(_count));
            for (var i = 0; i < _count; i++)
            {
                Spawn(i, columns);
            }

            if (_units[0].Materials.Count > 0)
            {
                _baseColor = _units[0].Materials[0].GetColor(_baseColorId);
            }
        }

        private void Update()
        {
            if (_isFlashing && Time.time >= _flashEnd)
            {
                SetColor(_baseColor);
                _isFlashing = false;
            }
        }

        private void Spawn(int index, int columns)
        {
            var position = transform.position + new Vector3(index % columns, 0f, index / columns) * _spacing;
            var unit = Instantiate(_prefab, position, transform.rotation, transform);
            unit.name = $"{_prefab.name} {index}";
            unit.ClipFinished += clip => OnClipFinished(unit, clip);
            _units.Add(unit);
            PlayRandom(unit);
        }

        private void OnClipFinished(VatAnimator unit, VatClip clip)
        {
            FinishedCount++;
            if (++_finishedSincePlay[unit] > 1)
            {
                DoubledCount++;
                Debug.LogError(FormattableString.Invariant($"{unit.name}: '{clip.Name}' finished {_finishedSincePlay[unit]} times."), unit);
            }

            if (!IsManual)
            {
                Advance(unit, NextClipIndex(unit, clip));
            }
        }

        private void StartManual(VatAnimator unit)
        {
            var clipIndex = NextClipIndex(unit, unit.CurrentClip);
            Play(unit, (clipIndex + unit.Asset.Clips.Count - 1) % unit.Asset.Clips.Count, 0f);
            unit.CrossFade(clipIndex, 0f);
            unit.SetWeight(ManualWeight);
        }

        private void Advance(VatAnimator unit, int clipIndex)
        {
            if (FadeDuration <= 0f)
            {
                Play(unit, clipIndex, 0f);
                return;
            }

            _finishedSincePlay[unit] = 0;
            unit.CrossFade(clipIndex, FadeDuration);
        }

        private static int NextClipIndex(VatAnimator unit, VatClip clip)
        {
            var nextClipIndex = clip != null && unit.Asset.TryFindClip(clip.Name, out var clipIndex) ? clipIndex + 1 : 0;
            return nextClipIndex % unit.Asset.Clips.Count;
        }

        private void PlayRandom(VatAnimator unit)
        {
            Play(unit, Random.Range(0, unit.Asset.Clips.Count), Random.value);
        }

        private void Play(VatAnimator unit, int clipIndex, float normalizedTime)
        {
            _finishedSincePlay[unit] = 0;
            unit.Play(clipIndex, unit.Speed < 0f ? 1f - normalizedTime : normalizedTime);
        }

        private void SetColor(Color color)
        {
            foreach (var unit in _units)
            {
                foreach (var material in unit.Materials)
                {
                    material.SetColor(_baseColorId, color);
                }
            }
        }
    }
}
