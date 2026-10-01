using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    [CustomEditor(typeof(VatBakeProfile))]
    sealed class VatBakeProfileEditor : ControllerInspector
    {
        protected override IEnumerable<IController> CreateControllers(VisualElement root)
        {
            var context = new VatProfileContext(serializedObject);
            var layout = root.CreateContainer<VatProfileLayoutContainer>();

            yield return new VatProfileChangeController(context, root);
            yield return new VatBoundFieldsController<VatSourceFieldsContainer>(context, layout.Source);
            yield return new VatEstimateController(context, layout.Source);
            yield return new VatProblemsController(context, layout.Actions);
            yield return new VatBakeButtonController(context, layout.Actions);
            yield return new VatResultController(context, layout.Result);
            yield return new VatBoundFieldsController<VatMaterialFieldsContainer>(context, layout.Material);
            yield return new VatPrefabController(context, layout.Prefab);
        }
    }
}
