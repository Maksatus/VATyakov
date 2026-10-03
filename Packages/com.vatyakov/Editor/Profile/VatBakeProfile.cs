using System;
using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    [CreateAssetMenu(menuName = "VATyakov/VAT Bake Profile", fileName = "vat_bake_profile", order = VatBakeProfile.MenuOrder)]
    public sealed class VatBakeProfile : ScriptableObject
    {
        public const int MenuOrder = 400;

        [Tooltip("Skinned Mesh Renderer: a renderer with clips. Alembic: an .abc with constant topology (cloth, soft body, liquid).")]
        [SerializeField]
        private VatSourceKind _kind;

        [Tooltip("Skinned Mesh Renderer of a prefab or model. Positions are baked in the prefab root space.")]
        [SerializeField]
        private SkinnedMeshRenderer _source;

        [Tooltip("Animation clips baked one under another into one texture. Transitions work only between clips of one VAT asset.")]
        [SerializeField]
        private AnimationClip[] _clips = Array.Empty<AnimationClip>();

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

        [HideInInspector]
        [SerializeField]
        private VatAsset _asset;

        [Tooltip("Material the baker writes the textures into. Created next to the profile when empty. " +
                 "A rebake keeps the clip it shows when the clip is still there.")]
        [SerializeField]
        private Material _material;

        [Tooltip("Shader of a new template material.")]
        [SerializeField]
        private Shader _shader;

        [Tooltip("Test prefab for the Compare scene. Created by its button, never by a bake.")]
        [SerializeField]
        private GameObject _prefab;

        public VatSourceKind Kind { get => _kind; set => _kind = value; }
        public SkinnedMeshRenderer Source { get => _source; set => _source = value; }
        public IReadOnlyList<AnimationClip> Clips => _clips;
        public GameObject Alembic { get => _alembic; set => _alembic = value; }
        public bool IsLooping { get => _isLooping; set => _isLooping = value; }
        public float Fps { get => _fps; set => _fps = value; }
        public VatAsset Asset { get => _asset; set => _asset = value; }
        public Material Material { get => _material; set => _material = value; }
        public Shader Shader { get => _shader; set => _shader = value; }
        public GameObject Prefab { get => _prefab; set => _prefab = value; }
        public bool IsBaked => _asset != null && _asset.TryValidate(out _) && _material != null;

        public void SetClips(params AnimationClip[] clips)
        {
            _clips = (AnimationClip[])clips.Clone();
        }

        private void Reset()
        {
            _shader = Shader.Find(VatBaker.DefaultShaderName);
        }
    }
}
