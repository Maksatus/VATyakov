using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace VATyakov.Editor
{
    // Bake step 1 (§1.8): built data must match the layout before anything on disk is touched.
    static class VatLayoutVerifier
    {
        public static void Verify(VatLayout layout, Mesh mesh, VatBakeTextures textures)
        {
            var info = layout.Info;
            var problems = new List<string>();
            AddMeshProblems(info, mesh, problems);
            AddTextureProblems(textures.Position, info.Width, info.Height, info.PositionFormat, "_VatPosTex", problems);
            AddTextureProblems(textures.Rotation, info.Width, info.Height, info.RotationFormat, "_VatRotTex", problems);
            AddTextureProblems(textures.Drift, VatMath.DriftWidth, info.TotalRows, info.DriftFormat, "_VatDriftTex", problems);
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

        static void AddTextureProblems(Texture2D texture, int width, int height, GraphicsFormat format, string name, List<string> problems)
        {
            if (texture.width != width || texture.height != height)
                problems.Add("size of " + name);
            if (texture.graphicsFormat != format || texture.mipmapCount != 1)
                problems.Add("format of " + name);
        }
    }
}
