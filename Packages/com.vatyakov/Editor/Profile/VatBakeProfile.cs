using UnityEngine;

namespace VATyakov.Editor
{
    // 1.1: one SkinnedMeshRenderer, one clip, Vertex mode. Tooltips are for artists, hence in Russian.
    [CreateAssetMenu(menuName = "VATyakov/VAT Bake Profile", fileName = "VatBakeProfile", order = 400)]
    public sealed class VatBakeProfile : ScriptableObject
    {
        [Tooltip("SkinnedMeshRenderer из префаба или модели. Позиции запекаются в пространстве корня префаба.")]
        [SerializeField] SkinnedMeshRenderer _source;

        [Tooltip("Анимация, которая запекается в текстуру. Играет по кругу.")]
        [SerializeField] AnimationClip _clip;

        [Tooltip("Сколько кадров в секунду анимации сохранить. Больше — плавнее и тяжелее по памяти.")]
        [SerializeField, Min(0.001f)] float _fps = 30f;

        [HideInInspector, SerializeField] VatAsset _asset;

        [Tooltip("Материал, в который бейкер пишет текстуру и клип. Если пусто — создаётся рядом с профилем.")]
        [SerializeField] Material _material;

        [Tooltip("Шейдер для нового материала-шаблона.")]
        [SerializeField] Shader _shader;

        [Tooltip("Тестовый префаб для сцены Compare. Создаётся кнопкой, бейк его не создаёт.")]
        [SerializeField] GameObject _prefab;

        public SkinnedMeshRenderer Source { get => _source; set => _source = value; }

        public AnimationClip Clip { get => _clip; set => _clip = value; }

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
