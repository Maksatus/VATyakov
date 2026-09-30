using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kefir.Vat.Editor
{
    /// <summary>
    /// Frame source of the bake pipeline (§4): VatBakeProfile → IVatFrameSource → VatLayout → encoder → VatAssetWriter.
    /// SkinnedFrameSource today; the Alembic source implements the same contract.
    /// </summary>
    interface IVatFrameSource : IDisposable
    {
        VatSourceMesh Mesh { get; }

        IReadOnlyList<VatSourceClip> Clips { get; }

        /// <summary>Samples a clip at a time in seconds from its start. Data is in prefab-root space.</summary>
        void Sample(int clip, double time, VatFrame frame);
    }

    readonly struct VatSourceClip
    {
        public readonly string Name;
        public readonly float Length;

        public VatSourceClip(string name, float length)
        {
            Name = name;
            Length = length;
        }
    }

    /// <summary>Data shared by all frames: topology and UVs (§1.7).</summary>
    sealed class VatSourceMesh
    {
        public readonly string Name;
        public readonly int VertexCount;
        public readonly Vector2[] Uv0;
        public readonly SubMesh[] SubMeshes;

        public VatSourceMesh(string name, int vertexCount, Vector2[] uv0, SubMesh[] subMeshes)
        {
            Name = name;
            VertexCount = vertexCount;
            Uv0 = uv0;
            SubMeshes = subMeshes;
        }

        public readonly struct SubMesh
        {
            /// <summary>Absolute vertex indices, baseVertex already applied.</summary>
            public readonly int[] Indices;
            public readonly MeshTopology Topology;

            public SubMesh(int[] indices, MeshTopology topology)
            {
                Indices = indices;
                Topology = topology;
            }
        }
    }

    /// <summary>Vertex data of one frame in prefab-root space. Normals are unit length.</summary>
    sealed class VatFrame
    {
        public readonly Vector3[] Positions;
        public readonly Vector3[] Normals;
        public readonly Vector4[] Tangents;

        public VatFrame(int vertexCount)
        {
            Positions = new Vector3[vertexCount];
            Normals = new Vector3[vertexCount];
            Tangents = new Vector4[vertexCount];
        }
    }
}
