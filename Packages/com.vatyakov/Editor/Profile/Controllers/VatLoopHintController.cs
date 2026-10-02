using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatLoopHintController : IVatController
    {
        private readonly VatProfileContext _context;
        private readonly VatLoopHintContainer _container;

        public VatLoopHintController(VatProfileContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatLoopHintContainer>();
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
            var probe = Probe(_context.Profile);
            if (probe == null || probe.Problem != null)
            {
                _container.Box.SetMessage(null);
                return;
            }

            var isLooping = _context.Profile.IsLooping;
            var isClosed = probe.LoopGap <= VatLoopGap.Max;
            _container.Box.messageType = isClosed == isLooping ? HelpBoxMessageType.Info : HelpBoxMessageType.Warning;
            _container.Box.SetMessage(VatText.LoopHint(probe.LoopGap, isClosed, isLooping));
        }

        private static VatAlembicProbe Probe(VatBakeProfile profile)
        {
            return profile.Kind == VatSourceKind.Alembic && VatAlembic.IsInstalled && profile.Alembic != null
                ? VatAlembicProbe.For(profile.Alembic)
                : null;
        }
    }
}
