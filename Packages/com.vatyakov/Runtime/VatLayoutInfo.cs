using System;
using UnityEngine;

namespace VATyakov
{
    [Serializable]
    public struct VatLayoutInfo
    {
        [SerializeField]
        private int _elements;
        [SerializeField]
        private int _width;
        [SerializeField]
        private int _blocks;
        [SerializeField]
        private int _totalRows;

        public int Elements => _elements;
        public int Width => _width;
        public int Blocks => _blocks;
        public int TotalRows => _totalRows;
        public int Height => _blocks * _totalRows;
        public Vector4 ShaderLayout => new Vector4(_width, _totalRows, 0f, 0f);

        public VatLayoutInfo(int elements, int width, int blocks, int totalRows)
        {
            _elements = elements;
            _width = width;
            _blocks = blocks;
            _totalRows = totalRows;
        }
    }
}
