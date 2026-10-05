using System;
using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    [CreateAssetMenu(menuName = "VATyakov/VAT Bake Profile", fileName = "vat_bake_profile", order = VatBakeProfile.MenuOrder)]
    public sealed class VatBakeProfile : ScriptableObject
    {
        public const int MenuOrder = 400;

        private const float MillimetersPerMeter = 1000f;

        [Tooltip("Skinned Mesh Renderer: a renderer with clips. Alembic: an .abc with constant topology (cloth, soft body, liquid) " +
                 "or rigid pieces on xform nodes (destruction).")]
        [SerializeField]
        private VatSourceKind _kind;

        [Tooltip("Skinned Mesh Renderer of a prefab or model. Positions are baked in the prefab root space.")]
        [SerializeField]
        private SkinnedMeshRenderer _source;

        [Tooltip("Vertex: positions and rotations of every vertex per frame, any deformation. " +
                 "Bone: rotation, uniform scale and offset of every bone per frame, two bones per vertex, much less memory. " +
                 "A bone that scales non-uniformly or a blend shape bakes the asset as Vertex. Skinned Mesh Renderer only. " +
                 "Rigid: rotation, uniform scale and offset of every piece per frame, pieces on xform nodes of an Alembic or rigid parts of a deforming mesh (destruction), up to 2048 pieces.")]
        [SerializeField]
        private VatMode _mode;

        [Tooltip("Animation clips baked one under another into one texture. Transitions work only between clips of one VAT asset.")]
        [SerializeField]
        private AnimationClip[] _clips = Array.Empty<AnimationClip>();

        [Tooltip("Bone mode only. More meshes of the same rig on the same bone texture: LOD meshes, other skins, equipment. " +
                 "The asset gets one mesh per renderer, in this order.")]
        [SerializeField]
        private VatExtraRenderer[] _extraRenderers = Array.Empty<VatExtraRenderer>();

        [Tooltip(".abc from the project. The whole importer Time Range is baked; positions are in the .abc root space. " +
                 "Requires com.unity.formats.alembic 2.4.5 or newer.")]
        [SerializeField]
        private GameObject _alembic;

        [Tooltip("For every clip. On: the clip loops, the last frame does not repeat the first. " +
                 "Off: the clip plays once and stops on the last frame.")]
        [SerializeField]
        private bool _isLooping = true;

        [Tooltip("Baked frames per second of animation, for every clip. Higher is smoother and costs more memory.")]
        [Min(0.001f)]
        [SerializeField]
        private float _fps = 30f;

        [Tooltip("Millimeters. Positions take one byte per axis within the bounds of the animation when their error stays within this value, " +
                 "half the memory of half floats; otherwise they stay in half floats. 0 always keeps half floats.")]
        [Min(0f)]
        [SerializeField]
        private float _maxPositionError = VatPositionEncoding.ByteTolerance * MillimetersPerMeter;

        [HideInInspector]
        [SerializeField]
        private VatAsset _asset;

        [Tooltip("Material the baker writes the textures into. Created next to the profile when empty. " +
                 "A rebake keeps the clip it shows when the clip is still there.")]
        [SerializeField]
        private Material _material;

        [Tooltip("Shader of a new template material. A template whose shader does not fit the baked mode gets the default shader of that mode.")]
        [SerializeField]
        private Shader _shader;

        public VatSourceKind Kind { get => _kind; set => _kind = value; }
        public VatMode Mode { get => _mode; set => _mode = value; }
        public SkinnedMeshRenderer Source { get => _source; set => _source = value; }
        public IReadOnlyList<AnimationClip> Clips => _clips;
        public IReadOnlyList<VatExtraRenderer> ExtraRenderers => _extraRenderers;
        public GameObject Alembic { get => _alembic; set => _alembic = value; }
        public bool IsLooping { get => _isLooping; set => _isLooping = value; }
        public float Fps { get => _fps; set => _fps = value; }
        public float MaxPositionError { get => _maxPositionError / MillimetersPerMeter; set => _maxPositionError = value * MillimetersPerMeter; }
        public VatAsset Asset { get => _asset; set => _asset = value; }
        public Material Material { get => _material; set => _material = value; }
        public Shader Shader { get => _shader; set => _shader = value; }
        public bool IsBone => _kind == VatSourceKind.Skinned && _mode == VatMode.Bone;
        public bool IsRigid => _kind == VatSourceKind.Alembic && _mode == VatMode.Rigid;
        public bool IsBaked => _asset != null && _asset.TryValidate(out _) && _material != null;

        public void SetClips(params AnimationClip[] clips)
        {
            _clips = (AnimationClip[])clips.Clone();
        }

        public void SetExtraRenderers(params VatExtraRenderer[] extraRenderers)
        {
            _extraRenderers = (VatExtraRenderer[])extraRenderers.Clone();
        }

        private void Reset()
        {
            _shader = Shader.Find(VatBaker.DefaultShaderName);
        }
    }
}
