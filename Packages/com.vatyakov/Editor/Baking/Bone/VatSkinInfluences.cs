using System;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatSkinInfluences
    {
        public static VatBoneInfluence[] Read(SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh;
            var weights = mesh.boneWeights;
            var rootBone = Math.Max(Array.IndexOf(renderer.bones, renderer.rootBone), 0);
            var influences = new VatBoneInfluence[mesh.vertexCount];
            for (var vertex = 0; vertex < influences.Length; vertex++)
            {
                influences[vertex] = VatBoneInfluence.From(weights.Length == influences.Length ? weights[vertex] : default, rootBone);
            }

            return influences;
        }

        public static VatBoneInfluence[] Remap(VatBoneInfluence[] influences, int[] boneMap)
        {
            return Array.ConvertAll(influences, influence => influence.Remapped(boneMap));
        }

        public static VatBoneInfluence[] Single(int vertexCount, int bone)
        {
            var influences = new VatBoneInfluence[vertexCount];
            Array.Fill(influences, VatBoneInfluence.Single(bone));
            return influences;
        }
    }
}
