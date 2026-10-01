using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VATyakov.Editor
{
    // BakeMesh includes blend shapes; the result goes to prefab-root space.
    sealed class VatFrameReader : IDisposable
    {
        readonly int _vertexCount;
        readonly Mesh _baked = new Mesh { name = "VatBakeFrame" };
        readonly List<Vector3> _positions = new List<Vector3>();
        readonly List<Vector3> _normals = new List<Vector3>();
        readonly List<Vector4> _tangents = new List<Vector4>();

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

        public void Dispose() => Object.DestroyImmediate(_baked);

        void Bake(SkinnedMeshRenderer renderer)
        {
            renderer.BakeMesh(_baked, true); // renderer's local space, scale included
            _baked.GetVertices(_positions);
            _baked.GetNormals(_normals);
            _baked.GetTangents(_tangents);
            if (_positions.Count != _vertexCount)
                throw new InvalidOperationException($"BakeMesh returned {_positions.Count} vertices, the source mesh has {_vertexCount}.");
        }

        void WritePositions(VatRootSpace space, VatFrame frame)
        {
            for (int i = 0; i < _vertexCount; i++)
                frame.Positions[i] = space.Point(_positions[i]);
        }

        void WriteNormals(VatRootSpace space, VatFrame frame)
        {
            bool present = _normals.Count == _vertexCount;
            for (int i = 0; i < _vertexCount; i++)
                frame.Normals[i] = present ? space.Normal(_normals[i]) : Vector3.forward;
        }

        void WriteTangents(VatRootSpace space, VatFrame frame)
        {
            bool present = _tangents.Count == _vertexCount;
            for (int i = 0; i < _vertexCount; i++)
                frame.Tangents[i] = present ? space.Tangent(_tangents[i]) : new Vector4(1f, 0f, 0f, 1f);
        }
    }
}
