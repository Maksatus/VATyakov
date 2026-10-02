using System;
using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatSubMeshes
    {
        private readonly int[][] _vertices;
        private readonly VatBoundsBuilder[] _bounds;

        public VatSubMeshes(VatSourceMesh source)
        {
            _vertices = Array.ConvertAll(source.SubMeshes, subMesh => UniqueSorted(subMesh.Indices));
            _bounds = Array.ConvertAll(_vertices, _ => new VatBoundsBuilder());
        }

        public void Encapsulate(Vector3[] positions)
        {
            for (var subMesh = 0; subMesh < _vertices.Length; subMesh++)
            {
                foreach (var vertex in _vertices[subMesh])
                {
                    _bounds[subMesh].Add(positions[vertex]);
                }
            }
        }

        public Bounds Bounds(int subMesh)
        {
            return _bounds[subMesh].Bounds;
        }

        public int FirstVertex(int subMesh)
        {
            return _vertices[subMesh].Length > 0 ? _vertices[subMesh][0] : 0;
        }

        public int VertexCount(int subMesh)
        {
            var vertices = _vertices[subMesh];
            return vertices.Length > 0 ? vertices[vertices.Length - 1] - vertices[0] + 1 : 0;
        }

        private static int[] UniqueSorted(int[] indices)
        {
            var result = new List<int>(new HashSet<int>(indices));
            result.Sort();
            return result.ToArray();
        }
    }
}
