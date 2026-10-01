using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    // §2.2: checked on every bake frame — vertex count, index hash, UV hash, sub-mesh split.
    sealed class VatAlembicTopology
    {
        readonly int _vertexCount;
        readonly Hash128 _indices;
        readonly Hash128 _uv;
        readonly Hash128 _subMeshes;

        VatAlembicTopology(int vertexCount, Hash128 indices, Hash128 uv, Hash128 subMeshes)
        {
            _vertexCount = vertexCount;
            _indices = indices;
            _uv = uv;
            _subMeshes = subMeshes;
        }

        public static VatAlembicTopology Capture(MeshFilter[] nodes)
        {
            int vertexCount = 0;
            Hash128 indices = default, uv = default, subMeshes = default;
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

        // Null when nothing changed.
        public string Difference(VatAlembicTopology other)
        {
            if (other._vertexCount != _vertexCount)
                return $"vertices {_vertexCount} → {other._vertexCount}";
            if (other._subMeshes != _subMeshes)
                return "sub-mesh split changed";
            if (other._indices != _indices)
                return "indices changed";
            return other._uv != _uv ? "UV changed" : null;
        }

        static void AppendSubMeshes(Mesh mesh, ref Hash128 indices, ref Hash128 subMeshes)
        {
            subMeshes.Append(mesh.vertexCount);
            subMeshes.Append(mesh.subMeshCount);
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                subMeshes.Append((int)mesh.GetTopology(s));
                subMeshes.Append(mesh.GetIndexCount(s));
                indices.Append(mesh.GetIndices(s, true));
            }
        }
    }
}
