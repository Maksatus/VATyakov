using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatBoneSkin
    {
        public readonly VatSourceMesh Source;
        public readonly VatBoneInfluence[] Influences;
        public readonly VatRestPose Rest;

        public VatBoneSkin(VatSourceMesh source, VatBoneInfluence[] influences, VatRestPose rest)
        {
            Source = source;
            Influences = influences;
            Rest = rest;
        }

        public static VatBoneSkin Skinned(SkinnedMeshRenderer renderer, Matrix4x4 meshToRoot)
        {
            var mesh = renderer.sharedMesh;
            var source = VatSourceWinding.Oriented(VatSourceMesh.Read(mesh), meshToRoot);
            return new VatBoneSkin(source, VatSkinInfluences.Read(renderer), BindPose(mesh, new VatRootSpace(meshToRoot)));
        }

        private static VatRestPose BindPose(Mesh mesh, VatRootSpace space)
        {
            var frame = new VatFrame(mesh.vertexCount);
            var positions = mesh.vertices;
            var normals = mesh.normals;
            var tangents = mesh.tangents;
            for (var vertex = 0; vertex < positions.Length; vertex++)
            {
                frame.Positions[vertex] = space.Point(positions[vertex]);
                frame.Normals[vertex] = normals.Length == positions.Length ? space.Normal(normals[vertex]) : VatFrame.MissingNormal;
                frame.Tangents[vertex] = tangents.Length == positions.Length ? space.Tangent(tangents[vertex]) : VatFrame.MissingTangent;
            }

            var basis = new VatTangentFrames(positions.Length);
            basis.Build(0, frame);
            return new VatRestPose(frame, basis);
        }
    }
}
