using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatPositions
    {
        private readonly VatBoundsBuilder _bounds = new();
        private readonly Vector3[] _decoded;

        public VatPositionTexels Texels { get; }
        public VatSubMeshes SubMeshes { get; }
        public Bounds Bounds => _bounds.Bounds;
        public float MaxOffset { get; private set; }
        public float MaxError { get; private set; }

        public VatPositions(VatLayoutInfo info, VatSourceMesh source)
        {
            Texels = new VatPositionTexels(info);
            SubMeshes = new VatSubMeshes(source);
            _decoded = new Vector3[source.VertexCount];
        }

        public void Write(int vertex, int row, Vector3 position, Vector3 rest, Vector3 drift)
        {
            var delta = position - rest - drift;
            var decoded = rest + drift + Texels.Write(vertex, row, delta);
            MaxOffset = Mathf.Max(MaxOffset, delta.magnitude);
            MaxError = Mathf.Max(MaxError, (decoded - position).magnitude);
            _bounds.Add(decoded);
            _decoded[vertex] = decoded;
        }

        public void EndRow(int row)
        {
            SubMeshes.Encapsulate(_decoded);
            Texels.MarkRow(row);
        }
    }
}
