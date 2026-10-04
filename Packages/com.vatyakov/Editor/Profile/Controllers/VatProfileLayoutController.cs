using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatProfileLayoutController : IVatController
    {
        private readonly VatProfileLayoutContainer _container;
        private readonly IVatController[] _controllers;

        public VatProfileLayoutController(VatProfileContext context, VisualElement parent)
        {
            _container = parent.CreateContainer<VatProfileLayoutContainer>();
            _controllers = new IVatController[]
            {
                new VatSourceFieldsController(context, _container.Source),
                new VatEstimateController(context, _container.Source),
                new VatLoopHintController(context, _container.Source),
                new VatProblemsController(context, _container.Actions),
                new VatBakeButtonController(context, _container.Actions),
                new VatResultController(context, _container.Result),
                new VatBoundFieldsController<VatMaterialFieldsContainer>(context, _container.Material),
            };
        }

        public void Deactivate()
        {
            _controllers.Deactivate();
            _container.DestroyView();
        }

        public void Activate()
        {
            _controllers.Activate();
        }
    }
}
