using System;
using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    // All mesh nodes, in hierarchy order, as one mesh in root space; node transforms are animated too.
    sealed class VatAlembicReader
    {
        static readonly Vector4 MissingTangent = new Vector4(0f, 0f, 0f, 1f); // degenerate: VatTangentFrames builds it from N

        readonly MeshFilter[] _nodes;
        readonly List<Vector3> _positions = new List<Vector3>();
        readonly List<Vector3> _normals = new List<Vector3>();
        readonly List<Vector4> _tangents = new List<Vector4>();

        public VatAlembicReader(MeshFilter[] nodes)
        {
            _nodes = nodes;
        }

        public void Read(VatFrame frame)
        {
            int offset = 0;
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

        void ReadNode(Mesh mesh)
        {
            mesh.GetVertices(_positions);
            mesh.GetNormals(_normals);
            mesh.GetTangents(_tangents);
        }

        // A missing normal stays zero: VatTangentFrames.Normal treats it as absent.
        void Write(VatRootSpace space, VatFrame frame, int offset)
        {
            bool normals = _normals.Count == _positions.Count;
            bool tangents = _tangents.Count == _positions.Count;
            for (int i = 0; i < _positions.Count; i++)
            {
                frame.Positions[offset + i] = space.Point(_positions[i]);
                frame.Normals[offset + i] = normals ? space.Normal(_normals[i]) : Vector3.zero;
                frame.Tangents[offset + i] = tangents ? space.Tangent(_tangents[i]) : MissingTangent;
            }
        }
    }
}
