Shader "Custom/SpriteVerticalFade"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [Header(Gradient Colors)]
        _BottomColor ("Bottom Color (Mau Duoi)", Color) = (0.85, 0.12, 0.12, 1.0)
        _TopColor ("Top Color (Mau Tren)", Color) = (0.85, 0.12, 0.12, 0.0)
        
        [Header(Fade Settings)]
        _FadeStart ("Fade Start (0 = Day, 1 = Dinh)", Range(0.0, 1.0)) = 0.0
        _FadeEnd ("Fade End (0 = Day, 1 = Dinh)", Range(0.0, 1.0)) = 1.0
        _FadePower ("Fade Curve / Smoothness", Range(0.1, 5.0)) = 1.0
        [MaterialToggle] _InvertFade ("Dao nguoc huong Fade (Invert)", Float) = 0

        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Universal2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                half4 color         : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                half4 color         : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _BottomColor;
                half4 _TopColor;
                float _FadeStart;
                float _FadeEnd;
                float _FadePower;
                float _InvertFade;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                float t = input.uv.y;
                if (_InvertFade > 0.5)
                {
                    t = 1.0 - t;
                }

                float range = max(_FadeEnd - _FadeStart, 0.0001);
                float factor = saturate((t - _FadeStart) / range);
                factor = pow(factor, _FadePower);

                half4 gradColor = lerp(_BottomColor, _TopColor, factor);

                return texColor * input.color * gradColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "SRPDefaultUnlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                half4 color         : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                half4 color         : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _BottomColor;
                half4 _TopColor;
                float _FadeStart;
                float _FadeEnd;
                float _FadePower;
                float _InvertFade;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                float t = input.uv.y;
                if (_InvertFade > 0.5)
                {
                    t = 1.0 - t;
                }

                float range = max(_FadeEnd - _FadeStart, 0.0001);
                float factor = saturate((t - _FadeStart) / range);
                factor = pow(factor, _FadePower);

                half4 gradColor = lerp(_BottomColor, _TopColor, factor);

                return texColor * input.color * gradColor;
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
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
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            fixed4 _BottomColor;
            fixed4 _TopColor;
            float _FadeStart;
            float _FadeEnd;
            float _FadePower;
            float _InvertFade;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord);
                float t = IN.texcoord.y;
                if (_InvertFade > 0.5)
                {
                    t = 1.0 - t;
                }

                float range = max(_FadeEnd - _FadeStart, 0.0001);
                float factor = saturate((t - _FadeStart) / range);
                factor = pow(factor, _FadePower);

                fixed4 gradColor = lerp(_BottomColor, _TopColor, factor);
                return c * IN.color * gradColor;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
