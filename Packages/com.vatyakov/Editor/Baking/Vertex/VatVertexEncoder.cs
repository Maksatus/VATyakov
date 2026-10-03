using System;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatVertexEncoder
    {
        public readonly VatLayout Layout;
        public readonly VatQuantizationStats Stats = new();
        public readonly VatChirality Chirality;
        public readonly VatRotationSigns Signs;

        private readonly VatSourceMesh _source;
        private readonly VatPositions _positions;
        private readonly VatDriftRows _drift;
        private readonly VatRotationTexels _rotations;
        private readonly VatTangentFrames _frames;

        private VatRestPose _rest;

        public VatPrecision Precision => new VatPrecision(_drift.MaxDistance, _positions.MaxError);
        public float MaxOffset => _positions.MaxOffset;
        public Vector3[] Drift => _drift.Rows;

        public VatVertexEncoder(VatLayout layout, VatSourceMesh source)
        {
            Layout = layout;
            _source = source;
            _positions = new VatPositions(layout.Info, source);
            _drift = new VatDriftRows(layout.Info);
            _rotations = new VatRotationTexels(layout.Info);
            _frames = new VatTangentFrames(source.VertexCount);
            Chirality = new VatChirality(source.VertexCount);
            Signs = new VatRotationSigns(source.VertexCount);
        }

        public void AddFrame(int clip, int frame, VatFrame data)
        {
            var clipLayout = Layout.Clips[clip];
            Stats.AddDegenerate(_frames.Build(frame, data));
            _rest ??= new VatRestPose(data, _frames);
            Chirality.Check(_rest, data, clipLayout.Name, frame);
            var row = clipLayout.StartRow + frame;
            var drift = VatCentroid.Of(data.Positions) - _rest.Centroid;
            _drift.Write(row, drift);
            for (var vertex = 0; vertex < _source.VertexCount; vertex++)
            {
                EncodePosition(vertex, row, data.Positions[vertex], drift, clip, frame);
                EncodeRotation(vertex, row, frame);
            }

            _positions.EndRow(row);
            if (clipLayout.IsLooping && frame == clipLayout.FrameCount - 1)
            {
                Signs.CloseLoop(clipLayout.Name);
            }
        }

        public Mesh BuildMesh(string name)
        {
            return VatVertexMeshBuilder.Build(name, _rest, _source, _positions.SubMeshes, _positions.Bounds);
        }

        public Texture2D BuildPositionTexture(string name)
        {
            return _positions.Texels.Build(name);
        }

        public Texture2D BuildRotationTexture(string name)
        {
            return _rotations.Build(name);
        }

        private void EncodePosition(int vertex, int row, Vector3 position, Vector3 drift, int clip, int frame)
        {
            var rest = _rest.Positions[vertex];
            RequireFinite(position - rest, vertex, clip, frame);
            _positions.Write(vertex, row, position, rest, drift);
        }

        private void EncodeRotation(int vertex, int row, int frame)
        {
            var normal = _frames.Normals[vertex];
            var tangent = _frames.Tangents[vertex];
            var rotation = _rotations.Write(vertex, row, Signs.Align(vertex, frame, VatTangentFrames.Rotation(normal, tangent)));
            Stats.AddRotation(Vector3.Angle(normal, VatMath.FrameNormal(rotation)), Vector3.Angle(tangent, VatMath.FrameTangent(rotation)));
        }

        private void RequireFinite(Vector3 delta, int vertex, int clip, int frame)
        {
            if (!float.IsFinite(delta.x) || !float.IsFinite(delta.y) || !float.IsFinite(delta.z))
            {
                throw new VatBakeException(
                    FormattableString.Invariant($"Non-finite position of vertex {vertex}: clip '{Layout.Clips[clip].Name}', frame {frame}."));
            }
        }
    }
}
