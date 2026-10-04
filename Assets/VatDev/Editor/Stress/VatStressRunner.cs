using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace VATyakov.Dev
{
    internal sealed class VatStressRunner : IDisposable
    {
        public const int RecommendedProfilerFrameCount = 2000;

        private const int CapturePollMs = 50;
        private const int WaitPollMs = 250;
        private const double StalledSeconds = 5d;
        private const string IdleStatus = "Ready";
        private const string ProfileExtension = ".data";
        private const string UrpPackagePath = "Packages/com.unity.render-pipelines.universal";

        private static readonly VatStressVariant[] _steps =
        {
            VatStressVariant.Empty,
            VatStressVariant.Smr,
            VatStressVariant.Vat,
            VatStressVariant.SmrCrossFade,
            VatStressVariant.VatCrossFade,
            VatStressVariant.SmrLod0,
            VatStressVariant.VatLod0,
            VatStressVariant.VatLod0,
            VatStressVariant.SmrLod0,
            VatStressVariant.VatCrossFade,
            VatStressVariant.SmrCrossFade,
            VatStressVariant.Vat,
            VatStressVariant.Smr,
            VatStressVariant.Empty
        };

        private readonly IConnectionState _connection;
        private readonly VatStressPlayer _player = VatStressPlayer.Create();
        private readonly VatStressThermal _thermal;

        private CancellationTokenSource _cancellation;
        private bool _isDisposed;

        public event Action<string> Logged;

        public string Status { get; private set; } = IdleStatus;
        public bool IsRunning => _cancellation != null;
        public static int StepCount => _steps.Length;
        public static string ReportFolder => Path.Combine(ProjectRoot, "Docs", "perf");
        private static string ProfileFolder => Path.Combine(ProjectRoot, "Logs", "VatStress");
        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

        public VatStressRunner(IConnectionState connection)
        {
            _connection = connection;
            _thermal = new VatStressThermal(Log);
        }

        public void Dispose()
        {
            _isDisposed = true;
            if (IsRunning)
            {
                _cancellation.Cancel();
            }
            else
            {
                Object.DestroyImmediate(_player);
            }
        }

        public void Stop()
        {
            _cancellation?.Cancel();
        }

        public async Task Run(VatStressSettings settings)
        {
            if (IsRunning)
            {
                return;
            }

            var cancellation = new CancellationTokenSource();
            var session = $"stress_{DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)}";
            var captures = new List<VatStressCapture>();
            var invalidReason = string.Empty;
            _cancellation = cancellation;
            EditorApplication.LockReloadAssemblies();
            try
            {
                PrepareProfiler();
                await EnsureVariants(cancellation.Token);
                await _thermal.Check("the start");
                Log(FormattableString.Invariant($"{session}: {_steps.Length} captures"));
                for (var i = 0; i < _steps.Length; i++)
                {
                    captures.Add(await Capture(i, _steps[i], settings, session, cancellation.Token));
                    Log(Describe(captures[i]));
                }
            }
            catch (OperationCanceledException)
            {
                Log($"{session}: stopped");
            }
            catch (VatThrottlingException exception)
            {
                invalidReason = $"{exception.Message}: the device throttles, the session is invalid. Let it cool down and measure again.";
                Log($"{session}: {invalidReason}");
            }
            catch (Exception exception)
            {
                Log($"{session}: {exception.Message}");
                Debug.LogException(exception);
            }
            finally
            {
                WriteReport(session, captures, settings, invalidReason);
                EditorApplication.UnlockReloadAssemblies();
                cancellation.Dispose();
                _cancellation = null;
                Status = IdleStatus;
                if (_isDisposed)
                {
                    Object.DestroyImmediate(_player);
                }
            }
        }

        private void PrepareProfiler()
        {
            if (_connection.connectedToTarget != ConnectionTarget.Player)
            {
                throw new InvalidOperationException("The Profiler is not connected to a player: pick the device in the target list.");
            }

            if (ProfilerDriver.deepProfiling)
            {
                throw new InvalidOperationException("Turn Deep Profile off.");
            }

            ProfilerDriver.SetAreaEnabled(ProfilerArea.CPU, true);
            ProfilerDriver.SetAreaEnabled(ProfilerArea.Rendering, true);
            ProfilerDriver.SetAreaEnabled(ProfilerArea.GPU, false);
            ProfilerDriver.enabled = true;
        }

        private async Task EnsureVariants(CancellationToken token)
        {
            var available = await _player.List(token);
            var missing = _steps.Select(step => step.Name).Where(name => !available.Contains(name)).Distinct().ToArray();
            if (missing.Length > 0)
            {
                throw new InvalidOperationException($"The player has no variants {string.Join(", ", missing)}: rebuild it.");
            }
        }

        private async Task<VatStressCapture> Capture(int index, VatStressVariant variant, VatStressSettings settings, string session, CancellationToken token)
        {
            var prefix = FormattableString.Invariant($"{index + 1}/{_steps.Length} {variant.Name}");
            await _player.Select(variant.Name, token);
            await Wait(prefix, settings.WarmupSeconds, token);
            var thermalBefore = await _thermal.Check(prefix);
            var startedAt = DateTime.Now;
            var analyzer = new VatStressAnalyzer();
            ProfilerDriver.ClearAllFrames();
            await Record(prefix, analyzer, settings.CaptureFrames, token);
            var profile = SaveProfile(session, index, variant, analyzer);
            var info = await _player.Info(token);
            var thermalAfter = await _thermal.Check(prefix);
            return new VatStressCapture(index + 1, variant.Name, startedAt, analyzer.Complete(), info, thermalBefore, thermalAfter, profile);
        }

        private async Task Wait(string prefix, float seconds, CancellationToken token)
        {
            var elapsed = Stopwatch.StartNew();
            while (elapsed.Elapsed.TotalSeconds < seconds)
            {
                Status = FormattableString.Invariant($"{prefix}: warmup {seconds - elapsed.Elapsed.TotalSeconds:0} s");
                await _thermal.Poll(prefix);
                await Task.Delay(WaitPollMs, token);
            }
        }

        private async Task Record(string prefix, VatStressAnalyzer analyzer, int frames, CancellationToken token)
        {
            var stalled = Stopwatch.StartNew();
            var lastCount = 0;
            while (analyzer.FrameCount < frames)
            {
                analyzer.ProcessAvailableFrames(frames);
                if (analyzer.FrameCount != lastCount)
                {
                    lastCount = analyzer.FrameCount;
                    stalled.Restart();
                }
                else if (stalled.Elapsed.TotalSeconds > StalledSeconds)
                {
                    throw new InvalidOperationException("Profiler frames stopped coming: check the connection and that the Profiler records.");
                }

                Status = FormattableString.Invariant($"{prefix}: capture {analyzer.FrameCount}/{frames}");
                await _thermal.Poll(prefix);
                await Task.Delay(CapturePollMs, token);
            }
        }

        private string SaveProfile(string session, int index, VatStressVariant variant, VatStressAnalyzer analyzer)
        {
            var folder = Path.Combine(ProfileFolder, session);
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, FormattableString.Invariant($"{index + 1:00}_{variant.Name}{ProfileExtension}"));
            if (!ProfilerDriver.SaveProfile(path))
            {
                Log($"The Profiler did not save {Path.GetFileName(path)}");
                return string.Empty;
            }

            if (ProfilerDriver.firstFrameIndex > analyzer.FirstFrame)
            {
                Log(FormattableString.Invariant(
                    $"{Path.GetFileName(path)} misses frames: set Preferences > Analysis > Profiler > Frame count to {RecommendedProfilerFrameCount}."));
            }

            return path;
        }

        private void WriteReport(string session, IReadOnlyList<VatStressCapture> captures, VatStressSettings settings, string invalidReason)
        {
            if (captures.Count == 0)
            {
                return;
            }

            Directory.CreateDirectory(ReportFolder);
            var urp = PackageInfo.FindForAssetPath(UrpPackagePath);
            var name = invalidReason.Length > 0 ? $"{session}_invalid" : session;
            var markdown = Path.Combine(ReportFolder, $"{name}.md");
            VatStressCsv.Write(Path.Combine(ReportFolder, $"{name}.csv"), captures);
            VatStressMarkdown.Write(markdown, captures, settings, urp != null ? urp.version : "unknown", invalidReason);
            Log($"Report: {markdown}");
        }

        private static string Describe(VatStressCapture capture)
        {
            var stats = capture.Stats;
            var step = FormattableString.Invariant($"{capture.Index}/{_steps.Length} {capture.Variant}: frame {stats.FrameMedian:0.0} ms, ");
            var cpu = FormattableString.Invariant(
                $"CPU main {stats.Value(VatStressMetrics.CpuMain):0.00} ms, render {stats.Value(VatStressMetrics.CpuRender):0.00} ms, ");
            var gpu = FormattableString.Invariant($"GPU {stats.Value(VatStressMetrics.Gpu):0.00} ms, animation {stats.AnimationCpu():0.00} ms, ");
            var before = capture.ThermalBefore;
            var after = capture.ThermalAfter;
            var battery = FormattableString.Invariant($"battery {before.Battery:0.0} -> {after.Battery:0.0} C, ");
            var thermal = FormattableString.Invariant($"Thermal Status {before.Status} -> {after.Status}");
            return step + cpu + gpu + battery + thermal;
        }

        private void Log(string message)
        {
            Logged?.Invoke(message);
        }
    }
}
