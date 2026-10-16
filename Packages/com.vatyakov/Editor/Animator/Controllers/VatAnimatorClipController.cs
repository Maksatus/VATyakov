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
        private VatAsset _shownAsset;

        private SerializedObject SerializedObject => _context.SerializedObject;

        public VatAnimatorClipController(VatAnimatorContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatAnimatorClipContainer>();
        }

        public void Deactivate()
        {
            _container.Clip.formatSelectedValueCallback = null;
            _container.Clip.formatListItemCallback = null;
            _container.Root.Unbind();
            _container.DestroyView();
        }

        public void Activate()
        {
            _asset = SerializedObject.FindProperty(VatAnimatorContext.AssetPath);
            _clip = SerializedObject.FindProperty(VatAnimatorContext.ClipPath);
            _container.Clip.formatSelectedValueCallback = Format;
            _container.Clip.formatListItemCallback = Format;
            Refresh();
            _container.Root.Bind(SerializedObject);
            _container.Root.TrackPropertyValue(_asset, OnPropertyChanged);
            _container.Root.TrackPropertyValue(_clip, OnPropertyChanged);
        }

        private void OnPropertyChanged(SerializedProperty _)
        {
            Refresh();
        }

        private void Refresh()
        {
            SerializedObject.Update();
            _shownAsset = _asset.objectReferenceValue as VatAsset;
            var clipName = _clip.stringValue;
            _container.Clip.choices = Choices(_shownAsset, clipName);
            _container.Clip.SetValueWithoutNotify(clipName);
            _container.Asset.SetEnabled(!EditorApplication.isPlaying);
            _container.Clip.SetEnabled(_shownAsset != null && !EditorApplication.isPlaying);
            _container.Problem.SetMessage(Problem(_shownAsset, clipName));
        }

        private string Format(string choice)
        {
            if (!string.IsNullOrEmpty(choice))
            {
                return choice;
            }

            return _shownAsset != null && _shownAsset.TryValidate(out _) ? $"{FirstChoice} ({_shownAsset.Clips[0].Name})" : FirstChoice;
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
            var choices = new List<string> { string.Empty };
            if (asset != null)
            {
                foreach (var clip in asset.Clips)
                {
                    choices.Add(clip.Name);
                }
            }

            if (!choices.Contains(clipName))
            {
                choices.Add(clipName);
            }

            return choices;
        }
    }
}
