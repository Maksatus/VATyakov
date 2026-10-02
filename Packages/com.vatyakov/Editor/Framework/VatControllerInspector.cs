using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal abstract class VatControllerInspector : UnityEditor.Editor
    {
        private IVatController[] _controllers = Array.Empty<IVatController>();

        public override VisualElement CreateInspectorGUI()
        {
            DeactivateControllers();
            var root = VatUi.Root();
            _controllers = CreateControllers(root).ToArray();
            _controllers.Activate();
            return root;
        }

        protected abstract IEnumerable<IVatController> CreateControllers(VisualElement root);

        private void OnDisable()
        {
            DeactivateControllers();
        }

        private void DeactivateControllers()
        {
            _controllers.Deactivate();
            _controllers = Array.Empty<IVatController>();
        }
    }
}
