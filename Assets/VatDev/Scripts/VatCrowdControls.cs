using System;
using UnityEngine;

namespace VATyakov.Dev
{
    public sealed class VatCrowdControls : MonoBehaviour
    {
        private const float CountPeriod = 0.5f;
        private const int LabelFontSize = 18;
        private const float ButtonWidth = 120f;
        private const float AreaHeight = 140f;

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

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Label(Status(), label);
            GUILayout.EndArea();
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
        }

        private string Status()
        {
            var pooled = _crowd.IsActive ? string.Empty : " (pooled)";
            return FormattableString.Invariant(
                $"Units {_crowd.Count}{pooled}   Finished {_crowd.FinishedCount}   Doubled {_crowd.DoubledCount}   Materials {_materialCount}");
        }
    }
}
