Shader "Melenitas Dev/SG_ScrollOffset"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Speed ("Scroll Speed", Range(-0.5, 0.5)) = 0.1
    }

    // The URP SubShader below is written in between these markers automatically when the Universal
    // Render Pipeline package is present, and stripped back out when it is not. A shader cannot
    // #include URP's ShaderLibrary in a project without URP — Unity compiles every SubShader on
    // import, whatever the active pipeline is — so the URP source is kept out of the compiler in
    // URP/SG_ScrollOffset.urp.txt and pasted in only when it can resolve. Do not edit between the markers.
    //#SG_URP_BEGIN
    // ----- Universal Render Pipeline
    // The RenderPipeline tag is what picks this SubShader over the Built-In one at render time.
    // It does not stop Unity compiling it, which is why this block only reaches the shader file at
    // all once the URP package is installed — DemoShaderPipelineSync pastes it in.
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Opaque"
        }
        LOD 100

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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // Every material property lives in this buffer so the SRP Batcher can batch the shader.
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Speed;
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
                float2 tiling = _MainTex_ST.xy;

                float2 uv = IN.uv * tiling;

                uv.x += _Time.y * _Speed;
                uv.x = frac(uv.x);

                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
            }
            ENDHLSL
        }
    }
    //#SG_URP_END

    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "RenderType"="Opaque"
        }
        LOD 100

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

            sampler2D _MainTex;
            float _Speed;
            float2 _MainTex_ST;
            float4 _Color;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 tiling = _MainTex_ST.xy;

                float2 uv = i.uv * tiling;

                uv.x += _Time.y * _Speed;
                uv.x = frac(uv.x);

                fixed4 col = tex2D(_MainTex, uv);

                return col;
            }
            ENDCG
        }
    }
}
