using System;
using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    // Vertex sets and bounds of each submesh; §1.8: bounds are the union over all frames.
    sealed class VatSubMeshes
    {
        readonly int[][] _vertices;
        readonly VatBoundsBuilder[] _bounds;

        public VatSubMeshes(VatSourceMesh source)
        {
            _vertices = Array.ConvertAll(source.SubMeshes, s => UniqueSorted(s.Indices));
            _bounds = Array.ConvertAll(_vertices, _ => new VatBoundsBuilder());
        }

        public void Encapsulate(Vector3[] positions)
        {
            for (int s = 0; s < _vertices.Length; s++)
                foreach (int v in _vertices[s])
                    _bounds[s].Add(positions[v]);
        }

        public Bounds Bounds(int subMesh) => _bounds[subMesh].Bounds;

        public int FirstVertex(int subMesh) => _vertices[subMesh].Length > 0 ? _vertices[subMesh][0] : 0;

        public int VertexCount(int subMesh)
        {
            var vertices = _vertices[subMesh];
            return vertices.Length > 0 ? vertices[vertices.Length - 1] - vertices[0] + 1 : 0;
        }

        static int[] UniqueSorted(int[] indices)
        {
            var result = new List<int>(new HashSet<int>(indices));
            result.Sort();
            return result.ToArray();
        }
    }
}
