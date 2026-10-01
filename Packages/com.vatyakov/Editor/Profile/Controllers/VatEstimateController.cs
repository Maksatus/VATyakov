using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatEstimateController : IController
    {
        readonly VatProfileContext _context;
        readonly VatEstimateContainer _container;

        public VatEstimateController(VatProfileContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatEstimateContainer>();
        }

        public void Activate()
        {
            _context.Model.Changed.OnCall += Refresh;
            Refresh();
        }

        public void Deactivate()
        {
            _context.Model.Changed.OnCall -= Refresh;
            _container.DestroyView();
        }

        void Refresh()
        {
            var estimate = VatBakeEstimate.For(_context.Profile);
            _container.Show(estimate);
            _context.Model.EstimateFits.Value = estimate.Fits;
        }
    }
}
