using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace VATyakov.Dev
{
    /// <summary>
    /// Compare scene driver (plan §5): the source SkinnedMeshRenderer and its VAT copy side by side.
    /// Step mode shows baked frame k on both — the source is sampled at t_k, the VAT material gets rate 0 and
    /// offset k. Play mode runs both from Time.time. Works in Play mode and in player builds; the VAT material is a
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
        float _playStart;

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

            _playStart = Time.time;
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
                time = VatMath.LoopFrameTime(_frame, clip.FrameCount, clip.Length);
                _material.SetVector(VatShaderIds.ClipA, clip.State(0f, 0f, _frame)); // f = offset = k exactly
            }
            else
            {
                float elapsed = (Time.time - _playStart) * _speed;
                time = Mathf.Repeat(elapsed, clip.Length);
                _material.SetVector(VatShaderIds.ClipA, clip.State(_playStart, _speed));
            }

            Sample(time);
        }

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
            _playStart = Time.time;
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
