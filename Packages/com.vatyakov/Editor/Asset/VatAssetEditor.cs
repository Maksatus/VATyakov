using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    // Read-only: everything in the asset is written by the baker.
    [CustomEditor(typeof(VatAsset))]
    sealed class VatAssetEditor : ControllerInspector
    {
        protected override IEnumerable<IController> CreateControllers(VisualElement root)
        {
            var asset = (VatAsset)target;
            bool valid = asset.TryValidate(out _);

            yield return valid ? new VatAssetOverviewController(asset, root) : new VatAssetErrorController(asset, root);
            if (valid)
                yield return new VatAssetPrecisionController(asset, root);
            yield return new VatAssetProfileController(asset, root);
            if (valid)
                yield return new VatAssetContentsController(asset, root);
        }
    }
}
