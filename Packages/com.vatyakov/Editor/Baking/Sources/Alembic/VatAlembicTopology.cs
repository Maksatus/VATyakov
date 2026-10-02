using System;
using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatAlembicTopology
    {
        private readonly int _vertexCount;
        private readonly Hash128 _indices;
        private readonly Hash128 _uv;
        private readonly Hash128 _subMeshes;

        private VatAlembicTopology(int vertexCount, Hash128 indices, Hash128 uv, Hash128 subMeshes)
        {
            _vertexCount = vertexCount;
            _indices = indices;
            _uv = uv;
            _subMeshes = subMeshes;
        }

        public static VatAlembicTopology Capture(MeshFilter[] nodes)
        {
            var vertexCount = 0;
            var indices = new Hash128();
            var uv = new Hash128();
            var subMeshes = new Hash128();
            var uvs = new List<Vector2>();
            subMeshes.Append(nodes.Length);
            foreach (var node in nodes)
            {
                var mesh = node.sharedMesh;
                vertexCount += mesh.vertexCount;
                AppendSubMeshes(mesh, ref indices, ref subMeshes);
                mesh.GetUVs(0, uvs);
                uv.Append(uvs);
            }

            return new VatAlembicTopology(vertexCount, indices, uv, subMeshes);
        }

        public string Difference(VatAlembicTopology other)
        {
            if (other._vertexCount != _vertexCount)
            {
                return FormattableString.Invariant($"vertices {_vertexCount} → {other._vertexCount}");
            }

            if (other._subMeshes != _subMeshes)
            {
                return "sub-mesh split changed";
            }

            if (other._indices != _indices)
            {
                return "indices changed";
            }

            return other._uv != _uv ? "UV changed" : null;
        }

        private static void AppendSubMeshes(Mesh mesh, ref Hash128 indices, ref Hash128 subMeshes)
        {
            subMeshes.Append(mesh.vertexCount);
            subMeshes.Append(mesh.subMeshCount);
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                subMeshes.Append((int)mesh.GetTopology(subMesh));
                subMeshes.Append(mesh.GetIndexCount(subMesh));
                indices.Append(mesh.GetIndices(subMesh, true));
            }
        }
    }
}
