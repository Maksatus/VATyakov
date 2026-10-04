using System;
using System.Collections.Generic;
using UnityEditor.Profiling;
using UnityEditorInternal;

namespace VATyakov.Dev
{
    internal sealed class VatStressAnalyzer
    {
        private const int MainThread = 0;
        private const int NotStarted = -1;
        private const double Median = 0.5;
        private const double P95 = 0.95;

        private readonly List<double> _frameTimes = new();
        private readonly List<double>[] _values = new List<double>[VatStressMetrics.All.Length];
        private readonly double[] _frameSampleTimes = new double[VatStressMetrics.All.Length];
        private readonly int[] _subtreeEnds = new int[VatStressMetrics.All.Length];
        private readonly Dictionary<int, int> _sampleMetrics = new();

        private int _nextFrame = NotStarted;

        public int FrameCount => _frameTimes.Count;
        public int FirstFrame { get; private set; } = NotStarted;
        public int DroppedFrames { get; private set; }

        public VatStressAnalyzer()
        {
            for (var i = 0; i < _values.Length; i++)
            {
                _values[i] = new List<double>();
            }
        }

        public void ProcessAvailableFrames(int maxFrames)
        {
            var firstAvailable = ProfilerDriver.firstFrameIndex;
            if (firstAvailable < 0)
            {
                return;
            }

            if (_nextFrame == NotStarted)
            {
                _nextFrame = firstAvailable;
                FirstFrame = firstAvailable;
            }
            else if (firstAvailable > _nextFrame)
            {
                DroppedFrames += firstAvailable - _nextFrame;
                _nextFrame = firstAvailable;
            }

            while (_nextFrame < ProfilerDriver.lastFrameIndex && _frameTimes.Count < maxFrames)
            {
                AddFrame(_nextFrame);
                _nextFrame++;
            }
        }

        public VatStressStats Complete()
        {
            var values = new double[_values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = VatStressMetrics.All[i].IsCounter ? Percentile(_values[i], Median) : Mean(_values[i]);
            }

            return new VatStressStats(FrameCount, DroppedFrames, Percentile(_frameTimes, Median), Percentile(_frameTimes, P95), values);
        }

        private void AddFrame(int frame)
        {
            using var view = ProfilerDriver.GetRawFrameDataView(frame, MainThread);
            if (!view.valid)
            {
                DroppedFrames++;
                return;
            }

            _frameTimes.Add(view.frameTimeMs);
            AddSamples(view);
            AddCounters(view);
        }

        private void AddSamples(RawFrameDataView view)
        {
            ResolveSamples(view);
            Array.Clear(_frameSampleTimes, 0, _frameSampleTimes.Length);
            Array.Clear(_subtreeEnds, 0, _subtreeEnds.Length);
            for (var i = 0; i < view.sampleCount; i++)
            {
                if (!_sampleMetrics.TryGetValue(view.GetSampleMarkerId(i), out var metric) || i < _subtreeEnds[metric])
                {
                    continue;
                }

                _frameSampleTimes[metric] += view.GetSampleTimeMs(i);
                _subtreeEnds[metric] = i + 1 + view.GetSampleChildrenCountRecursive(i);
            }

            for (var i = 0; i < _values.Length; i++)
            {
                if (!VatStressMetrics.All[i].IsCounter)
                {
                    _values[i].Add(_frameSampleTimes[i]);
                }
            }
        }

        private void ResolveSamples(FrameDataView view)
        {
            for (var i = 0; i < VatStressMetrics.All.Length; i++)
            {
                var metric = VatStressMetrics.All[i];
                var id = view.GetMarkerId(metric.Marker);
                if (!metric.IsCounter && id != FrameDataView.invalidMarkerId)
                {
                    _sampleMetrics[id] = i;
                }
            }
        }

        private void AddCounters(FrameDataView view)
        {
            for (var i = 0; i < _values.Length; i++)
            {
                var metric = VatStressMetrics.All[i];
                var id = view.GetMarkerId(metric.Marker);
                if (metric.IsCounter && id != FrameDataView.invalidMarkerId && view.HasCounterValue(id))
                {
                    _values[i].Add(view.GetCounterValueAsLong(id) * metric.Scale);
                }
            }
        }

        private static double Mean(List<double> values)
        {
            var sum = 0d;
            foreach (var value in values)
            {
                sum += value;
            }

            return values.Count > 0 ? sum / values.Count : double.NaN;
        }

        private static double Percentile(List<double> values, double percentile)
        {
            if (values.Count == 0)
            {
                return double.NaN;
            }

            var sorted = new List<double>(values);
            sorted.Sort();
            return sorted[(int)Math.Round((sorted.Count - 1) * percentile)];
        }
    }
}
