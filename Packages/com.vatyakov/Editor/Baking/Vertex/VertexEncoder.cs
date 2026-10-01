using System;
using UnityEngine;

namespace VATyakov.Editor
{
    // §2.2: positions in _VatPosTex and _VatDriftTex, the frame (T, N×T, N) in _VatRotTex.
    // Positions are written with drift and without it; drift is chosen after the last frame (VatDriftPolicy).
    sealed class VertexEncoder
    {
        readonly VatLayout _layout;
        readonly VatSourceMesh _source;
        readonly bool _allowDrift;
        readonly VatPositionVariant _plain;
        readonly VatPositionVariant _drifted;
        readonly VatDriftTexels _drift;
        readonly VatRotationTexels _rotations;
        readonly VatTangentFrames _frames;
        VatRestPose _rest;

        // allowDrift = false only for the dev comparison (VatBakeProfile.NoDrift).
        public VertexEncoder(VatLayout layout, VatSourceMesh source, bool allowDrift = true)
        {
            RequireMatch(layout, source);
            _layout = layout;
            _source = source;
            _allowDrift = allowDrift;
            _plain = new VatPositionVariant(layout.Info, source);
            _drifted = new VatPositionVariant(layout.Info, source);
            _drift = new VatDriftTexels(layout.Info);
            _rotations = new VatRotationTexels(layout.Info);
            _frames = new VatTangentFrames(source.VertexCount);
            Chirality = new VatChirality(source.VertexCount);
        }

        public VatQuantizationStats Stats { get; } = new VatQuantizationStats();

        public VatChirality Chirality { get; }

        public bool DriftOn => _allowDrift && VatDriftPolicy.IsNeeded(_drift.MaxDistance, _plain.MaxError);

        public VatLayout Layout => _layout.WithDrift(DriftOn);

        public VatPrecision Precision => new VatPrecision(_drift.MaxDistance, _plain.MaxError, _drifted.MaxError);

        public float MaxOffset => Positions.MaxOffset;

        VatPositionVariant Positions => DriftOn ? _drifted : _plain;

        public void AddFrame(int clip, int frame, VatFrame data)
        {
            Stats.AddDegenerate(_frames.Build(frame, data));
            _rest ??= VatRestPose.Capture(clip, frame, data, _frames);
            Chirality.Check(_rest, data, _layout.Clips[clip].Name, frame);
            int row = _layout.Clips[clip].StartRow + frame;
            var drift = _drift.Write(row, VatCentroid.Of(data.Positions) - _rest.Centroid);
            for (int v = 0; v < _source.VertexCount; v++)
            {
                EncodePosition(v, row, data.Positions[v], drift, clip, frame);
                EncodeRotation(v, row);
            }
            _plain.EndRow(row);
            _drifted.EndRow(row);
        }

        public Mesh BuildMesh(string name)
        {
            Positions.Texels.RequireComplete();
            return VatVertexMeshBuilder.Build(name, _rest, _source, Positions.SubMeshes, Positions.Bounds);
        }

        public Texture2D BuildPositionTexture(string name) => Positions.Texels.Build(name);

        public Texture2D BuildRotationTexture(string name)
        {
            Positions.Texels.RequireComplete(); // all textures are written row by row together
            return _rotations.Build(name);
        }

        public Texture2D BuildDriftTexture(string name)
        {
            Positions.Texels.RequireComplete();
            return DriftOn ? _drift.Build(name) : VatDriftTexels.Zero(_layout.Info, name);
        }

        void EncodePosition(int vertex, int row, Vector3 position, Vector3 drift, int clip, int frame)
        {
            var rest = _rest.Positions[vertex];
            RequireFinite(position - rest, vertex, clip, frame);
            _plain.Write(vertex, row, position, rest, Vector3.zero);
            _drifted.Write(vertex, row, position, rest, drift);
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
                throw new VatBakeException($"Non-finite position of vertex {vertex}: clip '{_layout.Clips[clip].Name}', frame {frame}.");
        }

        static void RequireMatch(VatLayout layout, VatSourceMesh source)
        {
            if (layout.Info.Mode != VatMode.Vertex || layout.Info.Elements != source.VertexCount)
                throw new ArgumentException("Layout does not match the source mesh.", nameof(layout));
        }
    }
}
