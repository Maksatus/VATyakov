using System;
using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatAlembicReader
    {
        private static readonly Vector4 _missingTangent = new(0f, 0f, 0f, 1f);

        private readonly MeshFilter[] _nodes;
        private readonly List<Vector3> _positions = new();
        private readonly List<Vector3> _normals = new();
        private readonly List<Vector4> _tangents = new();

        public VatAlembicReader(MeshFilter[] nodes)
        {
            _nodes = nodes;
        }

        public void Read(VatFrame frame)
        {
            var offset = 0;
            foreach (var node in _nodes)
            {
                ReadNode(node.sharedMesh);
                Write(new VatRootSpace(node.transform.localToWorldMatrix), frame, offset);
                offset += _positions.Count;
            }
        }

        public VatSourceMesh SourceMesh(string name)
        {
            var meshes = Array.ConvertAll(_nodes, node => VatSourceMesh.Read(node.sharedMesh));
            return VatSourceMeshes.Combine(name, meshes);
        }

        private void ReadNode(Mesh mesh)
        {
            mesh.GetVertices(_positions);
            mesh.GetNormals(_normals);
            mesh.GetTangents(_tangents);
        }

        private void Write(VatRootSpace space, VatFrame frame, int offset)
        {
            var normals = _normals.Count == _positions.Count;
            var tangents = _tangents.Count == _positions.Count;
            for (var i = 0; i < _positions.Count; i++)
            {
                frame.Positions[offset + i] = space.Point(_positions[i]);
                frame.Normals[offset + i] = normals ? space.Normal(_normals[i]) : Vector3.zero;
                frame.Tangents[offset + i] = tangents ? space.Tangent(_tangents[i]) : _missingTangent;
            }
        }
    }
}
