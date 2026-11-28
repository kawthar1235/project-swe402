Shader "BuriedOutside/ChestSoftDust"
{
    Properties
    {
        _Color ("Dust Color", Color) = (0.025, 0.02, 0.018, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert(appdata input)
            {
                v2f output;

                output.position =
                    UnityObjectToClipPos(input.vertex);

                output.uv = input.uv;
                output.color = input.color;

                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;

                float alpha = saturate(1.0 - dot(p, p));
                alpha *= alpha;

                float variation =
                    0.8 + 0.2 * sin(p.x * 12.0 + p.y * 7.0);

                alpha *= variation;
                alpha *= input.color.a * _Color.a;

                return fixed4(_Color.rgb, alpha);
            }

            ENDCG
        }
    }
}