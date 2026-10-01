using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace VATyakov
{
    public enum VatMode
    {
        Vertex = 0,
        Bone = 1, // 1.11
        Rigid = 2, // 1.15: Bone layout, one influence per piece
    }

    // §1.8, §1.9.
    [Serializable]
    public struct VatLayoutInfo
    {
        [SerializeField] VatMode _mode;
        [SerializeField] int _elements;
        [SerializeField] int _texelsPerItem;
        [SerializeField] int _width;
        [SerializeField] int _blocks;
        [SerializeField] int _totalRows;
        [SerializeField] bool _pivotRow;
        [SerializeField] bool _drift;
        [SerializeField] GraphicsFormat _positionFormat;
        [SerializeField] GraphicsFormat _rotationFormat;

        public VatLayoutInfo(VatMode mode, int elements, int texelsPerItem, int width, int blocks, int totalRows,
            bool pivotRow, bool drift, GraphicsFormat positionFormat, GraphicsFormat rotationFormat)
        {
            _mode = mode;
            _elements = elements;
            _texelsPerItem = texelsPerItem;
            _width = width;
            _blocks = blocks;
            _totalRows = totalRows;
            _pivotRow = pivotRow;
            _drift = drift;
            _positionFormat = positionFormat;
            _rotationFormat = rotationFormat;
        }

        public VatMode Mode => _mode;

        // E: vertices, or 2 × bones.
        public int Elements => _elements;

        public int TexelsPerItem => _texelsPerItem;

        public int Width => _width;

        public int Blocks => _blocks;

        // Rows of one block: ΣF, plus the pivot row in Bone and Rigid.
        public int TotalRows => _totalRows;

        public bool PivotRow => _pivotRow;

        public bool Drift => _drift;

        public GraphicsFormat PositionFormat => _positionFormat;

        // Vertex mode: _VatRotTex, same size as _VatPosTex.
        public GraphicsFormat RotationFormat => _rotationFormat;

        public int Height => _blocks * _totalRows;

        // _VatLayout = (W, totalRows, driftOn, 0).
        public Vector4 ShaderLayout => new Vector4(_width, _totalRows, _drift ? 1f : 0f, 0f);
    }
}
