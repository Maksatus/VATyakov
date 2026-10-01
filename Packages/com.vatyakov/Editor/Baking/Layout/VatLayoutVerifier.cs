using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace VATyakov.Editor
{
    // Bake step 1 (§1.8): built data must match the layout before anything on disk is touched.
    static class VatLayoutVerifier
    {
        public static void Verify(VatLayout layout, Mesh mesh, Texture2D position, Texture2D rotation)
        {
            var problems = new List<string>();
            AddMeshProblems(layout.Info, mesh, problems);
            AddTextureProblems(layout.Info, position, layout.Info.PositionFormat, "_VatPosTex", problems);
            AddTextureProblems(layout.Info, rotation, layout.Info.RotationFormat, "_VatRotTex", problems);
            if (problems.Count > 0)
                throw new VatBakeException("Built data does not match the layout: " + string.Join(", ", problems) + ".");
        }

        static void AddMeshProblems(VatLayoutInfo info, Mesh mesh, List<string> problems)
        {
            if (!mesh.GetVertexAttributes().SequenceEqual(VatVertexFormat.Attributes))
                problems.Add("vertex attributes");
            for (int stream = 0; stream < VatVertexFormat.Strides.Length; stream++)
                if (mesh.GetVertexBufferStride(stream) != VatVertexFormat.Strides[stream])
                    problems.Add($"stride of stream {stream}");
            if (mesh.vertexCount != info.Elements)
                problems.Add("vertex count");
            for (int i = 0; i < mesh.subMeshCount; i++)
                if (mesh.GetSubMesh(i).baseVertex != 0)
                    problems.Add($"baseVertex of sub-mesh {i}");
        }

        static void AddTextureProblems(VatLayoutInfo info, Texture2D texture, GraphicsFormat format, string name, List<string> problems)
        {
            if (texture.width != info.Width || texture.height != info.Height)
                problems.Add("size of " + name);
            if (texture.graphicsFormat != format || texture.mipmapCount != 1)
                problems.Add("format of " + name);
        }
    }
}
