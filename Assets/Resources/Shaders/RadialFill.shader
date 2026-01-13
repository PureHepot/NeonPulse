Shader "Custom/SpriteRadialFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        // 核心参数：填充角度 (0到1，1代表180度全开)
        _FillAmount ("Fill Amount", Range(0, 1)) = 1 
        // 你的贴图是水平的还是垂直的？如果贴图是竖直的圆环，这里可能需要调整
        _StartAngleOffset ("Angle Offset", Float) = 90 
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

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
                float2 uvCenter : TEXCOORD1; // 用来存UV中心点
            };

            fixed4 _Color;
            float _FillAmount;
            float _StartAngleOffset;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                // 假设 Sprite 的中心就是 UV 的 (0.5, 0.5)
                OUT.uvCenter = IN.texcoord - float2(0.5, 0.5);
                return OUT;
            }

            sampler2D _MainTex;
            sampler2D _AlphaTex;

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;

                // --- 核心计算逻辑 ---
                // 计算当前像素的角度 (Atan2 返回弧度)
                float angle = atan2(IN.uvCenter.y, IN.uvCenter.x) * 57.2958; // 转为角度

                // 修正角度偏移 (让0度对准你的贴图正中间)
                // 如果你的贴图是朝右的，Offset通常设为0或90，需根据实际情况微调
                float currentAngle = abs(angle); 
                
                // 计算阈值：_FillAmount 为 1 时，允许 90度（即上下各90度=180度）
                // 这里的 180 是因为 Atan2 的范围是 -180 到 180
                // 我们要做的是从 X轴(0度) 向两边展开
                float maxVisibleAngle = _FillAmount * 94.0; // 半边允许的角度

                // 裁剪：如果当前像素的角度超过了允许范围，把透明度设为0
                // 注意：这里简单的用 abs(angle) < maxVisibleAngle 即可实现以X轴为中心的对称展开
                if (abs(angle) > maxVisibleAngle)
                {
                    c.a = 0;
                }

                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
