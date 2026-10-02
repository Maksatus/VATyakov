using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatProblemsController : IVatController
    {
        private readonly VatProfileContext _context;
        private readonly VatProblemsContainer _container;

        public VatProblemsController(VatProfileContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatProblemsContainer>();
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
            var problems = VatBakeValidator.Validate(_context.Profile);
            _container.Box.SetMessage(string.Join("\n", problems));
            _context.Model.HasProblems.Value = problems.Count > 0;
        }
    }
}
