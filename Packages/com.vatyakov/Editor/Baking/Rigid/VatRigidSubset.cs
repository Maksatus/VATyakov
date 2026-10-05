using System;

namespace VATyakov.Editor
{
    internal static class VatRigidSubset
    {
        public static VatSourceMesh Mesh(VatSourceMesh source, int[] vertices, string name)
        {
            var remap = new int[source.VertexCount];
            Array.Fill(remap, -1);
            for (var i = 0; i < vertices.Length; i++)
            {
                remap[vertices[i]] = i;
            }

            var subMeshes = Array.ConvertAll(source.SubMeshes, subMesh => SubMesh(subMesh, remap));
            var uv = source.Uv0 == null ? null : Array.ConvertAll(vertices, vertex => source.Uv0[vertex]);
            return new VatSourceMesh(name, vertices.Length, uv, subMeshes);
        }

        public static VatFrame Frame(VatFrame source, int[] vertices)
        {
            var frame = new VatFrame(vertices.Length);
            for (var i = 0; i < vertices.Length; i++)
            {
                frame.Positions[i] = source.Positions[vertices[i]];
                frame.Normals[i] = source.Normals[vertices[i]];
                frame.Tangents[i] = source.Tangents[vertices[i]];
            }

            return frame;
        }

        private static VatSourceSubMesh SubMesh(VatSourceSubMesh subMesh, int[] remap)
        {
            var size = VatRigidIslands.PrimitiveSize(subMesh);
            var source = subMesh.Indices;
            var indices = new int[source.Length];
            var count = 0;
            for (var start = 0; start + size <= source.Length; start += size)
            {
                if (remap[source[start]] < 0)
                {
                    continue;
                }

                for (var corner = 0; corner < size; corner++)
                {
                    indices[count] = remap[source[start + corner]];
                    count++;
                }
            }

            Array.Resize(ref indices, count);
            return new VatSourceSubMesh(indices, subMesh.Topology);
        }
    }
}
