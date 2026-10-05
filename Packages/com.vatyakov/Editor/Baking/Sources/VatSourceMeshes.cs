using System;
using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatSourceMeshes
    {
        public static VatSourceMesh Combine(string name, VatSourceMesh[] parts)
        {
            var vertexCount = 0;
            var subMeshes = new List<VatSourceSubMesh>();
            foreach (var part in parts)
            {
                foreach (var subMesh in part.SubMeshes)
                {
                    subMeshes.Add(Shift(subMesh, vertexCount));
                }

                vertexCount += part.VertexCount;
            }

            return new VatSourceMesh(name, vertexCount, CombineUv(parts, vertexCount), subMeshes.ToArray());
        }

        public static VatSourceMesh MergeSlots(string name, VatSourceMesh[] parts)
        {
            var vertexCount = 0;
            var slots = new List<List<int>>();
            var topologies = new List<MeshTopology>();
            foreach (var part in parts)
            {
                for (var slot = 0; slot < part.SubMeshes.Length; slot++)
                {
                    AddToSlot(slots, topologies, slot, Shift(part.SubMeshes[slot], vertexCount), part.Name);
                }

                vertexCount += part.VertexCount;
            }

            var subMeshes = new VatSourceSubMesh[slots.Count];
            for (var slot = 0; slot < subMeshes.Length; slot++)
            {
                subMeshes[slot] = new VatSourceSubMesh(slots[slot].ToArray(), topologies[slot]);
            }

            return new VatSourceMesh(name, vertexCount, CombineUv(parts, vertexCount), subMeshes);
        }

        private static void AddToSlot(List<List<int>> slots, List<MeshTopology> topologies, int slot, VatSourceSubMesh subMesh, string part)
        {
            if (slot == slots.Count)
            {
                slots.Add(new List<int>());
                topologies.Add(subMesh.Topology);
            }

            if (topologies[slot] != subMesh.Topology)
            {
                throw new VatBakeException(
                    FormattableString.Invariant($"'{part}': submesh {slot} is {subMesh.Topology}, other pieces have {topologies[slot]} there."));
            }

            slots[slot].AddRange(subMesh.Indices);
        }

        private static VatSourceSubMesh Shift(VatSourceSubMesh subMesh, int offset)
        {
            var indices = new int[subMesh.Indices.Length];
            for (var i = 0; i < indices.Length; i++)
            {
                indices[i] = subMesh.Indices[i] + offset;
            }

            return new VatSourceSubMesh(indices, subMesh.Topology);
        }

        private static Vector2[] CombineUv(VatSourceMesh[] parts, int vertexCount)
        {
            if (Array.TrueForAll(parts, part => part.Uv0 == null))
            {
                return null;
            }

            var uv = new Vector2[vertexCount];
            var offset = 0;
            foreach (var part in parts)
            {
                part.Uv0?.CopyTo(uv, offset);
                offset += part.VertexCount;
            }

            return uv;
        }
    }
}
