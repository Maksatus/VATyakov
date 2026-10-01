using UnityEditor;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatAssetDefaultClipController : IController
    {
        readonly VatAsset _asset;
        readonly VatAssetDefaultClipContainer _container;

        public VatAssetDefaultClipController(VatAsset asset, VisualElement parent)
        {
            _asset = asset;
            _container = parent.CreateContainer<VatAssetDefaultClipContainer>();
        }

        public void Activate()
        {
            _container.Clip.RegisterValueChangedCallback(OnChanged);
            Undo.undoRedoPerformed += Refresh;
            Refresh();
        }

        public void Deactivate()
        {
            Undo.undoRedoPerformed -= Refresh;
            _container.Clip.UnregisterValueChangedCallback(OnChanged);
            _container.DestroyView();
        }

        void Refresh() => _container.Show(_asset.Clips, _asset.DefaultClipIndex);

        void OnChanged(ChangeEvent<string> _) => VatDefaultClip.Set(_asset, _container.Clip.index);
    }
}
