Shader "Melenitas Dev/SG_MusicZoneVisualizer"
{
    Properties
    {
        _FillColor ("Fill Color", Color) = (0.05, 0.65, 1.0, 0.12)
        _EdgeColor ("Edge Color", Color) = (0.05, 0.95, 1.0, 0.85)
        _EdgeWidth ("Edge Width", Range(0.001, 0.25)) = 0.045
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.1)) = 0.01
        _EdgeGlow ("Edge Glow", Range(0, 3)) = 1
        _PulseStrength ("Pulse Strength", Range(0, 0.5)) = 0.08
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 2
    }

    // The URP SubShader below is written in between these markers automatically when the Universal
    // Render Pipeline package is present, and stripped back out when it is not. A shader cannot
    // #include URP's ShaderLibrary in a project without URP — Unity compiles every SubShader on
    // import, whatever the active pipeline is — so the URP source is kept out of the compiler in
    // URP/SG_MusicZoneVisualizer.urp.txt and pasted in only when it can resolve. Do not edit between the markers.
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
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            // Every material property lives in this buffer so the SRP Batcher can batch the shader.
            CBUFFER_START(UnityPerMaterial)
                half4 _FillColor;
                half4 _EdgeColor;
                float _EdgeWidth;
                float _EdgeSoftness;
                float _EdgeGlow;
                float _PulseStrength;
                float _PulseSpeed;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 faceEdgeDistance = min(IN.uv, 1.0 - IN.uv);
                float edgeDistance = min(faceEdgeDistance.x, faceEdgeDistance.y);
                float edgeMask = 1.0 - smoothstep(_EdgeWidth, _EdgeWidth + _EdgeSoftness, edgeDistance);

                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseStrength;

                half4 fill = _FillColor;
                fill.rgb *= pulse;

                half4 edge = _EdgeColor;
                edge.rgb *= 1.0 + _EdgeGlow;

                half4 color = lerp(fill, edge, edgeMask);
                color.a = saturate(fill.a + edge.a * edgeMask);
                return color;
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
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            fixed4 _FillColor;
            fixed4 _EdgeColor;
            float _EdgeWidth;
            float _EdgeSoftness;
            float _EdgeGlow;
            float _PulseStrength;
            float _PulseSpeed;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 faceEdgeDistance = min(i.uv, 1.0 - i.uv);
                float edgeDistance = min(faceEdgeDistance.x, faceEdgeDistance.y);
                float edgeMask = 1.0 - smoothstep(_EdgeWidth, _EdgeWidth + _EdgeSoftness, edgeDistance);

                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseStrength;

                fixed4 fill = _FillColor;
                fill.rgb *= pulse;

                fixed4 edge = _EdgeColor;
                edge.rgb *= 1.0 + _EdgeGlow;

                fixed4 color = lerp(fill, edge, edgeMask);
                color.a = saturate(fill.a + edge.a * edgeMask);
                return color;
            }
            ENDCG
        }
    }
}
