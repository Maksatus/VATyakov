using System;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VertexEncoder
    {
        private readonly VatLayout _layout;
        private readonly VatSourceMesh _source;
        private readonly VatPositions _positions;
        private readonly VatDriftTexels _drift;
        private readonly VatRotationTexels _rotations;
        private readonly VatTangentFrames _frames;
        private VatRestPose _rest;

        public VatQuantizationStats Stats { get; } = new();
        public VatChirality Chirality { get; }
        public VatLayout Layout => _layout;
        public VatPrecision Precision => new VatPrecision(_drift.MaxDistance, _positions.MaxError);
        public float MaxOffset => _positions.MaxOffset;

        public VertexEncoder(VatLayout layout, VatSourceMesh source)
        {
            RequireMatch(layout, source);
            _layout = layout;
            _source = source;
            _positions = new VatPositions(layout.Info, source);
            _drift = new VatDriftTexels(layout.Info);
            _rotations = new VatRotationTexels(layout.Info);
            _frames = new VatTangentFrames(source.VertexCount);
            Chirality = new VatChirality(source.VertexCount);
        }

        public void AddFrame(int clip, int frame, VatFrame data)
        {
            Stats.AddDegenerate(_frames.Build(frame, data));
            _rest ??= VatRestPose.Capture(clip, frame, data, _frames);
            Chirality.Check(_rest, data, _layout.Clips[clip].Name, frame);
            var row = _layout.Clips[clip].StartRow + frame;
            var drift = _drift.Write(row, VatCentroid.Of(data.Positions) - _rest.Centroid);
            for (var v = 0; v < _source.VertexCount; v++)
            {
                EncodePosition(v, row, data.Positions[v], drift, clip, frame);
                EncodeRotation(v, row);
            }

            _positions.EndRow(row);
        }

        public Mesh BuildMesh(string name)
        {
            _positions.Texels.RequireComplete();
            return VatVertexMeshBuilder.Build(name, _rest, _source, _positions.SubMeshes, _positions.Bounds);
        }

        public Texture2D BuildPositionTexture(string name)
        {
            return _positions.Texels.Build(name);
        }

        public Texture2D BuildRotationTexture(string name)
        {
            _positions.Texels.RequireComplete();
            return _rotations.Build(name);
        }

        public Texture2D BuildDriftTexture(string name)
        {
            _positions.Texels.RequireComplete();
            return _drift.Build(name);
        }

        private void EncodePosition(int vertex, int row, Vector3 position, Vector3 drift, int clip, int frame)
        {
            var rest = _rest.Positions[vertex];
            RequireFinite(position - rest, vertex, clip, frame);
            _positions.Write(vertex, row, position, rest, drift);
        }

        private void EncodeRotation(int vertex, int row)
        {
            var n = _frames.Normals[vertex];
            var t = _frames.Tangents[vertex];
            var q = _rotations.Write(vertex, row, VatTangentFrames.Rotation(n, t));
            Stats.AddRotation(Vector3.Angle(n, VatMath.FrameNormal(q)), Vector3.Angle(t, VatMath.FrameTangent(q)));
        }

        private void RequireFinite(Vector3 delta, int vertex, int clip, int frame)
        {
            if (!float.IsFinite(delta.x) || !float.IsFinite(delta.y) || !float.IsFinite(delta.z))
            {
                throw new VatBakeException($"Non-finite position of vertex {vertex}: clip '{_layout.Clips[clip].Name}', frame {frame}.");
            }
        }

        private static void RequireMatch(VatLayout layout, VatSourceMesh source)
        {
            if (layout.Info.Elements != source.VertexCount)
            {
                throw new ArgumentException("Layout does not match the source mesh.", nameof(layout));
            }
        }
    }
}
