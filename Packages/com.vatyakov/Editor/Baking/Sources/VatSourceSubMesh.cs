using UnityEngine;

namespace VATyakov.Editor
{
    readonly struct VatSourceSubMesh
    {
        public readonly int[] Indices; // absolute, baseVertex already applied
        public readonly MeshTopology Topology;

        public VatSourceSubMesh(int[] indices, MeshTopology topology)
        {
            Indices = indices;
            Topology = topology;
        }
    }
}
