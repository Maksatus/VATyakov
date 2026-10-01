using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    // §2.2: loop detection is only a hint; Alembic only, a skinned clip has its own loop settings.
    sealed class VatLoopHintController : IController
    {
        readonly VatProfileContext _context;
        readonly VatLoopHintContainer _container;

        public VatLoopHintController(VatProfileContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatLoopHintContainer>();
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
            var probe = Probe(_context.Profile);
            if (probe == null || probe.Problem != null)
            {
                _container.Box.SetMessage(null);
                return;
            }
            bool loop = _context.Profile.Loop;
            bool closed = probe.LoopGap <= VatLoopGap.MaxLoopGap;
            _container.Box.messageType = closed == loop ? HelpBoxMessageType.Info : HelpBoxMessageType.Warning;
            _container.Box.SetMessage(VatText.LoopHint(probe.LoopGap, closed, loop));
        }

        static VatAlembicProbe Probe(VatBakeProfile profile) =>
            profile.Kind == VatSourceKind.Alembic && VatAlembic.IsInstalled && profile.Alembic != null
                ? VatAlembicProbe.For(profile.Alembic)
                : null;
    }
}
