using UnityEngine;

namespace VATyakov
{
    public static class VatShaderIds
    {
        public static readonly int PositionTexture = Shader.PropertyToID("_VatPosTex");
        public static readonly int RotationTexture = Shader.PropertyToID("_VatRotTex");
        public static readonly int Drift = Shader.PropertyToID("_VatDrift");
        public static readonly int Layout = Shader.PropertyToID("_VatLayout");
        public static readonly int Frame = Shader.PropertyToID("_VatFrame");
        public static readonly int FrameB = Shader.PropertyToID("_VatFrameB");
    }
}
