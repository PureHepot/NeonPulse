Shader "Custom/EnergyShieldLine"
{
    Properties
    {
        // [Header(Base Settings)]
        // 主颜色 (HDR)，建议设为高亮的蓝色
        [HDR] _Color ("Color Tint", Color) = (0, 0.5, 1, 1)
        // 整体亮度倍增器
        _Brightness ("Brightness Boost", Range(1, 10)) = 2.0
        
        // [Header(Energy Effect)]
        // 噪声纹理 (必须是无缝的/可平铺的)，用于生成粒子图案
        _NoiseTex ("Noise Texture (Alpha is Particles)", 2D) = "white" {}
        // 滚动速度 (X轴方向沿线条流动)
        _ScrollSpeed ("Scroll Speed", Float) = 1.0
        // 能量密度 (X轴拉伸噪声纹理)
        _EnergyTiling ("Energy Tiling (Density)", Float) = 5.0
        
        // [Header(Edge Fade)]
        // 控制线条宽度方向的柔化程度，越大边缘越硬
        _EdgePower ("Edge Hardness", Range(1, 10)) = 3.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
        }

        // 核心设置：加色混合模式 (Additive Blending)
        // 这种模式会让颜色叠加变亮，非常适合发光的能量效果，黑色背景下效果最好
        Blend SrcAlpha One
        // 关闭深度写入，防止透明物体遮挡问题
        ZWrite Off
        // 关闭背面剔除，保证线条翻转也能看到
        Cull Off
        // 关闭光照计算
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR; // 接收来自 LineRenderer 组件设置的定点颜色
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            half4 _Color;
            float _ScrollSpeed;
            float _EnergyTiling;
            float _Brightness;
            float _EdgePower;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                // 将 LineRenderer 组件上的颜色渐变与材质球颜色相乘
                o.color = v.color * _Color; 
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 scrollUV = i.uv;
                scrollUV.x = scrollUV.x * _EnergyTiling - _Time.y * _ScrollSpeed;
                
                // 采样噪声纹理，这里只取 Alpha 通道作为能量强度
                fixed noiseValue = tex2D(_NoiseTex, scrollUV).a;
                
                float edgeFade = 1.0 - abs(i.uv.y * 2.0 - 1.0);
                // 使用幂函数控制边缘硬度
                edgeFade = pow(edgeFade, _EdgePower);

                fixed4 finalColor = i.color;
                
                // 将噪声应用到透明度上，形成粒子感
                finalColor.a *= noiseValue;
                // 应用边缘柔化
                finalColor.a *= edgeFade;
                
                // 核心：因为是 Additive 混合，我们将 RGB 乘以 Alpha。
                // 这样 Alpha 越低的地方就越黑（不发光），越高的地方越亮。
                finalColor.rgb *= finalColor.a;
                
                // 最后乘以亮度增强系数
                finalColor.rgb *= _Brightness;

                return finalColor;
            }
            ENDCG
        }
    }
}
