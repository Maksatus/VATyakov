using UnityEngine;

namespace Kefir.Vat.Editor
{
    /// <summary>
    /// Bake settings (§4). Lives in the editor assembly and never reaches a build.
    /// Subversion 1.1: one SkinnedMeshRenderer, one clip, Vertex mode, positions only.
    /// </summary>
    [CreateAssetMenu(menuName = "Kefir/VAT Bake Profile", fileName = "VatBakeProfile", order = 400)]
    public sealed class VatBakeProfile : ScriptableObject
    {
        [Tooltip("SkinnedMeshRenderer inside a prefab or model. Positions are baked in the space of the prefab root.")]
        [SerializeField] SkinnedMeshRenderer _source;

        [SerializeField] AnimationClip _clip;

        [Tooltip("Bake frame rate. A loop clip gets F = max(1, round(length · fps)) frames.")]
        [SerializeField, Min(0.001f)] float _fps = 30f;

        [Header("Output")]
        [Tooltip("Baked asset. Created next to the profile on the first bake and updated in place afterwards.")]
        [SerializeField] VatAsset _asset;

        [Tooltip("Template material: the baker writes textures, layout and the clip into it. Created from Shader when empty.")]
        [SerializeField] Material _material;

        [Tooltip("Shader for a new template material.")]
        [SerializeField] Shader _shader;

        [Tooltip("Create a prefab (MeshFilter + MeshRenderer) on the first bake. Rebakes keep its references.")]
        [SerializeField] bool _createPrefab = true;

        [SerializeField] GameObject _prefab;

        public SkinnedMeshRenderer Source { get => _source; set => _source = value; }

        public AnimationClip Clip { get => _clip; set => _clip = value; }

        public float Fps { get => _fps; set => _fps = value; }

        public VatAsset Asset { get => _asset; set => _asset = value; }

        public Material Material { get => _material; set => _material = value; }

        public Shader Shader { get => _shader; set => _shader = value; }

        public bool CreatePrefab { get => _createPrefab; set => _createPrefab = value; }

        public GameObject Prefab { get => _prefab; set => _prefab = value; }

        void Reset()
        {
            _shader = Shader.Find(VatBaker.DefaultShaderName);
        }
    }
}
