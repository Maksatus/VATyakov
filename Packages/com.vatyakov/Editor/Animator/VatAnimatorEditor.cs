using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    [CustomEditor(typeof(VatAnimator))]
    internal sealed class VatAnimatorEditor : ControllerInspector
    {
        protected override IEnumerable<IController> CreateControllers(VisualElement root)
        {
            yield return new VatAnimatorClipController(serializedObject, root);
            yield return new VatAnimatorFieldsController(serializedObject, root);
        }
    }
}
