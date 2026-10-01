using UnityEditor;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatPrefabController : IController
    {
        readonly VatProfileContext _context;
        readonly VatPrefabContainer _container;

        public VatPrefabController(VatProfileContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatPrefabContainer>();
        }

        public void Activate()
        {
            _context.Model.Changed.OnCall += Refresh;
            _container.Button.clicked += CreatePrefab;
            Refresh();
        }

        public void Deactivate()
        {
            _context.Model.Changed.OnCall -= Refresh;
            _container.Button.clicked -= CreatePrefab;
            _container.DestroyView();
        }

        void Refresh() => _container.Show(_context.Profile.Prefab, _context.Profile.IsBaked);

        void CreatePrefab()
        {
            VatBakeDialog.Run(() => EditorGUIUtility.PingObject(VatBaker.CreatePrefab(_context.Profile)));
            _context.Refresh();
        }
    }
}
