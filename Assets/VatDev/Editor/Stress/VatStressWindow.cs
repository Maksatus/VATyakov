using System;
using System.IO;
using UnityEditor;
using UnityEditor.Networking.PlayerConnection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.UIElements;

namespace VATyakov.Dev
{
    internal sealed class VatStressWindow : EditorWindow
    {
        private const int MaxLogLines = 200;
        private const long RefreshIntervalMs = 250;
        private const float Spacing = 6f;

        private static readonly string _instructions =
            "Build Settings: 'stress' is the first scene; Development Build, Autoconnect Profiler, IL2CPP. Run the build on the device, " +
            "pick it in the target list below, turn Deep Profile off, " +
            FormattableString.Invariant($"set Preferences > Analysis > Profiler > Frame count to {VatStressRunner.RecommendedProfilerFrameCount}. ") +
            FormattableString.Invariant($"Measure runs {VatStressRunner.StepCount} captures and writes a Markdown table and a CSV to Docs/perf, ") +
            "Profiler captures to Logs/VatStress.";

        [SerializeField]
        private VatStressSettings _settings = new();

        private IConnectionState _connection;
        private VatStressRunner _runner;
        private Button _measureButton;
        private Button _stopButton;
        private Label _status;
        private ScrollView _log;

        [MenuItem("VATyakov/Dev/Stress Measurement")]
        private static void Open()
        {
            GetWindow<VatStressWindow>("VAT Stress");
        }

        private void OnEnable()
        {
            _connection = PlayerConnectionGUIUtility.GetConnectionState(this);
            _runner = new VatStressRunner(_connection);
            _runner.Logged += AddLog;
        }

        private void OnDisable()
        {
            _runner.Logged -= AddLog;
            _runner.Dispose();
            _connection.Dispose();
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = Spacing;
            root.style.paddingRight = Spacing;
            root.style.paddingTop = Spacing;
            root.Add(new HelpBox(_instructions, HelpBoxMessageType.Info));
            root.Add(new IMGUIContainer(DrawTarget));

            var window = new SerializedObject(this);
            var settings = new PropertyField(window.FindProperty(nameof(_settings)), "Settings");
            settings.Bind(window);
            root.Add(settings);

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            _measureButton = new Button(Measure) { text = "Measure" };
            _stopButton = new Button(_runner.Stop) { text = "Stop" };
            buttons.Add(_measureButton);
            buttons.Add(_stopButton);
            buttons.Add(new Button(OpenReportFolder) { text = "Open Report Folder" });
            root.Add(buttons);

            _status = new Label();
            _status.style.unityFontStyleAndWeight = FontStyle.Bold;
            _status.style.marginTop = Spacing;
            root.Add(_status);

            _log = new ScrollView();
            _log.style.flexGrow = 1f;
            _log.style.marginTop = Spacing;
            root.Add(_log);
            root.schedule.Execute(Refresh).Every(RefreshIntervalMs);
        }

        private void DrawTarget()
        {
            PlayerConnectionGUILayout.ConnectionTargetSelectionDropdown(_connection);
        }

        private void Measure()
        {
            _ = _runner.Run(_settings);
        }

        private void Refresh()
        {
            _status.text = _runner.Status;
            _measureButton.SetEnabled(!_runner.IsRunning);
            _stopButton.SetEnabled(_runner.IsRunning);
        }

        private void AddLog(string message)
        {
            var line = new Label(FormattableString.Invariant($"{DateTime.Now:HH:mm:ss} {message}"));
            line.style.whiteSpace = WhiteSpace.Normal;
            _log.Add(line);
            while (_log.childCount > MaxLogLines)
            {
                _log.RemoveAt(0);
            }

            _log.schedule.Execute(() => _log.ScrollTo(line));
            Debug.Log($"[VAT Stress] {message}");
        }

        private static void OpenReportFolder()
        {
            Directory.CreateDirectory(VatStressRunner.ReportFolder);
            EditorUtility.RevealInFinder(VatStressRunner.ReportFolder);
        }
    }
}
