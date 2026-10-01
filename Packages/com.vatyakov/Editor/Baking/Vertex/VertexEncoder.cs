using System;
using UnityEngine;

namespace VATyakov.Editor
{
    // §2.2, 1.1: positions only; normals and tangents stay the rest ones until 1.3.
    sealed class VertexEncoder
    {
        readonly VatLayout _layout;
        readonly VatSourceMesh _source;
        readonly VatPositionTexels _texels;
        readonly VatSubMeshes _subMeshes;
        readonly VatBoundsBuilder _bounds = new VatBoundsBuilder();
        readonly Vector3[] _decoded;
        VatRestPose _rest;

        public VertexEncoder(VatLayout layout, VatSourceMesh source)
        {
            RequireMatch(layout, source);
            _layout = layout;
            _source = source;
            _texels = new VatPositionTexels(layout.Info);
            _subMeshes = new VatSubMeshes(source);
            _decoded = new Vector3[source.VertexCount];
        }

        public VatQuantizationStats Stats { get; } = new VatQuantizationStats();

        public void AddFrame(int clip, int frame, VatFrame data)
        {
            _rest ??= VatRestPose.Capture(clip, frame, data);
            int row = _layout.Clips[clip].StartRow + frame;
            for (int v = 0; v < _source.VertexCount; v++)
                _decoded[v] = Encode(v, row, data.Positions[v], clip, frame);
            _subMeshes.Encapsulate(_decoded);
            _texels.MarkRow(row);
        }

        public Mesh BuildMesh(string name)
        {
            _texels.RequireComplete();
            return VatVertexMeshBuilder.Build(name, _rest, _source, _subMeshes, _bounds.Bounds);
        }

        public Texture2D BuildPositionTexture(string name) => _texels.Build(name);

        // Bounds and error use what the shader reconstructs, not the source value.
        Vector3 Encode(int vertex, int row, Vector3 position, int clip, int frame)
        {
            var delta = position - _rest.Positions[vertex];
            RequireFinite(delta, vertex, clip, frame);
            var decoded = _rest.Positions[vertex] + _texels.Write(vertex, row, delta);
            Stats.Add(delta, decoded - position);
            _bounds.Add(decoded);
            return decoded;
        }

        void RequireFinite(Vector3 delta, int vertex, int clip, int frame)
        {
            if (!float.IsFinite(delta.x) || !float.IsFinite(delta.y) || !float.IsFinite(delta.z))
                throw new VatBakeException($"Нечисловая позиция вертекса {vertex}: клип '{_layout.Clips[clip].Name}', кадр {frame}.");
        }

        static void RequireMatch(VatLayout layout, VatSourceMesh source)
        {
            if (layout.Info.Mode != VatMode.Vertex || layout.Info.Elements != source.VertexCount)
                throw new ArgumentException("Layout does not match the source mesh.", nameof(layout));
        }
    }
}
