using System;
using UnityEngine;

namespace VATyakov.Editor
{
    // §2.2: positions in _VatPosTex, the frame (T, N×T, N) in _VatRotTex.
    sealed class VertexEncoder
    {
        readonly VatLayout _layout;
        readonly VatSourceMesh _source;
        readonly VatPositionTexels _positions;
        readonly VatRotationTexels _rotations;
        readonly VatTangentFrames _frames;
        readonly VatSubMeshes _subMeshes;
        readonly VatBoundsBuilder _bounds = new VatBoundsBuilder();
        readonly Vector3[] _decoded;
        VatRestPose _rest;

        public VertexEncoder(VatLayout layout, VatSourceMesh source)
        {
            RequireMatch(layout, source);
            _layout = layout;
            _source = source;
            _positions = new VatPositionTexels(layout.Info);
            _rotations = new VatRotationTexels(layout.Info);
            _frames = new VatTangentFrames(source.VertexCount);
            _subMeshes = new VatSubMeshes(source);
            _decoded = new Vector3[source.VertexCount];
            Chirality = new VatChirality(source.VertexCount);
        }

        public VatQuantizationStats Stats { get; } = new VatQuantizationStats();

        public VatChirality Chirality { get; }

        public void AddFrame(int clip, int frame, VatFrame data)
        {
            Stats.AddDegenerate(_frames.Build(frame, data));
            _rest ??= VatRestPose.Capture(clip, frame, data, _frames);
            Chirality.Check(_rest, data, _layout.Clips[clip].Name, frame);
            int row = _layout.Clips[clip].StartRow + frame;
            for (int v = 0; v < _source.VertexCount; v++)
            {
                _decoded[v] = EncodePosition(v, row, data.Positions[v], clip, frame);
                EncodeRotation(v, row);
            }
            _subMeshes.Encapsulate(_decoded);
            _positions.MarkRow(row);
        }

        public Mesh BuildMesh(string name)
        {
            _positions.RequireComplete();
            return VatVertexMeshBuilder.Build(name, _rest, _source, _subMeshes, _bounds.Bounds);
        }

        public Texture2D BuildPositionTexture(string name) => _positions.Build(name);

        public Texture2D BuildRotationTexture(string name)
        {
            _positions.RequireComplete(); // both textures are written row by row together
            return _rotations.Build(name);
        }

        // Bounds and error use what the shader reconstructs, not the source value.
        Vector3 EncodePosition(int vertex, int row, Vector3 position, int clip, int frame)
        {
            var delta = position - _rest.Positions[vertex];
            RequireFinite(delta, vertex, clip, frame);
            var decoded = _rest.Positions[vertex] + _positions.Write(vertex, row, delta);
            Stats.Add(delta, decoded - position);
            _bounds.Add(decoded);
            return decoded;
        }

        void EncodeRotation(int vertex, int row)
        {
            var n = _frames.Normals[vertex];
            var t = _frames.Tangents[vertex];
            var q = _rotations.Write(vertex, row, VatTangentFrames.Rotation(n, t));
            Stats.AddRotation(Vector3.Angle(n, VatMath.FrameNormal(q)), Vector3.Angle(t, VatMath.FrameTangent(q)));
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
