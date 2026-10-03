using System;
using UnityEngine;

namespace VATyakov.Dev
{
    public sealed class VatCrowdControls : MonoBehaviour
    {
        private const float CountPeriod = 0.5f;
        private const int LabelFontSize = 18;
        private const float ButtonWidth = 120f;
        private const float AreaHeight = 200f;
        private const float SliderWidth = 360f;

        [SerializeField]
        private VatCrowd _crowd;

        private int _materialCount;
        private float _nextCountTime;

        private void Update()
        {
            if (Time.unscaledTime < _nextCountTime)
            {
                return;
            }

            _materialCount = Resources.FindObjectsOfTypeAll<Material>().Length;
            _nextCountTime = Time.unscaledTime + CountPeriod;
        }

        private void OnGUI()
        {
            if (_crowd == null)
            {
                return;
            }

            HandleKeys(Event.current);
            var scale = VatDevGui.Scale();
            var button = VatDevGui.ButtonStyle(scale);
            var label = VatDevGui.LabelStyle(LabelFontSize, TextAnchor.MiddleLeft, scale);
            var width = GUILayout.Width(ButtonWidth * scale);
            var height = GUILayout.Height(VatDevGui.ButtonHeight * scale);
            var margin = VatDevGui.Margin * scale;

            GUILayout.BeginArea(new Rect(margin, margin, Screen.width - 2f * margin, AreaHeight * scale));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_crowd.IsActive ? "Pool" : "Unpool", button, width, height))
            {
                _crowd.TogglePool();
            }

            if (GUILayout.Button("Hit", button, width, height))
            {
                _crowd.Hit();
            }

            if (GUILayout.Button("Reverse", button, width, height))
            {
                _crowd.Reverse();
            }

            if (GUILayout.Button(_crowd.IsPaused ? "Resume" : "Pause", button, width, height))
            {
                _crowd.TogglePause();
            }

            if (GUILayout.Button(FormattableString.Invariant($"Fade {_crowd.FadeDuration:0.##} s"), button, width, height))
            {
                _crowd.NextFadeDuration();
            }

            if (GUILayout.Button(_crowd.IsManual ? "Auto" : "Manual", button, width, height))
            {
                _crowd.ToggleManual();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (_crowd.IsManual)
            {
                DrawWeightSlider(scale);
            }

            GUILayout.Label(Status(), label);
            GUILayout.EndArea();
        }

        private void DrawWeightSlider(float scale)
        {
            var height = GUILayout.Height(VatDevGui.ButtonHeight * scale);
            var weight = GUILayout.HorizontalSlider(_crowd.ManualWeight, 0f, 1f, GUILayout.Width(SliderWidth * scale), height);
            if (!Mathf.Approximately(weight, _crowd.ManualWeight))
            {
                _crowd.SetManualWeight(weight);
            }
        }

        private void HandleKeys(Event current)
        {
            if (current.type != EventType.KeyDown)
            {
                return;
            }

            if (current.keyCode == KeyCode.P)
            {
                _crowd.TogglePool();
            }
            else if (current.keyCode == KeyCode.H)
            {
                _crowd.Hit();
            }
            else if (current.keyCode == KeyCode.R)
            {
                _crowd.Reverse();
            }
            else if (current.keyCode == KeyCode.Space)
            {
                _crowd.TogglePause();
            }
            else if (current.keyCode == KeyCode.F)
            {
                _crowd.NextFadeDuration();
            }
            else if (current.keyCode == KeyCode.M)
            {
                _crowd.ToggleManual();
            }
        }

        private string Status()
        {
            var pooled = _crowd.IsActive ? string.Empty : " (pooled)";
            return FormattableString.Invariant(
                $"Units {_crowd.Count}{pooled}   Finished {_crowd.FinishedCount}   Doubled {_crowd.DoubledCount}   Materials {_materialCount}   Weight #0 {_crowd.FirstUnitWeight:0.00}");
        }
    }
}
