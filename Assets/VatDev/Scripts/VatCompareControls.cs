using UnityEngine;

namespace VATyakov.Dev
{
    /// <summary>
    /// On-screen controls for every VatCompare in the scene: Step/Play and the previous/next baked frame.
    /// Keyboard in the editor: Space, Left, Right. Sized for phones too.
    /// </summary>
    public sealed class VatCompareControls : MonoBehaviour
    {
        [SerializeField] VatCompare[] _targets;

        void OnGUI()
        {
            if (_targets == null || _targets.Length == 0)
                return;

            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Space)
                    Toggle();
                else if (e.keyCode == KeyCode.LeftArrow)
                    Advance(-1);
                else if (e.keyCode == KeyCode.RightArrow)
                    Advance(1);
            }

            float scale = Mathf.Max(1f, Screen.dpi / 160f);
            var button = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(20 * scale) };
            var label = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(20 * scale), alignment = TextAnchor.MiddleLeft };
            var width = GUILayout.Width(110 * scale);
            var height = GUILayout.Height(56 * scale);
            var first = _targets[0];

            GUILayout.BeginArea(new Rect(12 * scale, 12 * scale, Screen.width - 24 * scale, 72 * scale));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(first.Step ? "Play" : "Step", button, width, height))
                Toggle();
            if (GUILayout.Button("<", button, width, height))
                Advance(-1);
            if (GUILayout.Button(">", button, width, height))
                Advance(1);
            GUILayout.Label(first.Step ? $"  frame {first.Frame}" : "  playing", label, height);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void Toggle()
        {
            bool step = !_targets[0].Step;
            foreach (var target in _targets)
                target.SetStep(step);
        }

        void Advance(int frames)
        {
            foreach (var target in _targets)
            {
                if (!target.Step)
                    target.SetStep(true);
                target.Advance(frames);
            }
        }
    }
}
