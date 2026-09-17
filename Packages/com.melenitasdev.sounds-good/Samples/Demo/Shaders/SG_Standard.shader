// A lit shader for flat colours or a simple base map, opaque or transparent.
//
// Its properties deliberately use Unity's Standard shader names (_MainTex, _Color, _Metallic,
// _Glossiness, _EmissionColor), so switching a Standard material over to this one keeps every value
// it already had, texture included — Unity matches serialized material properties by name.
Shader "Melenitas Dev/SG_Standard"
{
    Properties
    {
        // Defaults to white, so a material with no texture renders as the plain tint colour.
        _MainTex ("Base Map", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,1)

        // Picking Transparent here sets the three render-state properties below and the render
        // queue in one go (see SG_StandardShaderGUI). They stay as real material properties, using
        // Unity's Standard shader names, so a converted material keeps whatever it already had and
        // anyone who prefers to drive them from script still can.
        [HideInInspector] [Enum(Opaque, 0, Transparent, 1)] _Surface ("Surface Type", Float) = 0

        [HideInInspector] [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Float) = 1
        [HideInInspector] [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Float) = 0
        [HideInInspector] [Enum(Off, 0, On, 1)] _ZWrite ("Depth Write", Float) = 1
    }

    // The URP SubShader below is written in between these markers automatically when the Universal
    // Render Pipeline package is present, and stripped back out when it is not. A shader cannot
    // #include URP's ShaderLibrary in a project without URP — Unity compiles every SubShader on
    // import, whatever the active pipeline is — so the URP source is kept out of the compiler in
    // URP/SG_Standard.urp.txt and pasted in only when it can resolve. Do not edit between the markers.
    //#SG_URP_BEGIN
    // ----- Universal Render Pipeline
    // The RenderPipeline tag is what picks this SubShader over the Built-In one at render time.
    // It does not stop Unity compiling it, which is why this block only reaches the shader file at
    // all once the URP package is installed — DemoShaderPipelineSync pastes it in.
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        // Every material property lives in this buffer so the SRP Batcher can batch the shader.
        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _Color;
            half4 _EmissionColor;
            half _Metallic;
            half _Glossiness;
            // Never read by the shader code — declared only so every material property lives in
            // this buffer, which is what keeps the shader SRP Batcher compatible.
            float _Surface;
            float _SrcBlend;
            float _DstBlend;
            float _ZWrite;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            // Set on the pass, not the SubShader, so the depth and shadow passes below keep their
            // own ZWrite On instead of inheriting a transparent material's Depth Write Off.
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            // Unity 6 samples lightmaps from a Texture2DArray by default and only falls back to the
            // legacy Texture2D binding when this keyword is on. Without the variant declared here,
            // URP can't enable it, so the shader samples an array that was never bound and every
            // lightmapped surface comes out unlit.
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogCoord : TEXCOORD2;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 3);
                float2 uv : TEXCOORD4;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normals = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = positions.positionCS;
                OUT.positionWS = positions.positionWS;
                OUT.normalWS = normals.normalWS;
                OUT.fogCoord = ComputeFogFactor(positions.positionCS.z);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);

                OUTPUT_LIGHTMAP_UV(IN.lightmapUV, unity_LightmapST, OUT.lightmapUV);
                OUTPUT_SH(normals.normalWS, OUT.vertexSH);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = baseColor.rgb;
                surfaceData.alpha = baseColor.a;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Glossiness;
                surfaceData.emission = _EmissionColor.rgb;
                surfaceData.occlusion = 1.0;
                surfaceData.normalTS = half3(0, 0, 1);

                float3 normalWS = normalize(IN.normalWS);

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord = IN.fogCoord;
                inputData.bakedGI = SAMPLE_GI(IN.lightmapUV, IN.vertexSH, normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.positionCS = positionCS;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }

            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                return half4(normalize(IN.normalWS), 0.0);
            }
            ENDHLSL
        }
    }
    //#SG_URP_END

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]

        CGPROGRAM
        // keepalpha is what stops the generated code from forcing the output alpha to 1, which
        // would leave a transparent material fully solid no matter how it blends.
        #pragma surface surf Standard fullforwardshadows keepalpha
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        fixed4 _EmissionColor;
        half _Metallic;
        half _Glossiness;

        // Naming it uv_MainTex is what makes the surface shader apply the texture's tiling and
        // offset for us, straight from _MainTex_ST.
        struct Input
        {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 baseColor = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            o.Albedo = baseColor.rgb;
            o.Alpha = baseColor.a;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Emission = _EmissionColor.rgb;
        }
        ENDCG
    }

    FallBack "Diffuse"
    CustomEditor "MelenitasDev.SoundsGood.Editor.SG_StandardShaderGUI"
}
