Shader "RealmShards/SpriteTintRecolor"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0.72, 0.45, 0.95, 1)
        _RecolorStrength ("Recolor Strength", Range(0, 1)) = 0.85
        _PurpleCenter ("Purple Center", Color) = (0.45, 0.25, 0.7, 1)
        _PurpleTolerance ("Purple Tolerance", Range(0, 1)) = 0.42
        _GoldReject ("Gold Reject Threshold", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _Color;
            float _RecolorStrength;
            float4 _PurpleCenter;
            float _PurpleTolerance;
            float _GoldReject;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float3 rgb = tex.rgb;

                // Reject gold / warm metals (high R+G, low B relative).
                float warm = saturate((rgb.r + rgb.g) * 0.5 - rgb.b);
                float goldMask = smoothstep(_GoldReject, _GoldReject + 0.25, warm);

                // Reject near-black clothing.
                float luma = dot(rgb, float3(0.299, 0.587, 0.114));
                float darkReject = smoothstep(0.04, 0.18, luma);

                float dist = distance(normalize(rgb + 1e-5), normalize(_PurpleCenter.rgb + 1e-5));
                float purpleMask = saturate(1.0 - dist / max(0.001, _PurpleTolerance));
                purpleMask *= darkReject;
                purpleMask *= (1.0 - goldMask);

                float3 recolored = luma * _Color.rgb;
                rgb = lerp(rgb, recolored, purpleMask * _RecolorStrength);

                float4 result = float4(rgb, tex.a) * input.color;
                result.rgb *= result.a;
                return result;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
