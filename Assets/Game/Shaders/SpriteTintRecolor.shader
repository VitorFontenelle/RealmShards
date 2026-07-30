Shader "RealmShards/SpriteTintRecolor"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0.72, 0.45, 0.95, 1)
        _RecolorStrength ("Recolor Strength", Range(0, 1)) = 0.65
        _PurpleCenter ("Purple Center", Color) = (0.45, 0.25, 0.7, 1)
        _PurpleTolerance ("Purple Tolerance", Range(0, 1)) = 0.45
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
                output.color = input.color * _Color;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float3 rgb = tex.rgb;

                // Approximate purple robe mask via hue distance in RGB space.
                float dist = distance(normalize(rgb + 1e-5), normalize(_PurpleCenter.rgb + 1e-5));
                float mask = saturate(1.0 - dist / max(0.001, _PurpleTolerance));
                mask *= smoothstep(0.05, 0.2, max(rgb.r, max(rgb.g, rgb.b)));

                float luminance = dot(rgb, float3(0.299, 0.587, 0.114));
                float3 recolored = luminance * _Color.rgb;
                rgb = lerp(rgb, recolored, mask * _RecolorStrength);

                float4 result = float4(rgb, tex.a) * input.color;
                result.rgb *= result.a;
                return result;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
