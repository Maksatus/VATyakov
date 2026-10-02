using UnityEditor;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatPrefabController : IVatController
    {
        private const string NotBakedHint = "Bake the profile first.";

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
            _container.Button.clicked -= OnCreateClicked;
            _container.DestroyView();
        }

        public void Activate()
        {
            _context.Model.Changed.OnCall += Refresh;
            _container.Button.clicked += OnCreateClicked;
            Refresh();
        }

        private void Refresh()
        {
            var prefab = _context.Profile.Prefab;
            var isBaked = _context.Profile.IsBaked;
            _container.Prefab.Set(prefab);
            _container.Button.text = prefab != null ? "Update Prefab" : "Create Prefab";
            _container.Button.SetEnabled(isBaked);
            _container.Button.tooltip = isBaked ? string.Empty : NotBakedHint;
        }

        private void OnCreateClicked()
        {
            VatBakeDialog.Run(() => EditorGUIUtility.PingObject(VatBaker.CreatePrefab(_context.Profile)));
            _context.Refresh();
        }
    }
}
