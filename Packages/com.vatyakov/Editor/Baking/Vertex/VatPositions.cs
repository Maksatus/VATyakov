using System;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatPositions
    {
        private readonly VatLayoutInfo _info;
        private readonly VatSourceMesh _source;
        private readonly float _byteTolerance;
        private readonly Vector3[] _deltas;
        private readonly Vector3[] _rest;
        private readonly Vector3[] _drift;
        private readonly bool[] _rowWritten;

        private VatPositionEncoding _encoding;

        public float MaxOffset { get; private set; }
        public VatPositionEncoding Encoding => _encoding ??= Encode();

        public VatPositions(VatLayoutInfo info, VatSourceMesh source, float byteTolerance)
        {
            _info = info;
            _source = source;
            _byteTolerance = byteTolerance;
            _deltas = new Vector3[info.TotalRows * info.Elements];
            _rest = new Vector3[info.Elements];
            _drift = new Vector3[info.TotalRows];
            _rowWritten = new bool[info.TotalRows];
        }

        public void Write(int vertex, int row, Vector3 position, Vector3 rest, Vector3 drift)
        {
            var delta = position - rest - drift;
            _deltas[row * _info.Elements + vertex] = delta;
            _rest[vertex] = rest;
            _drift[row] = drift;
            MaxOffset = Mathf.Max(MaxOffset, delta.magnitude);
        }

        public void EndRow(int row)
        {
            _rowWritten[row] = true;
        }

        private VatPositionEncoding Encode()
        {
            RequireComplete();
            var range = VatBytePositions.Range(_deltas);
            var encoding = new VatPositionEncoding(_info, _source, range, VatBytePositions.MaxError(_deltas, range), _byteTolerance);
            for (var row = 0; row < _info.TotalRows; row++)
            {
                for (var vertex = 0; vertex < _info.Elements; vertex++)
                {
                    encoding.Write(vertex, row, _deltas[row * _info.Elements + vertex], _rest[vertex] + _drift[row]);
                }

                encoding.EndRow();
            }

            return encoding;
        }

        private void RequireComplete()
        {
            var missing = Array.IndexOf(_rowWritten, false);
            if (missing >= 0)
            {
                throw new InvalidOperationException(FormattableString.Invariant($"Row {missing} of the position texture was never written."));
            }
        }
    }
}
