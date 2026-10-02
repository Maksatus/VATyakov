using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatBoundsBuilder
    {
        private Bounds _bounds;
        private bool _isEmpty = true;

        public Bounds Bounds => _bounds;

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
