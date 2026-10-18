Shader "Switchyard Raiders/UI/Enamel"
{
    Properties
    {
        [PerRendererData] _MainTex ("Color and coverage", 2D) = "white" {}
        _NormalMap ("Linear tangent normal XYZ", 2D) = "bump" {}
        _SurfaceMap ("Metal / Rough / AO / Enamel", 2D) = "white" {}
        _Hover ("Hover", Range(0,1)) = 0
        _PressTime ("Press time", Float) = -100
        _Flow ("Enamel motion", Range(0,1)) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_SurfaceMap); SAMPLER(sampler_SurfaceMap);
            CBUFFER_START(UnityPerMaterial)
                float _Hover, _PressTime, _Flow;
            CBUFFER_END
            struct A { float4 vertex:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A i)
            {
                V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = TransformObjectToHClip(i.vertex.xyz); o.uv = i.uv; o.color = i.color; return o;
            }
            half4 Frag(V i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half4 c = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv) * i.color;
                half3 n = normalize(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,i.uv).xyz * 2 - 1);
                half4 surface = SAMPLE_TEXTURE2D(_SurfaceMap,sampler_SurfaceMap,i.uv);
                half light = saturate(dot(n,normalize(half3(-.35,.55,1))));
                half spec = pow(light, lerp(48,8,surface.g)) * lerp(.035,.13,surface.r);
                half drift = sin(i.uv.y * 19 + sin(i.uv.x * 8 + _Time.y * .23) * 2 - _Time.y * .35) * .025;
                float age = _Time.y - _PressTime;
                half ripple = (1 - smoothstep(.01,.07,abs(length((i.uv-.5)*float2(1,.45))-age*.9))) * saturate(1-age*2);
                c.rgb = c.rgb * (.94 + .06*light) * surface.b + half3(1,.83,.54)*spec;
                c.rgb += surface.a * _Flow * (drift + ripple*.075) + _Hover * half3(.055,.06,.04);
                c.rgb *= c.a;
                return c;
            }
            ENDHLSL
        }
    }
}
