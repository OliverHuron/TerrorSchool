Shader "NightVision/LCD"
{
    Properties
    {
        _MainTex ("Camara", 2D) = "black" {}
        _Brightness ("Brillo", Float) = 9.0
        _Lift ("Lift", Float) = 0.15
    }

    SubShader
    {
        Tags { "Queue" = "Overlay" "RenderType" = "Opaque" }
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Brightness;
            float _Lift;

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
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float luma = dot(c.rgb, float3(0.299, 0.587, 0.114));

                if (luma < 0.008)
                    return fixed4(0.01, 0.025, 0.01, 1.0);

                luma = pow(luma, 0.42);
                luma = saturate(luma * _Brightness + _Lift * luma);

                return fixed4(luma * 0.18, luma * 1.1, luma * 0.16, 1.0);
            }
            ENDCG
        }
    }
}
