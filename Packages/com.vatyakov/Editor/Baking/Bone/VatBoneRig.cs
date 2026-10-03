using System;
using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatBoneRig
    {
        public readonly Transform[] Bones;
        public readonly VatBoneInfluence[] Influences;
        public readonly int[][] BoneVertices;
        public readonly Vector3[] Pivots;
        public readonly VatRestPose Rest;

        private readonly Matrix4x4[] _bindposes;
        private readonly Matrix4x4 _rootToMesh;

        public int BoneCount => Bones.Length;

        public VatBoneRig(VatBakeCopy copy)
        {
            var renderer = copy.Renderer;
            var mesh = renderer.sharedMesh;
            var meshToRoot = copy.RendererToRoot;
            Bones = renderer.bones;
            _bindposes = mesh.bindposes;
            _rootToMesh = meshToRoot.inverse;
            Influences = ReadInfluences(mesh.boneWeights, mesh.vertexCount, RootBone(renderer));
            BoneVertices = GroupByBone(Influences, Bones.Length);
            Pivots = HalfPivots(_bindposes, meshToRoot);
            Rest = BindPose(mesh, new VatRootSpace(meshToRoot));
        }

        public static string Problem(SkinnedMeshRenderer renderer)
        {
            var bones = renderer.bones;
            var bindposes = renderer.sharedMesh.bindposes;
            if (bones.Length == 0 || bindposes.Length == 0)
            {
                return $"'{renderer.name}' has no bones: Bone mode needs a skinned mesh with bindposes. Use Mode = Vertex.";
            }

            if (bones.Length != bindposes.Length)
            {
                return FormattableString.Invariant($"'{renderer.name}' has {bones.Length} bones and {bindposes.Length} bindposes.");
            }

            if (bones.Length > VatBoneFormat.MaxBones)
            {
                var count = FormattableString.Invariant($"'{renderer.name}' has {bones.Length} bones, Bone mode takes up to {VatBoneFormat.MaxBones}");
                return $"{count}. Use Mode = Vertex.";
            }

            var missing = Array.IndexOf(bones, null);
            return missing >= 0 ? FormattableString.Invariant($"Bone {missing} of '{renderer.name}' is missing.") : null;
        }

        public bool IsUsed(int bone)
        {
            return BoneVertices[bone].Length > 0;
        }

        public void ReadSkin(Transform root, Matrix4x4[] skin)
        {
            var worldToRoot = root.worldToLocalMatrix;
            for (var bone = 0; bone < Bones.Length; bone++)
            {
                skin[bone] = worldToRoot * Bones[bone].localToWorldMatrix * _bindposes[bone] * _rootToMesh;
            }
        }

        private static int RootBone(SkinnedMeshRenderer renderer)
        {
            return Math.Max(Array.IndexOf(renderer.bones, renderer.rootBone), 0);
        }

        private static VatBoneInfluence[] ReadInfluences(BoneWeight[] weights, int vertexCount, int rootBone)
        {
            var influences = new VatBoneInfluence[vertexCount];
            for (var vertex = 0; vertex < vertexCount; vertex++)
            {
                influences[vertex] = VatBoneInfluence.From(weights.Length == vertexCount ? weights[vertex] : default, rootBone);
            }

            return influences;
        }

        private static int[][] GroupByBone(VatBoneInfluence[] influences, int boneCount)
        {
            var groups = new List<int>[boneCount];
            for (var bone = 0; bone < boneCount; bone++)
            {
                groups[bone] = new List<int>();
            }

            for (var vertex = 0; vertex < influences.Length; vertex++)
            {
                var influence = influences[vertex];
                groups[influence.Bone0].Add(vertex);
                if (influence.Bone1 != influence.Bone0)
                {
                    groups[influence.Bone1].Add(vertex);
                }
            }

            return Array.ConvertAll(groups, group => group.ToArray());
        }

        private static Vector3[] HalfPivots(Matrix4x4[] bindposes, Matrix4x4 meshToRoot)
        {
            return Array.ConvertAll(bindposes, bindpose => new VatHalf3(meshToRoot.MultiplyPoint3x4(bindpose.inverse.GetColumn(3))).ToVector3());
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
