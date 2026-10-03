using System;
using UnityEngine;

namespace VATyakov
{
    [Serializable]
    public struct VatPositionRange
    {
        [SerializeField]
        private Vector3 _min;
        [SerializeField]
        private Vector3 _size;

        public static VatPositionRange Identity => new(Vector3.zero, Vector3.one);

        public Vector3 Min => _min;
        public Vector3 Size => _size;
        public Vector4 ShaderScale => new Vector4(_size.x, _size.y, _size.z, 0f);

        public VatPositionRange(Vector3 min, Vector3 size)
        {
            _min = min;
            _size = size;
        }
    }
}
