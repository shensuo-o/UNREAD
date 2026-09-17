Shader "Melenitas Dev/SG_InteractableOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0.72, 0.16, 0.9)
        _OutlineWidth ("Outline Width", Range(0.001, 0.12)) = 0.035
        _PulseStrength ("Pulse Strength", Range(0, 0.5)) = 0.08
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 2.5
    }

    // The URP SubShader below is written in between these markers automatically when the Universal
    // Render Pipeline package is present, and stripped back out when it is not. A shader cannot
    // #include URP's ShaderLibrary in a project without URP — Unity compiles every SubShader on
    // import, whatever the active pipeline is — so the URP source is kept out of the compiler in
    // URP/SG_InteractableOutline.urp.txt and pasted in only when it can resolve. Do not edit between the markers.
    //#SG_URP_BEGIN
    // ----- Universal Render Pipeline
    // The RenderPipeline tag is what picks this SubShader over the Built-In one at render time.
    // It does not stop Unity compiling it, which is why this block only reaches the shader file at
    // all once the URP package is installed — DemoShaderPipelineSync pastes it in.
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        LOD 100
        Cull Front
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            // Every material property lives in this buffer so the SRP Batcher can batch the shader.
            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
                float _PulseStrength;
                float _PulseSpeed;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseStrength;
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                positionWS += normalize(normalWS) * _OutlineWidth * pulse;

                OUT.positionCS = TransformWorldToHClip(positionWS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
    //#SG_URP_END

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        LOD 100
        Cull Front
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _PulseStrength;
            float _PulseSpeed;

            v2f vert(appdata_t v)
            {
                v2f o;

                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseStrength;
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                float3 worldPosition = mul(unity_ObjectToWorld, v.vertex).xyz;
                worldPosition += normalize(worldNormal) * _OutlineWidth * pulse;

                o.vertex = UnityWorldToClipPos(worldPosition);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }

    FallBack Off
}
