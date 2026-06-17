Shader "Hidden/NightVision"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Brightness ("Brightness", Float) = 9.0
        _Lift ("Lift", Float) = 0.12
        _Noise ("Noise", Float) = 0.025
    }

    SubShader
    {
        ZTest Always Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            float _Brightness;
            float _Lift;
            float _Noise;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float luma = dot(c.rgb, float3(0.299, 0.587, 0.114));

                float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv));
                float profundidad = 1.0 - smoothstep(0.5, 45.0, depth);

                float señal = max(luma, profundidad * luma * 2.5);
                if (señal < 0.008 && profundidad < 0.05)
                    return fixed4(0.02, 0.02, 0.02, 1);

                señal = pow(max(señal, 0.001), 0.38);
                señal = saturate(señal * _Brightness + _Lift * señal);

                float n = (hash(i.uv * (_Time.y * 45.0 + 1.0)) - 0.5) * _Noise;
                señal = saturate(señal + n * señal);

                float2 centered = i.uv - 0.5;
                float vignette = 1.0 - dot(centered, centered) * 0.75;
                señal *= saturate(vignette);

                return fixed4(señal * 0.2, señal * 1.12, señal * 0.18, 1.0);
            }
            ENDCG
        }
    }
}
