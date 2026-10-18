Shader "Switchyard Raiders/XR/Room Menu Art"
{
    Properties
    {
        [PerRendererData] _MainTex ("UI texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ArtRect ("Atlas UV rectangle", Vector) = (0,0,1,1)
        _SheenMode ("0 static, 1 hover, 2 rail", Float) = 0
        _Hover ("Hover", Range(0,1)) = 0
        _StencilComp ("Stencil comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil operation", Float) = 0
        _StencilWriteMask ("Stencil write mask", Float) = 255
        _StencilReadMask ("Stencil read mask", Float) = 255
        _ColorMask ("Color mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Alpha clipping", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            Name "WorldSpaceUI"
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _MainTex_ST;
                float4 _ArtRect;
                half _SheenMode, _Hover;
            CBUFFER_END
            half4 _TextureSampleAdd;
            float4 _ClipRect;
            struct Attributes { float4 positionOS:POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; float2 positionOS:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.positionOS=input.positionOS.xy;
                output.uv=TRANSFORM_TEX(input.uv,_MainTex);
                output.color=input.color*_Color;
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 color=(SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv)+_TextureSampleAdd)*input.color;
                // Sweep the baked highlights; no lights, bloom, particle system, or texture copies.
                float2 localUV = (input.uv - _ArtRect.xy) / max(_ArtRect.zw, float2(.0001,.0001));
                float cycle = fmod(_Time.y, 7.0);
                float railPosition = cycle / 1.35 - .12;
                float buttonPosition = frac(_Time.y * .65) * 1.4 - .2;
                float sweep = _SheenMode > 1.5 ? railPosition : buttonPosition;
                half band = 1 - smoothstep(.015, .10, abs(localUV.x + localUV.y * .08 - sweep));
                half metal = smoothstep(.25, .65, max(color.r, max(color.g, color.b)));
                half strength = _SheenMode > 1.5 ? band * metal * .3 : band * _Hover * .19;
                strength *= step(.5, _SheenMode);
                color.rgb = lerp(color.rgb, half3(1,.94,.76), strength);
                color.rgb *= 1 + _Hover * .06;
                #ifdef UNITY_UI_CLIP_RECT
                    float2 inside=step(_ClipRect.xy,input.positionOS)*step(input.positionOS,_ClipRect.zw);
                    color.a*=inside.x*inside.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                    clip(color.a-0.001h);
                #endif
                return color;
            }
            ENDHLSL
        }
    }
}

