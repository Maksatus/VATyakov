using System;
using UnityEngine;

namespace VATyakov.Dev
{
    public sealed class VatStressControls : MonoBehaviour
    {
        private const int LabelFontSize = 18;
        private const float ButtonWidth = 150f;
        private const float AreaHeight = 140f;
        private const float FrameTimeSmoothing = 0.05f;

        [SerializeField]
        private VatStress _stress;

        private float _frameTime;

        private void Update()
        {
            _frameTime = Mathf.Lerp(_frameTime, Time.unscaledDeltaTime, FrameTimeSmoothing);
        }

        private void OnGUI()
        {
            var scale = VatDevGui.Scale();
            var button = VatDevGui.ButtonStyle(scale);
            var label = VatDevGui.LabelStyle(LabelFontSize, TextAnchor.MiddleLeft, scale);
            var width = GUILayout.Width(ButtonWidth * scale);
            var height = GUILayout.Height(VatDevGui.ButtonHeight * scale);
            var margin = VatDevGui.Margin * scale;

            GUILayout.BeginArea(new Rect(margin, margin, Screen.width - 2f * margin, AreaHeight * scale));
            GUILayout.BeginHorizontal();
            foreach (var variant in VatStressVariant.All)
            {
                if (GUILayout.Button(variant.Name, button, width, height))
                {
                    _stress.Select(variant);
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Label(Status(), label);
            GUILayout.EndArea();
        }

        private string Status()
        {
            return FormattableString.Invariant(
                $"{_stress.Variant.Name}   Units {_stress.Units.Count}   Frame {_frameTime * 1000f:0.0} ms   In transition {_stress.TransitionShare:P0}");
        }
    }
}
