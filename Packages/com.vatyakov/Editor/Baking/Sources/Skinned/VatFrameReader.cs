using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VATyakov.Editor
{
    internal sealed class VatFrameReader : IDisposable
    {
        private const string BakedMeshName = "VatBakeFrame";

        private readonly int _vertexCount;
        private readonly Mesh _baked = new() { name = BakedMeshName };
        private readonly List<Vector3> _positions = new();
        private readonly List<Vector3> _normals = new();
        private readonly List<Vector4> _tangents = new();

        public VatFrameReader(int vertexCount)
        {
            _vertexCount = vertexCount;
        }

        public void Read(VatBakeCopy copy, VatFrame frame)
        {
            Bake(copy.Renderer);
            var space = new VatRootSpace(copy.RendererToRoot);
            WritePositions(space, frame);
            WriteNormals(space, frame);
            WriteTangents(space, frame);
        }

        public void Dispose()
        {
            Object.DestroyImmediate(_baked);
        }

        private void Bake(SkinnedMeshRenderer renderer)
        {
            renderer.BakeMesh(_baked, true);
            _baked.GetVertices(_positions);
            _baked.GetNormals(_normals);
            _baked.GetTangents(_tangents);
            if (_positions.Count != _vertexCount)
            {
                throw new InvalidOperationException(
                    FormattableString.Invariant($"BakeMesh returned {_positions.Count} vertices, the source mesh has {_vertexCount}."));
            }
        }

        private void WritePositions(VatRootSpace space, VatFrame frame)
        {
            for (var i = 0; i < _vertexCount; i++)
            {
                frame.Positions[i] = space.Point(_positions[i]);
            }
        }

        private void WriteNormals(VatRootSpace space, VatFrame frame)
        {
            var hasNormals = _normals.Count == _vertexCount;
            for (var i = 0; i < _vertexCount; i++)
            {
                frame.Normals[i] = hasNormals ? space.Normal(_normals[i]) : VatFrame.MissingNormal;
            }
        }

        private void WriteTangents(VatRootSpace space, VatFrame frame)
        {
            var hasTangents = _tangents.Count == _vertexCount;
            for (var i = 0; i < _vertexCount; i++)
            {
                frame.Tangents[i] = hasTangents ? space.Tangent(_tangents[i]) : VatFrame.MissingTangent;
            }
        }
    }
}
