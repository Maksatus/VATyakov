using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatBakeButtonController : IController
    {
        readonly VatProfileContext _context;
        readonly VatBakeButtonContainer _container;

        public VatBakeButtonController(VatProfileContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatBakeButtonContainer>();
        }

        VatProfileModel Model => _context.Model;

        public void Activate()
        {
            Model.HasProblems.Changed += OnStateChanged;
            Model.EstimateFits.Changed += OnStateChanged;
            _container.Button.clicked += Bake;
            Refresh();
        }

        public void Deactivate()
        {
            Model.HasProblems.Changed -= OnStateChanged;
            Model.EstimateFits.Changed -= OnStateChanged;
            _container.Button.clicked -= Bake;
            _container.DestroyView();
        }

        void OnStateChanged(bool _) => Refresh();

        void Refresh() => _container.Button.SetEnabled(!Model.HasProblems.Value && Model.EstimateFits.Value);

        void Bake()
        {
            VatBakeDialog.Run(() => VatBaker.Bake(_context.Profile));
            _context.Refresh();
        }
    }
}
