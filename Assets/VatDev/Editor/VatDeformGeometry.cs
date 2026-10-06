using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace VATyakov.Dev
{
    internal sealed class VatDeformGeometry : IDisposable
    {
        private readonly MeshFilter[] _sources;
        private readonly MeshFilter _filter;
        private readonly HashSet<Mesh> _meshes = new();
        private readonly List<Vector3> _vertices = new();
        private readonly List<Vector3> _normals = new();

        public GameObject Root { get; }

        public VatDeformGeometry(GameObject rig, string name)
        {
            _sources = rig.GetComponentsInChildren<MeshFilter>(true);
            var material = _sources[0].GetComponent<MeshRenderer>().sharedMaterial;
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            _meshes.Add(mesh);
            Root = new GameObject(name);
            _filter = VatRigidRecorder.Piece(Root.transform, $"{name}_mesh", mesh, material).GetComponent<MeshFilter>();
            Update();
            var slotCount = _sources.Max(source => source.sharedMesh.subMeshCount);
            mesh.subMeshCount = slotCount;
            for (var slot = 0; slot < slotCount; slot++)
            {
                mesh.SetTriangles(Triangles(slot), slot);
            }

            mesh.SetUVs(0, Uvs());
        }

        public void Update()
        {
            _vertices.Clear();
            _normals.Clear();
            foreach (var source in _sources)
            {
                var matrix = source.transform.localToWorldMatrix;
                var normalMatrix = matrix.inverse.transpose;
                var mesh = source.sharedMesh;
                foreach (var vertex in mesh.vertices)
                {
                    _vertices.Add(matrix.MultiplyPoint3x4(vertex));
                }

                foreach (var normal in mesh.normals)
                {
                    _normals.Add(normalMatrix.MultiplyVector(normal).normalized);
                }
            }

            var target = _filter.sharedMesh;
            _meshes.Add(target);
            target.SetVertices(_vertices);
            target.SetNormals(_normals);
            target.RecalculateBounds();
        }

        public void Dispose()
        {
            foreach (var mesh in _meshes)
            {
                Object.DestroyImmediate(mesh);
            }

            _meshes.Clear();
        }

        private List<int> Triangles(int slot)
        {
            var triangles = new List<int>();
            var offset = 0;
            foreach (var source in _sources)
            {
                var mesh = source.sharedMesh;
                if (slot < mesh.subMeshCount)
                {
                    foreach (var index in mesh.GetTriangles(slot))
                    {
                        triangles.Add(offset + index);
                    }
                }

                offset += mesh.vertexCount;
            }

            return triangles;
        }

        private List<Vector2> Uvs()
        {
            var uvs = new List<Vector2>();
            foreach (var source in _sources)
            {
                uvs.AddRange(source.sharedMesh.uv);
            }

            return uvs;
        }
    }
}
