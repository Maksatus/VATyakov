using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatPositionEncoding
    {
        public const float ByteTolerance = 0.008f;

        public readonly VatPositionFormat Format;
        public readonly VatPositionRange Range;
        public readonly float ByteError;
        public readonly VatSubMeshes SubMeshes;

        private readonly IVatPositionTexels _texels;
        private readonly VatBoundsBuilder _bounds = new();
        private readonly Vector3[] _rowPositions;

        public float MaxError { get; private set; }
        public Bounds Bounds => _bounds.Bounds;

        public VatPositionEncoding(VatLayoutInfo info, VatSourceMesh source, VatPositionRange byteRange, float byteError, float byteTolerance)
        {
            var isByte = byteError < byteTolerance;
            Format = isByte ? VatPositionFormat.Byte : VatPositionFormat.Half;
            Range = isByte ? byteRange : VatPositionRange.Identity;
            ByteError = byteError;
            SubMeshes = new VatSubMeshes(source);
            _texels = isByte ? new VatBytePositionTexels(info, byteRange) : new VatHalfPositionTexels(info);
            _rowPositions = new Vector3[source.VertexCount];
        }

        public void Write(int vertex, int row, Vector3 delta, Vector3 origin)
        {
            var decoded = _texels.Write(vertex, row, delta);
            var position = origin + decoded;
            MaxError = Mathf.Max(MaxError, (decoded - delta).magnitude);
            _bounds.Add(position);
            _rowPositions[vertex] = position;
        }

        public void EndRow()
        {
            SubMeshes.Encapsulate(_rowPositions);
        }

        public Texture2D BuildTexture(string name)
        {
            return _texels.Build(name);
        }
    }
}
