Shader "Switchyard Raiders/Startup/Railway Title"
{
    Properties
    {
        _BaseMap ("Title RGBA", 2D) = "white" {}
        _Opacity ("Opacity", Range(0,1)) = 0
        _Sweep ("Light sweep", Float) = -1
        _SweepColor ("Warm reflected light", Color) = (0.38,0.27,0.12,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "UnlitTitle"
            Tags { "LightMode"="UniversalForward" }
            // Premultiplied output preserves passthrough coverage in the eye-buffer alpha.
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _SweepColor;
                float _Opacity;
                float _Sweep;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float band = 1 - smoothstep(0.015, 0.10, abs(input.uv.x + input.uv.y * 0.12 - _Sweep));
                color.rgb += band * _SweepColor.rgb;
                color.a *= saturate(_Opacity);
                color.rgb *= color.a;
                return color;
            }
            ENDHLSL
        }
    }
}
