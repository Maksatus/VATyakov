using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VATyakov.Editor
{
    // Bake step 1 (§1.8): built data must match the layout before anything on disk is touched.
    static class VatLayoutVerifier
    {
        public static void Verify(VatLayout layout, Mesh mesh, Texture2D position)
        {
            var problems = new List<string>();
            AddMeshProblems(layout.Info, mesh, problems);
            AddTextureProblems(layout.Info, position, problems);
            if (problems.Count > 0)
                throw new VatBakeException("Собранные данные не совпадают с раскладкой: " + string.Join(", ", problems) + ".");
        }

        static void AddMeshProblems(VatLayoutInfo info, Mesh mesh, List<string> problems)
        {
            if (!mesh.GetVertexAttributes().SequenceEqual(VatVertexFormat.Attributes))
                problems.Add("вертексные атрибуты");
            for (int stream = 0; stream < VatVertexFormat.Strides.Length; stream++)
                if (mesh.GetVertexBufferStride(stream) != VatVertexFormat.Strides[stream])
                    problems.Add($"stride потока {stream}");
            if (mesh.vertexCount != info.Elements)
                problems.Add("число вертексов");
            for (int i = 0; i < mesh.subMeshCount; i++)
                if (mesh.GetSubMesh(i).baseVertex != 0)
                    problems.Add($"baseVertex сабмеша {i}");
        }

        static void AddTextureProblems(VatLayoutInfo info, Texture2D position, List<string> problems)
        {
            if (position.width != info.Width || position.height != info.Height)
                problems.Add("размер _VatPosTex");
            if (position.graphicsFormat != VatVertexFormat.Position || position.mipmapCount != 1)
                problems.Add("формат _VatPosTex");
        }
    }
}
