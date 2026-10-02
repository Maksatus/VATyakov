using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAssetPrecisionController : IVatController
    {
        private const string ErrorHint = "Max position error the shader reconstructs, fp16 sampling included.";

        private readonly VatAssetContext _context;
        private readonly VatAssetPrecisionContainer _container;

        public VatAssetPrecisionController(VatAssetContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatAssetPrecisionContainer>();
        }

        public void Deactivate()
        {
            _container.DestroyView();
        }

        public void Activate()
        {
            var precision = _context.Asset.Precision;
            _container.Error.Set(VatText.Millimeters(precision.Error), ErrorHint);
            _container.Drift.Set(VatText.Meters(precision.MaxDrift), VatText.DriftTravel(precision.MaxDrift));
        }
    }
}
