using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatEstimateController : IVatController
    {
        private const string ErrorClass = "vat-hint--error";

        private readonly VatProfileContext _context;
        private readonly VatEstimateContainer _container;

        public VatEstimateController(VatProfileContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatEstimateContainer>();
        }

        public void Deactivate()
        {
            _context.Model.Changed.OnCall -= Refresh;
            _container.DestroyView();
        }

        public void Activate()
        {
            _context.Model.Changed.OnCall += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            var estimate = VatBakeEstimate.For(_context.Profile);
            Show(estimate);
            _context.Model.IsEstimateWithinLimits.Value = estimate.IsWithinLimits;
        }

        private void Show(VatBakeEstimate estimate)
        {
            _container.SetVisible(!estimate.IsEmpty);
            if (estimate.IsEmpty)
            {
                return;
            }

            _container.Text.text = estimate.IsWithinLimits ? VatText.Estimate(estimate) : estimate.Error;
            _container.Text.EnableInClassList(ErrorClass, !estimate.IsWithinLimits);
        }
    }
}
