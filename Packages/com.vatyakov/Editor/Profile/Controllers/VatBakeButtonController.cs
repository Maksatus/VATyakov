using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatBakeButtonController : IController
    {
        private readonly VatProfileContext _context;
        private readonly VatBakeButtonContainer _container;

        private VatProfileModel Model => _context.Model;

        public VatBakeButtonController(VatProfileContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatBakeButtonContainer>();
        }

        public void Deactivate()
        {
            Model.HasProblems.Changed -= OnStateChanged;
            Model.EstimateFits.Changed -= OnStateChanged;
            _container.Button.clicked -= Bake;
            _container.DestroyView();
        }

        public void Activate()
        {
            Model.HasProblems.Changed += OnStateChanged;
            Model.EstimateFits.Changed += OnStateChanged;
            _container.Button.clicked += Bake;
            Refresh();
        }

        private void OnStateChanged(bool _)
        {
            Refresh();
        }

        private void Refresh()
        {
            _container.Button.SetEnabled(!Model.HasProblems.Value && Model.EstimateFits.Value);
        }

        private void Bake()
        {
            VatBakeDialog.Run(() => VatBaker.Bake(_context.Profile));
            _context.Refresh();
        }
    }
}
