using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace VATyakov.Dev
{
    /// <summary>
    /// Compare scene driver (plan §5): the source SkinnedMeshRenderer and its VAT copy side by side.
    /// Step mode shows baked frame k on both. Play mode runs a VatPlayback from Time.time: the source is sampled at
    /// the time the VAT frames interpolate, so they match on baked frames and differ between them only by the
    /// interpolation error. _speed can be changed while playing. Works in Play mode and in player builds; the VAT material is a
    /// runtime copy, because MaterialPropertyBlock is not allowed (§1.6).
    /// </summary>
    public sealed class VatCompare : MonoBehaviour
    {
        [SerializeField] VatAsset _asset;
        [Tooltip("Root of the source instance: the clip is sampled on it.")]
        [SerializeField] GameObject _source;
        [SerializeField] AnimationClip _clip;
        [SerializeField] MeshRenderer _vat;
        [SerializeField] bool _step = true;
        [SerializeField, Min(0)] int _frame;
        [SerializeField] float _speed = 1f;

        Material _shared;
        Material _material;
        PlayableGraph _graph;
        AnimationClipPlayable _playable;
        VatPlayback _playback;

        public int Frame => _frame;

        public bool Step => _step;

        void OnEnable()
        {
            if (_asset == null || _source == null || _clip == null || _vat == null || !_asset.TryValidate(out _))
            {
                Debug.LogError($"{name}: VatCompare is not set up.", this);
                enabled = false;
                return;
            }

            _shared = _vat.sharedMaterial;
            _material = new Material(_shared) { name = _shared.name + " (Compare)", hideFlags = HideFlags.DontSave };
            _vat.sharedMaterial = _material;

            var legacy = _source.GetComponent<Animation>();
            if (legacy != null)
                legacy.enabled = false; // the clip is sampled explicitly

            if (!_clip.legacy)
            {
                var animator = _source.GetComponent<Animator>();
                animator.applyRootMotion = false;
                _graph = PlayableGraph.Create("VatCompare");
                _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                _playable = AnimationClipPlayable.Create(_graph, _clip);
                _playable.SetApplyFootIK(false);
                AnimationPlayableOutput.Create(_graph, "out", animator).SetSourcePlayable(_playable);
            }

            _playback = null;
        }

        void OnDisable()
        {
            if (_graph.IsValid())
                _graph.Destroy();
            if (_vat != null && _shared != null)
                _vat.sharedMaterial = _shared;
            if (_material != null)
                Destroy(_material);
            _material = null;
        }

        void Update()
        {
            var clip = _asset.Clips[0];
            double time;
            if (_step)
            {
                _frame = Mathf.Clamp(_frame, 0, clip.FrameCount - 1);
                time = clip.FrameTime(_frame);
                _material.SetVector(VatShaderIds.Frame, clip.Frame(_frame));
            }
            else
            {
                _playback ??= new VatPlayback(clip, Time.timeAsDouble, _speed, StartPosition(clip));
                if (_playback.Speed != _speed)
                    _playback.SetSpeed(Time.timeAsDouble, _speed);
                double position = clip.Wrap(_playback.Position(Time.timeAsDouble));
                time = clip.FrameRate > 0f ? position / clip.FrameRate : 0.0;
                _material.SetVector(VatShaderIds.Frame, clip.Frame(position));
            }

            Sample(time);
        }

        // A one-shot played backwards starts from its last frame.
        float StartPosition(VatClip clip) => _speed < 0f && !clip.Loop ? clip.FrameCount - 1 : 0f;

        void Sample(double time)
        {
            if (_clip.legacy)
            {
                _clip.SampleAnimation(_source, (float)time);
                return;
            }

            _playable.SetTime(time);
            _graph.Evaluate();
        }

        public void SetStep(bool step)
        {
            _step = step;
            _playback = null;
        }

        public void Advance(int frames)
        {
            if (_asset == null || _asset.Clips.Count == 0)
                return;
            int count = _asset.Clips[0].FrameCount;
            _frame = ((_frame + frames) % count + count) % count;
        }
    }
}
