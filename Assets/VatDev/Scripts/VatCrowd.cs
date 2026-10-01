using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Dev
{
    /// <summary>
    /// 1.7 checks: units of a VatAnimator prefab in a grid. Each unit plays the next clip from ClipFinished, so a
    /// doubled event shows up in the counters. Pool hides and shows every unit with SetActive, Hit flashes _BaseColor
    /// through VatAnimator.SetColor. The cost of the per-frame _VatFrame write is the "VatAnimator.Write" profiler marker.
    /// </summary>
    public sealed class VatCrowd : MonoBehaviour
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] VatAnimator _prefab;
        [SerializeField, Min(1)] int _count = 100;
        [SerializeField, Min(0.1f)] float _spacing = 1.5f;
        [SerializeField] Color _flash = new Color(1f, 0.15f, 0.1f);
        [SerializeField, Min(0f)] float _flashTime = 0.15f;

        readonly List<VatAnimator> _units = new List<VatAnimator>();
        readonly Dictionary<VatAnimator, int> _finishedSincePlay = new Dictionary<VatAnimator, int>();
        Color _baseColor = Color.white;
        float _flashEnd = -1f;
        bool _flashing;

        public int Count => _units.Count;

        public bool Active { get; private set; } = true;

        public bool Paused { get; private set; }

        public int Finished { get; private set; }

        // Events after the first one for the same Play: must stay 0.
        public int Doubled { get; private set; }

        void Start()
        {
            if (_prefab == null || _prefab.Asset == null)
            {
                Debug.LogError($"{name}: VatCrowd needs a prefab with a VatAnimator and a VAT asset.", this);
                enabled = false;
                return;
            }

            int columns = Mathf.CeilToInt(Mathf.Sqrt(_count));
            for (int i = 0; i < _count; i++)
                Spawn(i, columns);
            if (_units[0].Materials.Count > 0)
                _baseColor = _units[0].Materials[0].GetColor(BaseColor);
        }

        void Spawn(int index, int columns)
        {
            var position = transform.position + new Vector3(index % columns, 0f, index / columns) * _spacing;
            var unit = Instantiate(_prefab, position, transform.rotation, transform);
            unit.name = $"{_prefab.name} {index}";
            unit.ClipFinished += clip => OnFinished(unit, clip);
            _units.Add(unit);
            PlayRandom(unit);
        }

        void OnFinished(VatAnimator unit, VatClip clip)
        {
            Finished++;
            if (++_finishedSincePlay[unit] > 1)
            {
                Doubled++;
                Debug.LogError($"{unit.name}: '{clip.Name}' finished {_finishedSincePlay[unit]} times.", unit);
            }

            Play(unit, (unit.Asset.FindClip(clip.Name) + 1) % unit.Asset.Clips.Count, 0f);
        }

        // Random clip and phase, so the units do not move in step.
        void PlayRandom(VatAnimator unit) => Play(unit, Random.Range(0, unit.Asset.Clips.Count), Random.value);

        void Play(VatAnimator unit, int clipIndex, float normalizedTime)
        {
            _finishedSincePlay[unit] = 0;
            unit.Play(clipIndex, unit.Speed < 0f ? 1f - normalizedTime : normalizedTime);
        }

        void Update()
        {
            if (_flashing && Time.time >= _flashEnd)
                SetColor(_baseColor);
            _flashing &= Time.time < _flashEnd;
        }

        public void TogglePool()
        {
            Active = !Active;
            foreach (var unit in _units)
            {
                unit.gameObject.SetActive(Active);
                if (Active)
                    PlayRandom(unit); // after Play On Enable restarted the default clip
            }
        }

        public void Hit()
        {
            SetColor(_flash);
            _flashEnd = Time.time + _flashTime;
            _flashing = true;
        }

        public void Reverse()
        {
            foreach (var unit in _units)
                unit.Speed = -unit.Speed;
        }

        public void TogglePause()
        {
            Paused = !Paused;
            foreach (var unit in _units)
                if (Paused)
                    unit.Pause();
                else
                    unit.Resume();
        }

        void SetColor(Color color)
        {
            foreach (var unit in _units)
                unit.SetColor(BaseColor, color);
        }
    }
}
