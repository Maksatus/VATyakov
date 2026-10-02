Shader "VATyakov/Dev/RotDecodeTest"
{
    Properties
    {
        _VatRotTex ("Test texture", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }

        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.vatyakov/Shaders/VatCore.hlsl"

            TEXTURE2D(_VatRotTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = float4(input.uv * 2.0 - 1.0, 0.5, 1.0);
                output.uv = input.uv;
                return output;
            }

            uint4 ExpectedBytes(uint x)
            {
                return uint4(x, 255u - x, (x * 37u + 11u) & 255u, (x * 101u + 7u) & 255u);
            }

            uint4 ExpectedFields(uint i)
            {
                return uint4(i, (i * 389u + 17u) & 1023u, 1023u - i, i & 3u);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                uint2 texel = (uint2)(input.uv * float2(256.0, 5.0));
                half4 value = LOAD_TEXTURE2D_LOD(_VatRotTex, texel, 0);
                uint4 bytes = VatRotationBytes(value);
                bool ok = texel.y == 0u
                    ? all(bytes == ExpectedBytes(texel.x))
                    : all(VatRotationFields(bytes) == ExpectedFields((texel.y - 1u) * 256u + texel.x));
                return ok ? half4(0.0, 1.0, 0.0, 1.0) : half4(1.0, 0.0, 0.0, 1.0);
            }
            ENDHLSL
        }
    }
}
