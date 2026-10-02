using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    [CustomEditor(typeof(VatAsset))]
    internal sealed class VatAssetEditor : VatControllerInspector
    {
        protected override IEnumerable<IVatController> CreateControllers(VisualElement root)
        {
            var context = new VatAssetContext((VatAsset)target);
            var isValid = context.Asset.TryValidate(out _);

            yield return isValid ? new VatAssetOverviewController(context, root) : new VatAssetErrorController(context, root);
            if (isValid)
            {
                yield return new VatAssetPrecisionController(context, root);
            }

            yield return new VatAssetProfileController(context, root);
            if (isValid)
            {
                yield return new VatAssetContentsController(context, root);
            }
        }
    }
}
