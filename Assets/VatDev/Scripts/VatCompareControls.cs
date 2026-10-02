using UnityEngine;
using UnityEngine.SceneManagement;

namespace VATyakov.Dev
{
    public sealed class VatCompareControls : MonoBehaviour
    {
        [SerializeField]
        private VatCompare[] _targets;

        private void OnGUI()
        {
            if (_targets == null || _targets.Length == 0)
            {
                return;
            }

            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Space)
                {
                    Toggle();
                }
                else if (e.keyCode == KeyCode.LeftArrow)
                {
                    Advance(-1);
                }
                else if (e.keyCode == KeyCode.RightArrow)
                {
                    Advance(1);
                }
                else if (e.keyCode == KeyCode.C)
                {
                    NextClip();
                }
            }

            var scale = Mathf.Max(1f, Screen.dpi / 160f);
            var button = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(20 * scale) };
            var label = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(20 * scale), alignment = TextAnchor.MiddleLeft };
            var width = GUILayout.Width(110 * scale);
            var height = GUILayout.Height(56 * scale);
            var first = _targets[0];

            GUILayout.BeginArea(new Rect(12 * scale, 12 * scale, Screen.width - 24 * scale, 72 * scale));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(first.Step ? "Play" : "Step", button, width, height))
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

            GUILayout.Label($"  {first.ClipName}, " + (first.Step ? $"frame {first.Frame}" : "playing"), label, height);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("RGBA8", button, width, height))
            {
                SceneManager.LoadScene("rot_decode");
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void Toggle()
        {
            var step = !_targets[0].Step;
            foreach (var target in _targets)
            {
                target.SetStep(step);
            }
        }

        private void Advance(int frames)
        {
            foreach (var target in _targets)
            {
                if (!target.Step)
                {
                    target.SetStep(true);
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
    }
}
