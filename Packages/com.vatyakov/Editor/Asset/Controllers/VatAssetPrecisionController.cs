using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAssetPrecisionController : IVatController
    {
        private const string ErrorHint = "Max position error the shader reconstructs, fp16 sampling included.";
        private const string BoneErrorHint =
            "Max position error the shader reconstructs on the baked frames against Skin Weights = 2 Bones skinning, half precision included.";
        private const string RigidErrorHint = "Max position error the shader reconstructs on the visible baked frames against the Alembic, half precision included.";

        private readonly VatAssetContext _context;
        private readonly VatAssetPrecisionContainer _container;

        public VatAssetPrecisionController(VatAssetContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatAssetPrecisionContainer>();
        }

        public void Deactivate()
        {
            _container.DestroyView();
        }

        public void Activate()
        {
            var asset = _context.Asset;
            var precision = asset.Precision;
            var isVertex = asset.Mode == VatMode.Vertex;
            _container.Positions.SetVisible(isVertex);
            _container.Drift.SetVisible(isVertex);
            _container.Positions.Set(VatText.PositionFormat(asset.PositionFormat), VatText.PositionFormatHint(asset.PositionFormat, precision));
            _container.Error.Set(VatText.Millimeters(precision.Error), isVertex ? ErrorHint : asset.Mode == VatMode.Rigid ? RigidErrorHint : BoneErrorHint);
            _container.Drift.Set(VatText.Meters(precision.MaxDrift), VatText.DriftTravel(precision.MaxDrift));
        }
    }
}
