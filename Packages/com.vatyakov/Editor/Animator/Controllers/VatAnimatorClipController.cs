using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAnimatorClipController : IController
    {
        private readonly SerializedObject _serializedObject;
        private readonly SerializedProperty _asset;
        private readonly SerializedProperty _clip;
        private readonly VatAnimatorClipContainer _container;

        public VatAnimatorClipController(SerializedObject serializedObject, VisualElement parent)
        {
            _serializedObject = serializedObject;
            _asset = serializedObject.FindProperty("_asset");
            _clip = serializedObject.FindProperty("_clip");
            _container = parent.CreateContainer<VatAnimatorClipContainer>();
        }

        public void Deactivate()
        {
            _container.Clip.UnregisterValueChangedCallback(OnChanged);
            _container.Root.Unbind();
            _container.DestroyView();
        }

        public void Activate()
        {
            _container.Root.Bind(_serializedObject);
            _container.Clip.RegisterValueChangedCallback(OnChanged);
            _container.Root.TrackPropertyValue(_asset, _ => Refresh());
            _container.Root.TrackPropertyValue(_clip, _ => Refresh());
            Refresh();
        }

        private void Refresh()
        {
            _serializedObject.Update();
            _container.Show(_asset.objectReferenceValue as VatAsset, _clip.stringValue);
        }

        private void OnChanged(ChangeEvent<string> _)
        {
            _serializedObject.Update();
            _clip.stringValue = _container.Clip.index == 0 ? string.Empty : _container.Clip.value;
            _serializedObject.ApplyModifiedProperties();
        }
    }
}
