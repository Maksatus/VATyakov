using System;
using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatBoneBinding
    {
        public const float BindposeTolerance = 1e-4f;

        private const int MatrixElements = 16;

        public static string Problem(SkinnedMeshRenderer rig, Renderer renderer)
        {
            if (renderer == null)
            {
                return "An Extra Renderer is not set: assign a renderer or remove the element.";
            }

            if (renderer == rig)
            {
                return $"'{renderer.name}' is the Skinned Mesh Renderer of the profile: remove it from Extra Renderers.";
            }

            if (renderer.transform.root != rig.transform.root)
            {
                return $"'{renderer.name}' is not in the hierarchy of '{rig.name}': Extra Renderers come from the same prefab or model.";
            }

            return renderer switch
            {
                SkinnedMeshRenderer skinned => SkinnedProblem(rig, skinned),
                MeshRenderer rigid => RigidProblem(rig, rigid),
                _ => $"'{renderer.name}' is neither a Skinned Mesh Renderer nor a Mesh Renderer.",
            };
        }

        public static int[] BoneMap(SkinnedMeshRenderer rig, SkinnedMeshRenderer skinned)
        {
            var rigBones = rig.bones;
            return Array.ConvertAll(skinned.bones, bone => Array.IndexOf(rigBones, bone));
        }

        public static int ParentBone(Transform[] bones, Transform transform)
        {
            for (var parent = transform; parent != null; parent = parent.parent)
            {
                var bone = Array.IndexOf(bones, parent);
                if (bone >= 0)
                {
                    return bone;
                }
            }

            return -1;
        }

        public static Mesh RigidMesh(MeshRenderer renderer)
        {
            var filter = renderer.GetComponent<MeshFilter>();
            return filter != null ? filter.sharedMesh : null;
        }

        private static string SkinnedProblem(SkinnedMeshRenderer rig, SkinnedMeshRenderer skinned)
        {
            if (skinned.sharedMesh == null)
            {
                return $"'{skinned.name}' has no mesh.";
            }

            var problem = VatBoneRig.Problem(skinned);
            if (problem != null)
            {
                return problem;
            }

            var boneMap = BoneMap(rig, skinned);
            var rigBindposes = rig.sharedMesh.bindposes;
            var bindposes = skinned.sharedMesh.bindposes;
            foreach (var bone in UsedBones(skinned))
            {
                var name = skinned.bones[bone].name;
                if (boneMap[bone] < 0)
                {
                    return $"Bone '{name}' of '{skinned.name}' is not a bone of '{rig.name}'.";
                }

                if (!AreEqual(bindposes[bone], rigBindposes[boneMap[bone]]))
                {
                    return $"Bone '{name}' of '{skinned.name}' has another bindpose than in '{rig.name}': skin both meshes in one bind pose.";
                }
            }

            return null;
        }

        private static string RigidProblem(SkinnedMeshRenderer rig, MeshRenderer rigid)
        {
            if (RigidMesh(rigid) == null)
            {
                return $"'{rigid.name}' has no mesh in its Mesh Filter.";
            }

            return ParentBone(rig.bones, rigid.transform) < 0
                ? $"'{rigid.name}' is not under a bone of '{rig.name}': put the Mesh Renderer under the bone it follows."
                : null;
        }

        private static HashSet<int> UsedBones(SkinnedMeshRenderer skinned)
        {
            var bones = new HashSet<int>();
            foreach (var influence in VatSkinInfluences.Read(skinned))
            {
                bones.Add(influence.Bone0);
                bones.Add(influence.Bone1);
            }

            return bones;
        }

        private static bool AreEqual(Matrix4x4 a, Matrix4x4 b)
        {
            for (var i = 0; i < MatrixElements; i++)
            {
                if (Mathf.Abs(a[i] - b[i]) > BindposeTolerance)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
