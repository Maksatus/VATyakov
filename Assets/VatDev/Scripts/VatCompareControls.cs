using System;
using UnityEngine;

namespace VATyakov.Dev
{
    public sealed class VatCompareControls : MonoBehaviour
    {
        private const int LabelFontSize = 20;
        private const float ButtonWidth = 110f;
        private const float AreaHeight = 72f;

        [SerializeField]
        private VatCompare[] _targets;

        [Tooltip("LOD groups the LOD button and the L key force to one level; Auto gives them back to the camera.")]
        [SerializeField]
        private LODGroup[] _lodGroups = Array.Empty<LODGroup>();

        private int _forcedLod = -1;

        private void OnGUI()
        {
            if (_targets.Length == 0)
            {
                return;
            }

            HandleKeys(Event.current);
            var scale = VatDevGui.Scale();
            var button = VatDevGui.ButtonStyle(scale);
            var label = VatDevGui.LabelStyle(LabelFontSize, TextAnchor.MiddleLeft, scale);
            var width = GUILayout.Width(ButtonWidth * scale);
            var height = GUILayout.Height(VatDevGui.ButtonHeight * scale);
            var first = _targets[0];
            var margin = VatDevGui.Margin * scale;

            GUILayout.BeginArea(new Rect(margin, margin, Screen.width - 2f * margin, AreaHeight * scale));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(first.IsStepping ? "Play" : "Step", button, width, height))
            {
                Toggle();
            }

            if (GUILayout.Button("<", button, width, height))
            {
                Advance(-1);
            }

            if (GUILayout.Button(">", button, width, height))
            {
                Advance(1);
            }

            if (GUILayout.Button("Clip", button, width, height))
            {
                NextClip();
            }

            if (_lodGroups.Length > 0 && GUILayout.Button(LodLabel(), button, width, height))
            {
                NextLod();
            }

            GUILayout.Label(Status(first), label, height);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private static string Status(VatCompare compare)
        {
            return compare.IsStepping ? FormattableString.Invariant($"  {compare.ClipName}, frame {compare.Frame}") : $"  {compare.ClipName}, playing";
        }

        private string LodLabel()
        {
            return _forcedLod < 0 ? "LOD Auto" : FormattableString.Invariant($"LOD {_forcedLod}");
        }

        private void HandleKeys(Event current)
        {
            if (current.type != EventType.KeyDown)
            {
                return;
            }

            if (current.keyCode == KeyCode.Space)
            {
                Toggle();
            }
            else if (current.keyCode == KeyCode.LeftArrow)
            {
                Advance(-1);
            }
            else if (current.keyCode == KeyCode.RightArrow)
            {
                Advance(1);
            }
            else if (current.keyCode == KeyCode.C)
            {
                NextClip();
            }
            else if (current.keyCode == KeyCode.L && _lodGroups.Length > 0)
            {
                NextLod();
            }
        }

        private void Toggle()
        {
            var isStepping = !_targets[0].IsStepping;
            foreach (var target in _targets)
            {
                target.SetStepping(isStepping);
            }
        }

        private void Advance(int frames)
        {
            foreach (var target in _targets)
            {
                if (!target.IsStepping)
                {
                    target.SetStepping(true);
                }

                target.Advance(frames);
            }
        }

        private void NextClip()
        {
            foreach (var target in _targets)
            {
                target.NextClip();
            }
        }

        private void NextLod()
        {
            _forcedLod = _forcedLod + 1 < _lodGroups[0].lodCount ? _forcedLod + 1 : -1;
            foreach (var group in _lodGroups)
            {
                group.ForceLOD(_forcedLod);
            }
        }
    }
}
