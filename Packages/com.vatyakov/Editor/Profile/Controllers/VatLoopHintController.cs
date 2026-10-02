using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatLoopHintController : IController
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

            var loop = _context.Profile.Loop;
            var closed = probe.LoopGap <= VatLoopGap.MaxLoopGap;
            _container.Box.messageType = closed == loop ? HelpBoxMessageType.Info : HelpBoxMessageType.Warning;
            _container.Box.SetMessage(VatText.LoopHint(probe.LoopGap, closed, loop));
        }

        private static VatAlembicProbe Probe(VatBakeProfile profile)
        {
            return profile.Kind == VatSourceKind.Alembic && VatAlembic.IsInstalled && profile.Alembic != null
                ? VatAlembicProbe.For(profile.Alembic)
                : null;
        }
    }
}
