using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VATyakov.Dev
{
    internal sealed class VatDeformGeometry
    {
        private readonly MeshFilter[] _sources;
        private readonly MeshFilter _filter;
        private readonly List<Vector3> _vertices = new();
        private readonly List<Vector3> _normals = new();

        public GameObject Root { get; }

        public VatDeformGeometry(GameObject rig, string name)
        {
            _sources = rig.GetComponentsInChildren<MeshFilter>(true);
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            Root = new GameObject(name);
            var material = _sources[0].GetComponent<MeshRenderer>().sharedMaterial;
            _filter = VatRigidRecorder.Piece(Root.transform, $"{name}_mesh", mesh, material).GetComponent<MeshFilter>();
            Update();
            mesh.SetTriangles(Triangles(), 0);
            mesh.SetUVs(0, Uvs());
        }

        public void Update()
        {
            _vertices.Clear();
            _normals.Clear();
            foreach (var source in _sources)
            {
                var matrix = source.transform.localToWorldMatrix;
                var mesh = source.sharedMesh;
                foreach (var vertex in mesh.vertices)
                {
                    _vertices.Add(matrix.MultiplyPoint3x4(vertex));
                }

                foreach (var normal in mesh.normals)
                {
                    _normals.Add(matrix.MultiplyVector(normal).normalized);
                }
            }

            var target = _filter.sharedMesh;
            target.SetVertices(_vertices);
            target.SetNormals(_normals);
            target.RecalculateBounds();
        }

        private List<int> Triangles()
        {
            var triangles = new List<int>();
            var offset = 0;
            foreach (var source in _sources)
            {
                var mesh = source.sharedMesh;
                foreach (var index in mesh.triangles)
                {
                    triangles.Add(offset + index);
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
