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
    }
}
