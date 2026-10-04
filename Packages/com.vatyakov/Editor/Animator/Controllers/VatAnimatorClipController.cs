using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAnimatorClipController : IVatController
    {
        private const string FirstChoice = "First";

        private readonly VatAnimatorContext _context;
        private readonly VatAnimatorClipContainer _container;

        private SerializedProperty _asset;
        private SerializedProperty _clip;

        private SerializedObject SerializedObject => _context.SerializedObject;

        public VatAnimatorClipController(VatAnimatorContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatAnimatorClipContainer>();
        }

        public void Deactivate()
        {
            _container.Clip.UnregisterValueChangedCallback(OnClipChanged);
            _container.Root.Unbind();
            _container.DestroyView();
        }

        public void Activate()
        {
            _asset = SerializedObject.FindProperty(VatAnimatorContext.AssetPath);
            _clip = SerializedObject.FindProperty(VatAnimatorContext.ClipPath);
            _container.Root.Bind(SerializedObject);
            _container.Root.TrackPropertyValue(_asset, OnPropertyChanged);
            _container.Root.TrackPropertyValue(_clip, OnPropertyChanged);
            _container.Clip.RegisterValueChangedCallback(OnClipChanged);
            Refresh();
        }

        private void OnPropertyChanged(SerializedProperty _)
        {
            Refresh();
        }

        private void OnClipChanged(ChangeEvent<string> _)
        {
            SerializedObject.Update();
            _clip.stringValue = _container.Clip.index == 0 ? string.Empty : _container.Clip.value;
            SerializedObject.ApplyModifiedProperties();
        }

        private void Refresh()
        {
            SerializedObject.Update();
            var asset = _asset.objectReferenceValue as VatAsset;
            var clipName = _clip.stringValue;
            var choices = Choices(asset, clipName);
            _container.Clip.choices = choices;
            _container.Clip.SetValueWithoutNotify(string.IsNullOrEmpty(clipName) ? choices[0] : clipName);
            _container.Clip.SetEnabled(asset != null);
            _container.Problem.SetMessage(Problem(asset, clipName));
        }

        private static string Problem(VatAsset asset, string clipName)
        {
            if (asset == null)
            {
                return "No VAT asset: Play throws.";
            }

            if (!asset.TryValidate(out var error))
            {
                return error;
            }

            return string.IsNullOrEmpty(clipName) || asset.IndexOf(clipName) >= 0 ? null : $"Clip '{clipName}' is not in '{asset.name}': Play On Enable throws.";
        }

        private static List<string> Choices(VatAsset asset, string clipName)
        {
            var choices = new List<string> { FirstChoiceFor(asset) };
            if (asset != null)
            {
                foreach (var clip in asset.Clips)
                {
                    choices.Add(clip.Name);
                }
            }

            if (!string.IsNullOrEmpty(clipName) && !choices.Contains(clipName))
            {
                choices.Add(clipName);
            }

            return choices;
        }

        private static string FirstChoiceFor(VatAsset asset)
        {
            return asset != null && asset.TryValidate(out _) ? $"{FirstChoice} ({asset.Clips[0].Name})" : FirstChoice;
        }
    }
}
