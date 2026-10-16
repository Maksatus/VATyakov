using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatOutdatedController : IVatController
    {
        private const string OutdatedMessage = "The source or the settings changed after the last bake: bake again to apply them.";

        private readonly VatProfileContext _context;
        private readonly VatOutdatedContainer _container;

        public VatOutdatedController(VatProfileContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatOutdatedContainer>();
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
            var profile = _context.Profile;
            var isOutdated = profile.Asset != null && VatSourceHash.IsOutdated(profile, profile.Asset, _context.Model.HasProblems.Value);
            _container.Outdated.SetMessage(isOutdated ? OutdatedMessage : null);
        }
    }
}
