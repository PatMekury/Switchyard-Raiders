/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

// Adapted for URP stereo rendering; retain the Meta cursor appearance and property names.
Shader "Switchyard Raiders/XR/Poke Cursor"
{
    Properties
    {
        _OutlineWidth("OutlineWidth", Range( 0 , 0.4)) = 0.03
        _Color("Inner Color", Color) = (0,0,0,1)
        _OutlineColor("OutlineColor", Color) = (0,0.4410214,1,1)
        _Alpha("Alpha", Range( 0 , 1)) = 0
        _RadialGradientIntensity("RadialGradientIntensity", Range( 0 , 1)) = 0
        _RadialGradientScale("RadialGradientScale", Range( 0 , 1)) = 1
        _RadialGradientBackgroundOpacity("RadialGradientBackgroundOpacity", Range( 0 , 1)) = 0.1
        _RadialGradientOpacity("RadialGradientOpacity", Range( 0 , 1)) = 0.8550259
        [Toggle] _CLIP ("Use clip", Integer) = 1
        [HideInInspector] _texcoord( "", 2D ) = "white" {}
        [HideInInspector] __dirty( "", Int ) = 1
    }

    SubShader
    {
        Tags{ "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent"  "Queue" = "Transparent+10" "IgnoreProjector" = "True"  }
        Cull Off
        ZTest LEqual
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Offset -5, -5
        Pass
        {
            HLSLPROGRAM
            #pragma multi_compile_instancing
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _CLIP_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)

            uniform float _RadialGradientScale;
            uniform float _RadialGradientOpacity;
            uniform float _RadialGradientIntensity;
            uniform float _RadialGradientBackgroundOpacity;
            uniform float _OutlineWidth;
            uniform float4 _Color;
            uniform float4 _OutlineColor;
            uniform float _Alpha;
            CBUFFER_END

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv_texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 uv_texcoord : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o = (v2f)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv_texcoord.xy = v.uv_texcoord;
                o.uv_texcoord.z = 0.16 + _RadialGradientScale * 0.29;
                o.uv_texcoord.w = 5.0 + _RadialGradientIntensity * -3.5;
                return o;
            }

            static const float INLINE_OUTLINE_FALLOFF = 70.0;
            static const float OUTER_OUTLINE_FALLOFF = 20.0;
            static const float TRANSITION_THRESHOLD = 0.1;
            static const float2 CENTER =  float2(0.5, 0.5);

            half4 frag(v2f i) : SV_Target
            {
               UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
               float distToCenter = distance(i.uv_texcoord.xy, CENTER);
               float gradientMaxRadius = i.uv_texcoord.z;
               float gradientFallOff = i.uv_texcoord.w;

               half radialGradientNormalized = 1.0 - (distToCenter * rcp(gradientMaxRadius));
               half mainRadialFalloff = saturate(rcp(exp(radialGradientNormalized * gradientFallOff)));
               half inlineOutlineFalloff = saturate(rcp(exp(radialGradientNormalized * INLINE_OUTLINE_FALLOFF)));
               half outlineShadowStrength = 1.0 - inlineOutlineFalloff;

               half radialGradientComposite = saturate(
                   _RadialGradientOpacity * (mainRadialFalloff - inlineOutlineFalloff)
                   + outlineShadowStrength * _RadialGradientBackgroundOpacity
               );

               half outlineRingNormalized = 1.0 - (distToCenter * rcp(gradientMaxRadius + _OutlineWidth));
               half outlineRingFalloff = 1.0 - saturate(rcp(exp(outlineRingNormalized * OUTER_OUTLINE_FALLOFF)));
               half4 outlineColorContribution = radialGradientComposite
                   + (outlineRingFalloff - outlineShadowStrength) * _OutlineColor;

               half transitionBlend = saturate((_RadialGradientScale - TRANSITION_THRESHOLD) * 1000);
               half4 pointerColor = lerp(_OutlineColor, outlineColorContribution, transitionBlend);

               half3 color = pointerColor.rgb * _Color.rgb;
               half alpha = pointerColor.a * _Alpha;

               #if _CLIP_ON
                   clip(alpha - 0.01);
               #endif
           
               return half4(color.rgb, alpha);
           }
           ENDHLSL
        }
    }

}
