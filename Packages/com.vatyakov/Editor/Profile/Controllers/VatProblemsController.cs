using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatProblemsController : IController
    {
        readonly VatProfileContext _context;
        readonly VatProblemsContainer _container;

        public VatProblemsController(VatProfileContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatProblemsContainer>();
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
            var problems = VatBakeValidator.Validate(_context.Profile);
            _container.Box.SetMessage(string.Join("\n", problems));
            _context.Model.HasProblems.Value = problems.Count > 0;
        }
    }
}
