using System;
using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatBoneRig
    {
        public readonly Transform[] Bones;
        public readonly Vector3[] Pivots;
        public readonly VatBoneSkin[] Skins;
        public readonly Vector3[][] BonePoints;

        private readonly Matrix4x4[] _bindposes;
        private readonly Matrix4x4 _rootToMesh;

        public int BoneCount => Bones.Length;

        public VatBoneRig(VatBakeCopy copy)
        {
            var renderer = copy.Renderer;
            var meshToRoot = copy.RendererToRoot;
            Bones = renderer.bones;
            _bindposes = renderer.sharedMesh.bindposes;
            _rootToMesh = meshToRoot.inverse;
            Pivots = HalfPivots(_bindposes, meshToRoot);
            Skins = ReadSkins(copy, meshToRoot);
            BonePoints = GroupByBone(Skins, Bones.Length);
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
            return BonePoints[bone].Length > 0;
        }

        public void ReadSkin(Transform root, Matrix4x4[] skin)
        {
            var worldToRoot = root.worldToLocalMatrix;
            for (var bone = 0; bone < Bones.Length; bone++)
            {
                skin[bone] = worldToRoot * Bones[bone].localToWorldMatrix * _bindposes[bone] * _rootToMesh;
            }
        }

        private VatBoneSkin[] ReadSkins(VatBakeCopy copy, Matrix4x4 meshToRoot)
        {
            var skins = new VatBoneSkin[copy.Extras.Length + 1];
            var space = new VatRootSpace(meshToRoot);
            skins[0] = VatBoneSkin.Skinned(copy.Renderer, VatBoneBinding.BoneMap(copy.Renderer, copy.Renderer), space);
            for (var i = 0; i < copy.Extras.Length; i++)
            {
                skins[i + 1] = copy.Extras[i] is SkinnedMeshRenderer skinned
                    ? VatBoneSkin.Skinned(skinned, VatBoneBinding.BoneMap(copy.Renderer, skinned), space)
                    : Rigid((MeshRenderer)copy.Extras[i], meshToRoot);
            }

            return skins;
        }

        private VatBoneSkin Rigid(MeshRenderer renderer, Matrix4x4 meshToRoot)
        {
            var bone = VatBoneBinding.ParentBone(Bones, renderer.transform);
            var rendererToBone = Bones[bone].worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            return VatBoneSkin.Rigid(renderer, bone, new VatRootSpace(meshToRoot * _bindposes[bone].inverse * rendererToBone));
        }

        private static Vector3[][] GroupByBone(VatBoneSkin[] skins, int boneCount)
        {
            var groups = new List<Vector3>[boneCount];
            for (var bone = 0; bone < boneCount; bone++)
            {
                groups[bone] = new List<Vector3>();
            }

            foreach (var skin in skins)
            {
                for (var vertex = 0; vertex < skin.Influences.Length; vertex++)
                {
                    var influence = skin.Influences[vertex];
                    var point = skin.Rest.Positions[vertex];
                    groups[influence.Bone0].Add(point);
                    if (influence.Bone1 != influence.Bone0)
                    {
                        groups[influence.Bone1].Add(point);
                    }
                }
            }

            return Array.ConvertAll(groups, group => group.ToArray());
        }

        private static Vector3[] HalfPivots(Matrix4x4[] bindposes, Matrix4x4 meshToRoot)
        {
            return Array.ConvertAll(bindposes, bindpose => new VatHalf3(meshToRoot.MultiplyPoint3x4(bindpose.inverse.GetColumn(3))).ToVector3());
        }
    }
}
