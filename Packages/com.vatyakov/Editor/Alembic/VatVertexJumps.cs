using System.Globalization;
using UnityEngine;

namespace VATyakov.Editor
{
    // §2.2: a vertex that moves more than half the mesh size within one frame means the export reorders points.
    // Motion of the whole mesh is subtracted, so a fast soft body (1.5) does not trigger it.
    sealed class VatVertexJumps
    {
        readonly Vector3[] _previous;
        bool _hasPrevious;
        int _frames;
        string _first;

        public VatVertexJumps(int vertexCount)
        {
            _previous = new Vector3[vertexCount];
        }

        public string Warning => _frames == 0 ? null : string.Format(CultureInfo.InvariantCulture,
            "vertices jump by more than half the mesh size in {0} frames (first: {1}). " +
            "The point order in the .abc seems to change between frames: the export must keep the point order.", _frames, _first);

        public void Check(Vector3[] positions, double time)
        {
            if (_hasPrevious)
                Compare(positions, time);
            positions.CopyTo(_previous, 0);
            _hasPrevious = true;
        }

        void Compare(Vector3[] positions, double time)
        {
            var bounds = Bounds(_previous);
            float limit = 0.5f * Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            var shift = Centroid(positions) - Centroid(_previous);
            for (int v = 0; v < positions.Length; v++)
            {
                float distance = (positions[v] - _previous[v] - shift).magnitude;
                if (distance > limit)
                {
                    Report(v, time, distance);
                    return;
                }
            }
        }

        void Report(int vertex, double time, float distance)
        {
            _frames++;
            _first ??= string.Format(CultureInfo.InvariantCulture, "vertex {0} at {1:0.###} s, {2:0.###} m", vertex, time, distance);
        }

        static Bounds Bounds(Vector3[] points)
        {
            var builder = new VatBoundsBuilder();
            foreach (var point in points)
                builder.Add(point);
            return builder.Bounds;
        }

        static Vector3 Centroid(Vector3[] points)
        {
            var sum = Vector3.zero;
            foreach (var point in points)
                sum += point;
            return sum / Mathf.Max(1, points.Length);
        }
    }
}
