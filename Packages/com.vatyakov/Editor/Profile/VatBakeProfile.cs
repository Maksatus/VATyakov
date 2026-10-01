using UnityEngine;

namespace VATyakov.Editor
{
    // One SkinnedMeshRenderer with one clip or one Alembic (1.4), Vertex mode; several clips arrive in 1.6.
    [CreateAssetMenu(menuName = "VATyakov/VAT Bake Profile", fileName = "VatBakeProfile", order = 400)]
    public sealed class VatBakeProfile : ScriptableObject
    {
        [Tooltip("Skinned Mesh Renderer: a SkinnedMeshRenderer with a clip. Alembic: an .abc with constant topology (cloth, soft body, liquid).")]
        [SerializeField] VatSourceKind _kind;

        [Tooltip("SkinnedMeshRenderer of a prefab or model. Positions are baked in the prefab root space.")]
        [SerializeField] SkinnedMeshRenderer _source;

        [Tooltip("Animation clip baked into the textures.")]
        [SerializeField] AnimationClip _clip;

        [Tooltip(".abc from the project. The whole importer Time Range is baked; positions are in the .abc root space. " +
                 "Requires com.unity.formats.alembic 2.4.5 or newer.")]
        [SerializeField] GameObject _alembic;

        [Tooltip("On: the clip loops, the last frame does not repeat the first. " +
                 "Off: the clip plays once and stops on the last frame.")]
        [SerializeField] bool _loop = true;

        [Tooltip("Baked frames per second of animation. Higher is smoother and costs more memory.")]
        [SerializeField, Min(0.001f)] float _fps = 30f;

        [HideInInspector, SerializeField] VatAsset _asset;

        [Tooltip("Material the baker writes the textures and clip into. Created next to the profile when empty.")]
        [SerializeField] Material _material;

        [Tooltip("Shader of a new template material.")]
        [SerializeField] Shader _shader;

        [Tooltip("Test prefab for the Compare scene. Created by its button, never by a bake.")]
        [SerializeField] GameObject _prefab;

        public VatSourceKind Kind { get => _kind; set => _kind = value; }

        public SkinnedMeshRenderer Source { get => _source; set => _source = value; }

        public AnimationClip Clip { get => _clip; set => _clip = value; }

        public GameObject Alembic { get => _alembic; set => _alembic = value; }

        public bool Loop { get => _loop; set => _loop = value; }

        public float Fps { get => _fps; set => _fps = value; }

        public VatAsset Asset { get => _asset; set => _asset = value; }

        public Material Material { get => _material; set => _material = value; }

        public Shader Shader { get => _shader; set => _shader = value; }

        public GameObject Prefab { get => _prefab; set => _prefab = value; }

        public bool IsBaked => _asset != null && _asset.TryValidate(out _) && _material != null;

        void Reset()
        {
            _shader = Shader.Find(VatBaker.DefaultShaderName);
        }
    }
}
