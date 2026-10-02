using UnityEngine;

namespace VATyakov
{
    public static class VatShaderIds
    {
        public static readonly int PositionTexture = Shader.PropertyToID("_VatPosTex");
        public static readonly int RotationTexture = Shader.PropertyToID("_VatRotTex");
        public static readonly int DriftTexture = Shader.PropertyToID("_VatDriftTex");
        public static readonly int Layout = Shader.PropertyToID("_VatLayout");
        public static readonly int Frame = Shader.PropertyToID("_VatFrame");
    }
}
