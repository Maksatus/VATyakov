using UnityEngine;

namespace VATyakov.Editor
{
    // One split of a position into d + Δ (§1.9). The encoder writes the split with drift and without it in one pass
    // and keeps one, so the error of both is known (§2.2).
    sealed class VatPositionVariant
    {
        readonly VatBoundsBuilder _bounds = new VatBoundsBuilder();
        readonly Vector3[] _decoded;

        public VatPositionVariant(VatLayoutInfo info, VatSourceMesh source)
        {
            Texels = new VatPositionTexels(info);
            SubMeshes = new VatSubMeshes(source);
            _decoded = new Vector3[source.VertexCount];
        }

        public VatPositionTexels Texels { get; }

        public VatSubMeshes SubMeshes { get; }

        public Bounds Bounds => _bounds.Bounds;

        public float MaxOffset { get; private set; }

        // What the shader reconstructs against the source, meters.
        public float MaxError { get; private set; }

        // Bounds and error use the decoded position: rest + d + Δ in the shader order.
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
