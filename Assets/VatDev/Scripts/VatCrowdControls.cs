using UnityEngine;

namespace VATyakov.Dev
{
    public sealed class VatCrowdControls : MonoBehaviour
    {
        [SerializeField]
        private VatCrowd _crowd;

        private int _materials;
        private float _nextCount;

        private void Update()
        {
            if (Time.unscaledTime < _nextCount)
            {
                return;
            }

            _materials = Resources.FindObjectsOfTypeAll<Material>().Length;
            _nextCount = Time.unscaledTime + 0.5f;
        }

        private void OnGUI()
        {
            if (_crowd == null)
            {
                return;
            }

            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.P)
                {
                    _crowd.TogglePool();
                }
                else if (e.keyCode == KeyCode.H)
                {
                    _crowd.Hit();
                }
                else if (e.keyCode == KeyCode.R)
                {
                    _crowd.Reverse();
                }
                else if (e.keyCode == KeyCode.Space)
                {
                    _crowd.TogglePause();
                }
            }

            var scale = Mathf.Max(1f, Screen.dpi / 160f);
            var button = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(20 * scale) };
            var label = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(18 * scale), alignment = TextAnchor.MiddleLeft };
            var width = GUILayout.Width(120 * scale);
            var height = GUILayout.Height(56 * scale);

            GUILayout.BeginArea(new Rect(12 * scale, 12 * scale, Screen.width - 24 * scale, 140 * scale));
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
            GUILayout.Label($"Units {_crowd.Count}{(_crowd.IsActive ? "" : " (pooled)")}   " +
                $"Finished {_crowd.Finished}   Doubled {_crowd.Doubled}   Materials {_materials}", label);
            GUILayout.EndArea();
        }
    }
}
