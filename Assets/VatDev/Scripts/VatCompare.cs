using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
#if VAT_ALEMBIC
using UnityEngine.Formats.Alembic.Importer;
#endif
using UnityEngine.Playables;

namespace VATyakov.Dev
{
    public sealed class VatCompare : MonoBehaviour
    {
        [SerializeField]
        private VatAsset _asset;

        [Tooltip("Root of the source instance: the clip is sampled on it.")]
        [SerializeField]
        private GameObject _source;

        [Tooltip("Source clips; the one named like the current VAT clip is sampled.")]
        [SerializeField]
        private AnimationClip[] _clips = Array.Empty<AnimationClip>();

#if VAT_ALEMBIC
        [Tooltip("Alembic source instead of _source and _clips.")]
        [SerializeField]
        private AlembicStreamPlayer _alembic;
#endif

        [SerializeField]
        private MeshRenderer _vat;

        [Min(0)]
        [SerializeField]
        private int _clipIndex;

        [SerializeField]
        private bool _isStepping = true;

        [Min(0)]
        [SerializeField]
        private int _frame;

        [SerializeField]
        private float _speed = 1f;

        private Material _shared;
        private Material _material;
        private IReadOnlyList<Material> _materials;
        private VatAnimator _animator;
        private AnimationClip _clip;
        private PlayableGraph _graph;
        private AnimationClipPlayable _playable;
        private VatPlayback _playback;

        public int Frame => _frame;
        public bool IsStepping => _isStepping;
        public string ClipName => _asset != null && _asset.Clips.Count > 0 ? Clip.Name : string.Empty;

        private VatClip Clip => _asset.Clips[Mathf.Clamp(_clipIndex, 0, _asset.Clips.Count - 1)];

        public void SetStepping(bool isStepping)
        {
            _isStepping = isStepping;
            _playback = null;
        }

        public void Advance(int frames)
        {
            if (_asset == null || _asset.Clips.Count == 0)
            {
                return;
            }

            var count = Clip.FrameCount;
            _frame = ((_frame + frames) % count + count) % count;
        }

        public void NextClip()
        {
            if (_asset == null || _asset.Clips.Count == 0)
            {
                return;
            }

            _clipIndex = (_clipIndex + 1) % _asset.Clips.Count;
            _frame = 0;
            _playback = null;
            if (enabled && !HasAlembic())
            {
                SetUpAnimation();
            }
        }

        private void OnEnable()
        {
            if (_asset == null || !HasSource() || _vat == null || !_asset.TryValidate(out _))
            {
                Debug.LogError($"{name}: VatCompare is not set up.", this);
                enabled = false;
                return;
            }

            if (!UseAnimatorCopy())
            {
                _shared = _vat.sharedMaterial;
                _material = new Material(_shared) { name = $"{_shared.name} (Compare)", hideFlags = HideFlags.DontSave };
                _materials = new[] { _material };
                _vat.sharedMaterial = _material;
            }

            _playback = null;
            if (!HasAlembic())
            {
                SetUpAnimation();
            }
        }

        private void Update()
        {
            var clip = Clip;
            double time;
            if (_isStepping)
            {
                _frame = Mathf.Clamp(_frame, 0, clip.FrameCount - 1);
                time = clip.FrameTime(_frame);
                ApplyFrame(clip.Frame(_frame));
            }
            else
            {
                _playback ??= new VatPlayback(clip, Time.timeAsDouble, _speed, StartPosition(clip));
                if (_playback.Speed != _speed)
                {
                    _playback.SetSpeed(Time.timeAsDouble, _speed);
                }

                var position = clip.Wrap(_playback.Position(Time.timeAsDouble));
                time = clip.FrameRate > 0f ? position / clip.FrameRate : 0.0;
                ApplyFrame(clip.Frame(position));
            }

            Sample(time);
        }

        private void OnDisable()
        {
            DestroyGraph();
            if (_animator == null)
            {
                RestoreSharedMaterial();
            }

            _material = null;
            _materials = null;
        }

        private void ApplyFrame(Vector4 frame)
        {
            foreach (var material in _materials)
            {
                _asset.ApplyFrame(material, frame);
            }
        }

        private bool UseAnimatorCopy()
        {
            _animator = _vat.GetComponentInParent<VatAnimator>();
            if (_animator == null)
            {
                return false;
            }

            _animator.enabled = false;
            _materials = _animator.Materials;
            return true;
        }

        private void RestoreSharedMaterial()
        {
            if (_vat != null && _shared != null)
            {
                _vat.sharedMaterial = _shared;
            }

            if (_material != null)
            {
                Destroy(_material);
            }
        }

        private bool HasSource()
        {
            return HasAlembic() || _source != null && _clips.Length > 0;
        }

#if VAT_ALEMBIC
        private bool HasAlembic()
        {
            return _alembic != null;
        }
#else
        private bool HasAlembic()
        {
            return false;
        }
#endif

        private void SetUpAnimation()
        {
            DestroyGraph();
            _clip = Array.Find(_clips, clip => clip != null && clip.name == Clip.Name);
            if (_clip == null)
            {
                Debug.LogError($"{name}: no source clip named '{Clip.Name}'.", this);
                return;
            }

            var legacy = _source.GetComponent<Animation>();
            if (legacy != null)
            {
                legacy.enabled = false;
            }

            if (!_clip.legacy)
            {
                CreateGraph(_source.GetComponent<Animator>());
            }
        }

        private void CreateGraph(Animator animator)
        {
            animator.applyRootMotion = false;
            _graph = PlayableGraph.Create("VatCompare");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            _playable = AnimationClipPlayable.Create(_graph, _clip);
            _playable.SetApplyFootIK(false);
            AnimationPlayableOutput.Create(_graph, "out", animator).SetSourcePlayable(_playable);
        }

        private void DestroyGraph()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }

        private float StartPosition(VatClip clip)
        {
            return _speed < 0f && !clip.IsLooping ? clip.FrameCount - 1 : 0f;
        }

        private void Sample(double time)
        {
#if VAT_ALEMBIC
            if (_alembic != null)
            {
                _alembic.UpdateImmediately((float)time);
                return;
            }
#endif

            if (_clip == null)
            {
                return;
            }

            if (_clip.legacy)
            {
                _clip.SampleAnimation(_source, (float)time);
                return;
            }

            _playable.SetTime(time);
            _graph.Evaluate();
        }
    }
}
