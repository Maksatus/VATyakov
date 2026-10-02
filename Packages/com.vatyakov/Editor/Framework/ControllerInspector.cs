using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal abstract class ControllerInspector : UnityEditor.Editor
    {
        private IController[] _controllers = Array.Empty<IController>();

        public override VisualElement CreateInspectorGUI()
        {
            DeactivateControllers();
            var root = VatUi.Root();
            _controllers = CreateControllers(root).ToArray();
            _controllers.Activate();
            return root;
        }

        protected abstract IEnumerable<IController> CreateControllers(VisualElement root);

        protected virtual void OnDisable()
        {
            DeactivateControllers();
        }

        private void DeactivateControllers()
        {
            _controllers.Deactivate();
            _controllers = Array.Empty<IController>();
        }
    }
}
