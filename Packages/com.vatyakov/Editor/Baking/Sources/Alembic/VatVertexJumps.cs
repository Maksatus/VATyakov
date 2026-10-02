using System;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatVertexJumps
    {
        private const float MaxJumpOfMeshSize = 0.5f;
        private const string PointOrderHint =
            "The point order in the .abc seems to change between frames: the export must keep the point order.";

        private readonly Vector3[] _previous;

        private bool _hasPrevious;
        private int _frames;
        private string _first;

        public string Warning => _frames == 0 ? null : JumpWarning();

        public VatVertexJumps(int vertexCount)
        {
            _previous = new Vector3[vertexCount];
        }

        public void Check(Vector3[] positions, double time)
        {
            if (_hasPrevious)
            {
                Compare(positions, time);
            }

            positions.CopyTo(_previous, 0);
            _hasPrevious = true;
        }

        private void Compare(Vector3[] positions, double time)
        {
            var bounds = Bounds(_previous);
            var limit = MaxJumpOfMeshSize * Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            var shift = VatCentroid.Of(positions) - VatCentroid.Of(_previous);
            for (var vertex = 0; vertex < positions.Length; vertex++)
            {
                var distance = (positions[vertex] - _previous[vertex] - shift).magnitude;
                if (distance > limit)
                {
                    Report(vertex, time, distance);
                    return;
                }
            }
        }

        private string JumpWarning()
        {
            return FormattableString.Invariant($"vertices jump by more than half the mesh size in {_frames} frames (first: {_first}). {PointOrderHint}");
        }

        private void Report(int vertex, double time, float distance)
        {
            _frames++;
            _first ??= FormattableString.Invariant($"vertex {vertex} at {time:0.###} s, {distance:0.###} m");
        }

        private static Bounds Bounds(Vector3[] points)
        {
            var builder = new VatBoundsBuilder();
            foreach (var point in points)
            {
                builder.Add(point);
            }

            return builder.Bounds;
        }
    }
}
