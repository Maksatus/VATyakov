using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace VATyakov.Dev
{
    public sealed class VatStress : MonoBehaviour
    {
        private static readonly ProfilerMarker _driveMarker = new("VatStress.Drive");

        [SerializeField]
        private Animator _smrPrefab;

        [SerializeField]
        private RuntimeAnimatorController _smrController;

        [SerializeField]
        private VatAnimator _vatPrefab;

        [Min(1)]
        [SerializeField]
        private int _count = 500;

        [Min(1)]
        [SerializeField]
        private int _columns = 25;

        [Min(0.1f)]
        [SerializeField]
        private float _spacing = 1.4f;

        [Min(0f)]
        [SerializeField]
        private float _fadeDuration = 0.25f;

        [Min(0f)]
        [SerializeField]
        private float _minHoldTime = 1f;

        [Min(0f)]
        [SerializeField]
        private float _maxHoldTime = 4f;

        [Min(1)]
        [SerializeField]
        private int _frameRate = 60;

        [SerializeField]
        private string _startVariant = "vat";

        private readonly List<IVatStressUnit> _units = new();
        private readonly List<float> _switchTimes = new();
        private readonly List<float> _transitionEnds = new();

        private int[] _stateHashes;
        private long _transitionCount;
        private long _sampleCount;

        public VatStressVariant Variant { get; private set; } = VatStressVariant.Empty;
        public float TransitionShare => _sampleCount > 0 ? (float)_transitionCount / _sampleCount : 0f;
        internal IReadOnlyList<IVatStressUnit> Units => _units;
        private int ClipCount => _stateHashes.Length;
        private int RowCount => Mathf.CeilToInt((float)_count / _columns);

        public void Select(VatStressVariant variant)
        {
            Clear();
            Variant = variant;
            _transitionCount = 0;
            _sampleCount = 0;
            if (variant.Mode == VatStressMode.Empty)
            {
                return;
            }

            for (var i = 0; i < _count; i++)
            {
                Spawn(i);
            }
        }

        private void Start()
        {
            Application.targetFrameRate = _frameRate;
            Application.runInBackground = true;
            _stateHashes = StateHashes(_vatPrefab.Asset);
            Select(VatStressVariant.Find(_startVariant));
        }

        private void Update()
        {
            Drive(Time.time);
            CountTransitions(Time.time);
        }

        private void Drive(float time)
        {
            using var _ = _driveMarker.Auto();
            for (var i = 0; i < _units.Count; i++)
            {
                if (!Variant.IsCrossFadeEveryFrame && time < _switchTimes[i])
                {
                    continue;
                }

                _units[i].CrossFade(Random.Range(0, ClipCount), _fadeDuration);
                _switchTimes[i] = time + HoldTime();
                _transitionEnds[i] = time + _fadeDuration;
            }
        }

        private void CountTransitions(float time)
        {
            foreach (var end in _transitionEnds)
            {
                if (time < end)
                {
                    _transitionCount++;
                }
            }

            _sampleCount += _units.Count;
        }

        private void Spawn(int index)
        {
            var offset = new Vector3(index % _columns - (_columns - 1) * 0.5f, 0f, index / _columns - (RowCount - 1) * 0.5f) * _spacing;
            var position = transform.position + transform.rotation * offset;
            var unit = Variant.Mode == VatStressMode.Smr ? SpawnSmr(position) : SpawnVat(position);
            unit.Play(Random.Range(0, ClipCount), Random.value);
            if (Variant.ForcedLod != VatStressVariant.AutoLod)
            {
                unit.Root.GetComponent<LODGroup>().ForceLOD(Variant.ForcedLod);
            }

            _units.Add(unit);
            _switchTimes.Add(Time.time + HoldTime());
            _transitionEnds.Add(0f);
        }

        private IVatStressUnit SpawnSmr(Vector3 position)
        {
            var animator = Instantiate(_smrPrefab, position, transform.rotation, transform);
            animator.runtimeAnimatorController = _smrController;
            animator.fireEvents = false;
            foreach (var skin in animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skin.quality = SkinQuality.Bone2;
            }

            return new VatStressSmrUnit(animator, _stateHashes);
        }

        private IVatStressUnit SpawnVat(Vector3 position)
        {
            return new VatStressVatUnit(Instantiate(_vatPrefab, position, transform.rotation, transform));
        }

        private void Clear()
        {
            foreach (var unit in _units)
            {
                Destroy(unit.Root);
            }

            _units.Clear();
            _switchTimes.Clear();
            _transitionEnds.Clear();
        }

        private float HoldTime()
        {
            return Random.Range(_minHoldTime, _maxHoldTime);
        }

        private static int[] StateHashes(VatAsset asset)
        {
            var hashes = new int[asset.Clips.Count];
            for (var i = 0; i < hashes.Length; i++)
            {
                hashes[i] = Animator.StringToHash(asset.Clips[i].Name);
            }

            return hashes;
        }
    }
}
