using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatBoundsBuilder
    {
        private Bounds _bounds;
        private bool _isEmpty = true;

        public Bounds Bounds => _bounds;

        public static Bounds Of(Vector3[] points)
        {
            var builder = new VatBoundsBuilder();
            foreach (var point in points)
            {
                builder.Add(point);
            }

            return builder.Bounds;
        }

        public void Add(Vector3 point)
        {
            if (_isEmpty)
            {
                _bounds = new Bounds(point, Vector3.zero);
            }
            else
            {
                _bounds.Encapsulate(point);
            }

            _isEmpty = false;
        }
    }
}
