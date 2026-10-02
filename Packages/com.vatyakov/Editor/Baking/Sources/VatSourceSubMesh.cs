using UnityEngine;

namespace VATyakov.Editor
{
    internal readonly struct VatSourceSubMesh
    {
        public readonly int[] Indices;
        public readonly MeshTopology Topology;

        public VatSourceSubMesh(int[] indices, MeshTopology topology)
        {
            Indices = indices;
            Topology = topology;
        }
    }
}
