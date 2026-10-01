using UnityEngine;

namespace VATyakov.Editor
{
    // One SkinnedMeshRenderer, one clip, Vertex mode; several clips arrive in 1.6. Tooltips are for artists, hence in Russian.
    [CreateAssetMenu(menuName = "VATyakov/VAT Bake Profile", fileName = "VatBakeProfile", order = 400)]
    public sealed class VatBakeProfile : ScriptableObject
    {
        [Tooltip("SkinnedMeshRenderer из префаба или модели. Позиции запекаются в пространстве корня префаба.")]
        [SerializeField] SkinnedMeshRenderer _source;

        [Tooltip("Анимация, которая запекается в текстуру.")]
        [SerializeField] AnimationClip _clip;

        [Tooltip("Включено — клип играет по кругу, последний кадр не повторяет первый. " +
                 "Выключено — клип играет один раз и останавливается на последнем кадре.")]
        [SerializeField] bool _loop = true;

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
