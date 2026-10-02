using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    [CustomEditor(typeof(VatAnimator))]
    internal sealed class VatAnimatorEditor : VatControllerInspector
    {
        protected override IEnumerable<IVatController> CreateControllers(VisualElement root)
        {
            var context = new VatAnimatorContext(serializedObject);
            yield return new VatAnimatorClipController(context, root);
            yield return new VatAnimatorFieldsController(context, root);
        }
    }
}
