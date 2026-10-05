using System;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatRigidFpsCheck
    {
        private const float MillimetersPerMeter = 1000f;

        private readonly VatRigidEncoder _encoder;
        private readonly VatRigidInnerTimes _inner;

        public float MaxError { get; private set; }
        public int WorstPiece { get; private set; } = -1;
        public double WorstTime { get; private set; }
        public int PiecesOver { get; private set; }

        private VatClip Clip => _encoder.Layout.Clips[0];

        public VatRigidFpsCheck(VatRigidEncoder encoder, VatRigidInnerTimes inner)
        {
            _encoder = encoder;
            _inner = inner;
            for (var index = 0; index < encoder.Pieces.Length; index++)
            {
                MeasurePiece(index);
            }
        }

        public string Warning(float fps)
        {
            if (!(MaxError > VatRigidFormat.Tolerance))
            {
                return null;
            }

            var piece = _encoder.Pieces[WorstPiece].Name;
            var error = FormattableString.Invariant($"{MaxError * MillimetersPerMeter:0.#} mm at {WorstTime:0.###} s");
            var threshold = FormattableString.Invariant($"{VatRigidFormat.Tolerance * MillimetersPerMeter:0.#} mm");
            var pieces = FormattableString.Invariant($"{PiecesOver} {(PiecesOver == 1 ? "piece moves" : "pieces move")}");
            return FormattableString.Invariant(
                $"{pieces} too fast for {fps:0.###} fps: between baked frames the source differs by up to {error} (piece '{piece}'), over {threshold}. Raise Frames Per Second.");
        }

        private void MeasurePiece(int index)
        {
            var error = 0f;
            var time = 0.0;
            for (var sample = 0; sample < _inner.Count; sample++)
            {
                var sampleError = MeasureSample(index, sample);
                if (sampleError > error)
                {
                    error = sampleError;
                    time = _inner.Times[sample];
                }
            }

            if (error > VatRigidFormat.Tolerance)
            {
                PiecesOver++;
            }

            if (error > MaxError)
            {
                MaxError = error;
                WorstPiece = index;
                WorstTime = time;
            }
        }

        private float MeasureSample(int index, int sample)
        {
            var piece = _encoder.Pieces[index];
            var frame0 = _inner.Intervals[sample];
            var frame1 = (frame0 + 1) % Clip.FrameCount;
            if (!piece.Visibility.IsVisible(frame0) || !piece.Visibility.IsVisible(frame1) || !piece.Track.InnerVisible[sample])
            {
                return 0f;
            }

            var motion = piece.InnerMotion(sample);
            var fraction = _inner.Fractions[sample];
            var error = 0f;
            foreach (var rest in piece.RestPositions)
            {
                error = Mathf.Max(error, Vector3.Distance(_encoder.Point(index, frame0, frame1, fraction, rest), motion.MultiplyPoint3x4(rest)));
            }

            return error;
        }
    }
}
