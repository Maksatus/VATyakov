using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatBoneSkin
    {
        public readonly string Name;
        public readonly VatSourceMesh Source;
        public readonly VatBoneInfluence[] Influences;
        public readonly VatRestPose Rest;

        public VatBoneSkin(string name, VatSourceMesh source, VatBoneInfluence[] influences, VatRestPose rest)
        {
            Name = name;
            Source = source;
            Influences = influences;
            Rest = rest;
        }

        private VatBoneSkin(string name, Mesh mesh, VatBoneInfluence[] influences, VatRootSpace space) :
            this(name, VatSourceMesh.Read(mesh), influences, BindPose(mesh, space))
        {
        }

        public static VatBoneSkin Skinned(SkinnedMeshRenderer renderer, int[] boneMap, VatRootSpace space)
        {
            var influences = VatSkinInfluences.Remap(VatSkinInfluences.Read(renderer), boneMap);
            return new VatBoneSkin(renderer.name, renderer.sharedMesh, influences, space);
        }

        public static VatBoneSkin Rigid(MeshRenderer renderer, int bone, VatRootSpace space)
        {
            var mesh = VatBoneBinding.RigidMesh(renderer);
            return new VatBoneSkin(renderer.name, mesh, VatSkinInfluences.Single(mesh.vertexCount, bone), space);
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
