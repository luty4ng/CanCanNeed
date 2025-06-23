Shader "CosmosTech/StarfieldSkybox_Old"
{
    Properties
    {
        // 与上述着色器相同的属性
        _StarDensity ("Star Density", Range(0.01, 100)) = 10
        _StarSize ("Star Size", Range(0.001, 0.1)) = 0.01
        _StarColor ("Star Color", Color) = (1,1,1,1)
        _BackgroundColor ("Background Color", Color) = (0,0,0,1)
        _StarTwinkleSpeed ("Twinkle Speed", Range(0, 10)) = 1
        _StarTwinkleAmount ("Twinkle Amount", Range(0, 1)) = 0.2
    }
    
    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        
        Pass
        {
            Name "StarfieldSkybox"
            Cull Off
            ZWrite Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 viewDir : TEXCOORD0;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float _StarDensity;
                float _StarSize;
                float4 _StarColor;
                float4 _BackgroundColor;
                float _StarTwinkleSpeed;
                float _StarTwinkleAmount;
            CBUFFER_END
            
            // 随机函数和噪声函数与前面相同
            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }
            
            float noise(float2 st)
            {
                float2 i = floor(st);
                float2 f = frac(st);
                
                float a = random(i);
                float b = random(i + float2(1.0, 0.0));
                float c = random(i + float2(0.0, 1.0));
                float d = random(i + float2(1.0, 1.0));
                
                float2 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(a, b, u.x) +
                       (c - a) * u.y * (1.0 - u.x) +
                       (d - b) * u.x * u.y;
            }
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.viewDir = normalize(mul((float3x3)unity_ObjectToWorld, IN.positionOS.xyz));
                return OUT;
            }
            
            // 将3D方向向量转换为2D UV坐标（球形映射）
            float2 dirToUV(float3 dir)
            {
                float2 uv = float2(
                    atan2(dir.z, dir.x) / (2.0 * PI) + 0.5,
                    asin(dir.y) / PI + 0.5
                );
                return uv;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                // 将视图方向转换为UV坐标
                float2 uv = dirToUV(normalize(IN.viewDir));
                
                // 将UV坐标分成网格
                float2 gridUV = floor(uv * _StarDensity) / _StarDensity;
                
                // 为每个网格计算一个随机值
                float r = random(gridUV);
                
                // 只有随机值高于阈值的地方才显示星星
                float threshold = 0.95; // 控制星星数量
                float star = (r > threshold) ? 1.0 : 0.0;
                
                // 计算星星在网格内的位置
                float2 center = gridUV + 0.5 / _StarDensity;
                float dist = distance(uv, center);
                
                // 只在距离小于星星大小的地方显示星星
                star *= (dist < _StarSize / (_StarDensity * 2.0)) ? 1.0 : 0.0;
                
                // 添加星星闪烁效果
                float twinkle = noise(gridUV * 5.0 + _Time.y * _StarTwinkleSpeed);
                float brightness = 1.0 - _StarTwinkleAmount + _StarTwinkleAmount * twinkle;
                
                // 混合背景色和星星颜色
                return lerp(_BackgroundColor, _StarColor * brightness, star);
            }
            ENDHLSL
        }
    }
}
