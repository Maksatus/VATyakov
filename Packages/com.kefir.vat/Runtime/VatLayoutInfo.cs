using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Kefir.Vat
{
    /// <summary>What an element of the VAT textures is (§2).</summary>
    public enum VatMode
    {
        /// <summary>One texel per vertex per frame (§2.2).</summary>
        Vertex = 0,

        /// <summary>Two texels per bone per frame (§2.1, subversion 1.11).</summary>
        Bone = 1,

        /// <summary>Bone layout with one influence per piece (§2.3, subversion 1.15).</summary>
        Rigid = 2,
    }

    /// <summary>
    /// Texture layout of a baked asset (§1.8, §1.9). Width and row count are also written to _VatLayout.
    /// </summary>
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

        public VatLayoutInfo(VatMode mode, int elements, int texelsPerItem, int width, int blocks, int totalRows,
            bool pivotRow, bool drift, GraphicsFormat positionFormat)
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
        }

        public VatMode Mode => _mode;

        /// <summary>E — texels addressed per row: vertices, or 2 × bones.</summary>
        public int Elements => _elements;

        /// <summary>Texels per vertex (1) or per bone and piece (2).</summary>
        public int TexelsPerItem => _texelsPerItem;

        /// <summary>W = ceil(E / blocks).</summary>
        public int Width => _width;

        /// <summary>ceil(E / 4096) blocks stacked vertically.</summary>
        public int Blocks => _blocks;

        /// <summary>Rows in one block: ΣF, plus the pivot row in Bone and Rigid modes.</summary>
        public int TotalRows => _totalRows;

        /// <summary>Bone and Rigid: row 0 of each block holds pivots.</summary>
        public bool PivotRow => _pivotRow;

        /// <summary>Vertex mode with _VatDriftTex (§1.9).</summary>
        public bool Drift => _drift;

        public GraphicsFormat PositionFormat => _positionFormat;

        /// <summary>Texture height: blocks · totalRows, never above 4096.</summary>
        public int Height => _blocks * _totalRows;

        /// <summary>_VatLayout = (W, totalRows, driftOn, 0).</summary>
        public Vector4 ShaderLayout => new Vector4(_width, _totalRows, _drift ? 1f : 0f, 0f);
    }
}
