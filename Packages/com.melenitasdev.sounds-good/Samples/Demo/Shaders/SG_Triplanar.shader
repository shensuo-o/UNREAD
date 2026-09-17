Shader "Melenitas Dev/SG_Triplanar"
{
    Properties
    {
        _TopTex ("Top (Y) Texture", 2D) = "white" {}
        _SideXTex ("Side X Texture", 2D) = "white" {}
        _SideZTex ("Side Z Texture", 2D) = "white" {}

        _TintColor ("Tint Color", Color) = (1,1,1,1)
        _Scale ("Texture Scale", Float) = 1.0

        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
    }

    // The URP SubShader below is written in between these markers automatically when the Universal
    // Render Pipeline package is present, and stripped back out when it is not. A shader cannot
    // #include URP's ShaderLibrary in a project without URP — Unity compiles every SubShader on
    // import, whatever the active pipeline is — so the URP source is kept out of the compiler in
    // URP/SG_Triplanar.urp.txt and pasted in only when it can resolve. Do not edit between the markers.
    //#SG_URP_BEGIN
    // ----- Universal Render Pipeline
    // The RenderPipeline tag is what picks this SubShader over the Built-In one at render time.
    // It does not stop Unity compiling it, which is why this block only reaches the shader file at
    // all once the URP package is installed — DemoShaderPipelineSync pastes it in.
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_TopTex);   SAMPLER(sampler_TopTex);
        TEXTURE2D(_SideXTex); SAMPLER(sampler_SideXTex);
        TEXTURE2D(_SideZTex); SAMPLER(sampler_SideZTex);

        // Every material property lives in this buffer so the SRP Batcher can batch the shader.
        CBUFFER_START(UnityPerMaterial)
            float4 _TopTex_ST;
            float4 _SideXTex_ST;
            float4 _SideZTex_ST;
            half4 _TintColor;
            float _Scale;
            half _Metallic;
            half _Glossiness;
        CBUFFER_END

        /// Same triplanar blend as the Built-In surface shader below: world-space projection on the
        /// three axis planes, weighted by the world normal.
        half4 SampleTriplanar (float3 positionWS, float3 normalWS)
        {
            float3 an = abs(normalize(normalWS));

            // Pesos de mezcla normalizados
            float sum = an.x + an.y + an.z + 1e-5;
            an /= sum;

            // UVs triplanares (espacio mundo, sin tiling por objeto)
            float2 uvX = positionWS.zy * _Scale; // Proyección en plano YZ (para caras mirando +/-X)
            float2 uvY = positionWS.xz * _Scale; // Proyección en plano XZ (para caras mirando +/-Y)
            float2 uvZ = positionWS.xy * _Scale; // Proyección en plano XY (para caras mirando +/-Z)

            half4 colX = SAMPLE_TEXTURE2D(_SideXTex, sampler_SideXTex, uvX);
            half4 colY = SAMPLE_TEXTURE2D(_TopTex,   sampler_TopTex,   uvY);
            half4 colZ = SAMPLE_TEXTURE2D(_SideZTex, sampler_SideZTex, uvZ);

            // Mezcla triplanar
            half4 c = colX * an.x + colY * an.y + colZ * an.z;

            // Tint
            c.rgb *= _TintColor.rgb;
            c.a   *= _TintColor.a;

            return c;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

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
                float2 lightmapUV : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogCoord : TEXCOORD2;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 3);
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

                OUTPUT_LIGHTMAP_UV(IN.lightmapUV, unity_LightmapST, OUT.lightmapUV);
                OUTPUT_SH(normals.normalWS, OUT.vertexSH);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 c = SampleTriplanar(IN.positionWS, IN.normalWS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = c.rgb;
                surfaceData.alpha = c.a;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Glossiness;
                surfaceData.occlusion = 1.0;
                surfaceData.normalTS = half3(0, 0, 1);

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalize(IN.normalWS);
                inputData.viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord = IN.fogCoord;
                inputData.bakedGI = SAMPLE_GI(IN.lightmapUV, IN.vertexSH, inputData.normalWS);
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
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _TopTex;
        sampler2D _SideXTex;
        sampler2D _SideZTex;

        fixed4 _TintColor;
        float _Scale;
        half _Metallic;
        half _Glossiness;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Normal en mundo normalizada
            float3 n = normalize(IN.worldNormal);
            float3 an = abs(n);

            // Pesos de mezcla normalizados
            float sum = an.x + an.y + an.z + 1e-5;
            an /= sum;

            // UVs triplanares (espacio mundo, sin tiling por objeto)
            float2 uvX = IN.worldPos.zy * _Scale; // Proyección en plano YZ (para caras mirando +/-X)
            float2 uvY = IN.worldPos.xz * _Scale; // Proyección en plano XZ (para caras mirando +/-Y)
            float2 uvZ = IN.worldPos.xy * _Scale; // Proyección en plano XY (para caras mirando +/-Z)

            fixed4 colX = tex2D(_SideXTex, uvX);
            fixed4 colY = tex2D(_TopTex,   uvY);
            fixed4 colZ = tex2D(_SideZTex, uvZ);

            // Mezcla triplanar
            fixed4 c = colX * an.x + colY * an.y + colZ * an.z;

            // Tint
            c.rgb *= _TintColor.rgb;
            c.a   *= _TintColor.a;

            o.Albedo = c.rgb;
            o.Alpha  = c.a;

            o.Metallic   = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
