using UnityEditor;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatPrefabController : IController
    {
        private readonly VatProfileContext _context;
        private readonly VatPrefabContainer _container;

        public VatPrefabController(VatProfileContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatPrefabContainer>();
        }

        public void Deactivate()
        {
            _context.Model.Changed.OnCall -= Refresh;
            _container.Button.clicked -= CreatePrefab;
            _container.DestroyView();
        }

        public void Activate()
        {
            _context.Model.Changed.OnCall += Refresh;
            _container.Button.clicked += CreatePrefab;
            Refresh();
        }

        private void Refresh()
        {
            _container.Show(_context.Profile.Prefab, _context.Profile.IsBaked);
        }

        private void CreatePrefab()
        {
            VatBakeDialog.Run(() => EditorGUIUtility.PingObject(VatBaker.CreatePrefab(_context.Profile)));
            _context.Refresh();
        }
    }
}
