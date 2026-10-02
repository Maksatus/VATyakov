using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatBoundsBuilder
    {
        private Bounds _bounds;
        private bool _empty = true;

        public Bounds Bounds => _bounds;

        public void Add(Vector3 point)
        {
            if (_empty)
            {
                _bounds = new Bounds(point, Vector3.zero);
            }
            else
            {
                _bounds.Encapsulate(point);
            }

            _empty = false;
        }
    }
}
