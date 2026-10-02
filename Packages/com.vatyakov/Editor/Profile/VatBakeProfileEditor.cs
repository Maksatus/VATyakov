using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    [CustomEditor(typeof(VatBakeProfile))]
    internal sealed class VatBakeProfileEditor : VatControllerInspector
    {
        protected override IEnumerable<IVatController> CreateControllers(VisualElement root)
        {
            var context = new VatProfileContext(serializedObject);
            yield return new VatProfileChangeController(context, root);
            yield return new VatProfileLayoutController(context, root);
        }
    }
}
