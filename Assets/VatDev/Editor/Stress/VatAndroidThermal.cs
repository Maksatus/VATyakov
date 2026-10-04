using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace VATyakov.Dev
{
    internal static class VatAndroidThermal
    {
        private const int TimeoutMs = 10000;
        private const double BatteryScale = 0.1;
        private const string BatteryArguments = "-d shell dumpsys battery";
        private const string ThermalArguments = "-d shell dumpsys thermalservice";
        private const string HalSection = "Current temperatures from HAL";
        private const int CpuSensor = 0;
        private const int GpuSensor = 1;
        private const int SkinSensor = 3;

        private static readonly Regex _battery = new(@"^\s*temperature:\s*(-?\d+)", RegexOptions.Multiline);
        private static readonly Regex _status = new(@"^\s*Thermal Status:\s*(\d+)", RegexOptions.Multiline);
        private static readonly Regex _sensor = new(@"Temperature\{mValue=(-?[\d.]+), mType=(-?\d+),");

        public static Task<VatThermalState> Read()
        {
            return Task.Run(ReadNow);
        }

        private static VatThermalState ReadNow()
        {
            var adb = FindAdb();
            var battery = Run(adb, BatteryArguments);
            var thermal = Run(adb, ThermalArguments);
            var halStart = thermal.IndexOf(HalSection, StringComparison.Ordinal);
            var sensors = halStart >= 0 ? thermal.Substring(halStart) : thermal;
            return new VatThermalState(Battery(battery), Status(thermal), MaxSensor(sensors, CpuSensor, GpuSensor), MaxSensor(sensors, SkinSensor));
        }

        private static double Battery(string output)
        {
            var match = _battery.Match(output);
            return match.Success ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) * BatteryScale : double.NaN;
        }

        private static int Status(string output)
        {
            var match = _status.Match(output);
            return match.Success ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : VatThermalState.UnknownStatus;
        }

        private static double MaxSensor(string sensors, params int[] types)
        {
            var max = double.NaN;
            foreach (Match match in _sensor.Matches(sensors))
            {
                var value = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                if (Array.IndexOf(types, int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)) >= 0 && value > 0d)
                {
                    max = double.IsNaN(max) ? value : Math.Max(max, value);
                }
            }

            return max;
        }

        private static string Run(string adb, string arguments)
        {
            var startInfo = new ProcessStartInfo(adb, arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(TimeoutMs))
            {
                process.Kill();
                throw new TimeoutException($"adb {arguments}: no answer in {TimeoutMs / 1000} s");
            }

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"adb {arguments}: {error.Result.Trim()}");
            }

            return output.Result;
        }

        private static string FindAdb()
        {
            var executable = Application.platform == RuntimePlatform.WindowsEditor ? "adb.exe" : "adb";
            var bundled = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer", "SDK", "platform-tools", executable);
            return File.Exists(bundled) ? bundled : executable;
        }
    }
}
