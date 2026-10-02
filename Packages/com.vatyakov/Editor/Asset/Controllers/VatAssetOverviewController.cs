using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAssetOverviewController : IVatController
    {
        private readonly VatAssetContext _context;
        private readonly VatAssetOverviewContainer _container;

        public VatAssetOverviewController(VatAssetContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatAssetOverviewContainer>();
        }

        public void Deactivate()
        {
            _container.DestroyView();
        }

        public void Activate()
        {
            VatAssetSummary.Show(_container.Summary, _context.Asset);
        }
    }
}
