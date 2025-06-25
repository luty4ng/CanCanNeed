Shader "CosmosTech/StarfieldSkybox"
{
    Properties
    {
        _NumLayers ("Number of Layers", Range(1, 16)) = 8
        _Velocity ("Movement Velocity", Range(-0.1, 0.1)) = 0.025
        _StarGlow ("Star Glow", Range(0.001, 0.1)) = 0.025
        _StarSize ("Star Size", Range(0.1, 5)) = 2
        _CanvasView ("Canvas View Scale", Range(5, 50)) = 20
        _TimeScale ("Time Scale", Range(0, 2)) = 1
        _MouseSensitivity ("Mouse Sensitivity", Range(0, 2)) = 1
        _StarColorHue ("Star Color Hue", Range(0, 1)) = 0.2
        _StarColorSat ("Star Color Saturation", Range(0, 2)) = 1
        _StarColorBrightness ("Star Color Brightness", Range(0, 2)) = 1
        _FlickerSpeed ("Flicker Speed", Range(0, 2)) = 0.6
        _FadeDistance ("Fade Distance", Range(0.1, 1)) = 0.9
        _NoiseScale ("Noise Scale", Range(0.1, 5)) = 1
        _TwinkleSpeed ("Twinkle Speed", Range(0, 10)) = 1
    }
    
    SubShader {
        Tags { "RenderType" = "Background" "Queue" = "Background" }

        Pass
        {
            Name "StarField"
            Cull Off
            ZWrite Off
            // ZTest LEqual
            
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
                float3 rayDir : TEXCOORD0;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float _NumLayers;
                float _Velocity;
                float _StarGlow;
                float _StarSize;
                float _CanvasView;
                float _TimeScale;
                float _MouseSensitivity;
                float _StarColorHue;
                float _StarColorSat;
                float _StarColorBrightness;
                float _FlickerSpeed;
                float _FadeDistance;
                float _NoiseScale;
                float _TwinkleSpeed;
            CBUFFER_END
            
            #define TAU 6.28318530718
            #define PI 3.14159265359
            
            // 改进的3D哈希函数，避免周期性问题
            float Hash31(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }
            
            float2 Hash32(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yzx + 33.33);
                return frac((p.xy + p.yz) * p.zx);
            }
            
            // 3D噪声函数
            float Noise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                return lerp(
                    lerp(
                        lerp(Hash31(i + float3(0, 0, 0)), Hash31(i + float3(1, 0, 0)), f.x),
                        lerp(Hash31(i + float3(0, 1, 0)), Hash31(i + float3(1, 1, 0)), f.x), f.y),
                    lerp(
                        lerp(Hash31(i + float3(0, 0, 1)), Hash31(i + float3(1, 0, 1)), f.x),
                        lerp(Hash31(i + float3(0, 1, 1)), Hash31(i + float3(1, 1, 1)), f.x), f.y), f.z);
            }
            
            // 使用3D坐标直接生成星星，避免UV映射问题
            float Star3D(float3 pos, float size, float flare)
            {
                // 获取星星的局部坐标
                float3 cellId = floor(pos * _CanvasView);
                float3 cellPos = frac(pos * _CanvasView);
                
                float minDist = 10.0;
                float3 closestPos = float3(0, 0, 0);
                float closestHash = 0.0;
                
                // 检查3x3x3邻域
                for (int z = -1; z <= 1; z++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int x = -1; x <= 1; x++)
                        {
                            float3 neighbor = cellId + float3(x, y, z);
                            float3 starPoint = Hash32(neighbor).xyy;
                            starPoint.z = Hash31(neighbor + 100.0);
                            starPoint = starPoint + float3(x, y, z);
                            
                            float3 diff = starPoint - cellPos;
                            float dist = length(diff);
                            
                            if (dist < minDist)
                            {
                                minDist = dist;
                                closestPos = starPoint;
                                closestHash = Hash31(neighbor);
                            }
                        }
                    }
                }
                
                // 只有距离足够近才显示星星
                if (minDist > size * 0.1) return 0.0;
                
                // 星星亮度计算
                float intensity = 1.0 - smoothstep(0.0, size * 0.1, minDist);
                
                // 添加星芒效果
                float3 starPos = closestPos - floor(closestPos);
                float rays = 0.0;
                
                // 水平和垂直射线
                rays += max(0.0, 1.0 - abs(starPos.x - 0.5) * 20.0 / size) * 0.3;
                rays += max(0.0, 1.0 - abs(starPos.y - 0.5) * 20.0 / size) * 0.3;
                rays += max(0.0, 1.0 - abs(starPos.z - 0.5) * 20.0 / size) * 0.3;
                
                intensity += rays * flare;
                
                // 闪烁效果
                float flicker = sin(_Time.y * _FlickerSpeed * _TimeScale + closestHash * TAU) * 0.3 + 0.7;
                intensity *= flicker;
                
                return intensity * smoothstep(0.8, 1.0, closestHash);
            }
            
            // 星场层函数 - 使用3D坐标
            float3 StarLayer3D(float3 rayDir, float depth)
            {
                // 添加时间偏移制造运动效果
                float3 pos = rayDir * (1.0 + depth * 10.0);
                pos += float3(0, 0, _Time.y * _Velocity * _TimeScale * _TwinkleSpeed);
                
                // 添加轻微的旋转扰动
                float angle = _Time.y * 0.1 * _TimeScale + depth * 10.0;
                float s = sin(angle);
                float c = cos(angle);
                pos.xy = float2(c * pos.x - s * pos.y, s * pos.x + c * pos.y);
                
                // 缩放调整
                float scale = lerp(0.5, _CanvasView * 0.1, depth);
                pos *= scale;
                
                // 星星大小随深度变化
                float starSize = _StarSize * (0.5 + depth * 0.5);
                float flare = smoothstep(0.1, 0.9, Hash31(pos + depth * 100.0)) * 0.5;
                
                float starIntensity = Star3D(pos, starSize, flare);
                
                if (starIntensity <= 0.0) return float3(0, 0, 0);
                
                // 颜色计算
                float colorSeed = Hash31(pos * 1000.0);
                float3 color = float3(
                    sin(_StarColorHue * TAU + colorSeed * TAU),
                    sin((_StarColorHue + 0.33) * TAU + colorSeed * TAU),
                    sin((_StarColorHue + 0.66) * TAU + colorSeed * TAU)
                ) * 0.5 + 0.5;
                
                color = lerp(float3(1, 1, 1), color, _StarColorSat);
                color *= _StarColorBrightness;
                
                return color * starIntensity;
            }
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                // 天空盒顶点处理
                float4 pos = IN.positionOS;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.rayDir = normalize(mul(unity_ObjectToWorld, pos).xyz);
                
                return OUT;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                float3 rayDir = normalize(IN.rayDir);
                
                // 添加轻微的相机摆动
                float2 camOffset = float2(
                    sin(_Time.y * 0.22 * _TimeScale) * 0.1,
                    cos(_Time.y * 0.17 * _TimeScale) * 0.08
                ) * _MouseSensitivity;
                
                // 应用相机摆动
                float angle = length(camOffset);
                if (angle > 0.001)
                {
                    float3 axis = normalize(float3(-camOffset.y, camOffset.x, 0));
                    float s = sin(angle);
                    float c = cos(angle);
                    float oc = 1.0 - c;
                    
                    // 旋转矩阵
                    float3x3 rot = float3x3(
                        oc * axis.x * axis.x + c, oc * axis.x * axis.y - axis.z * s, oc * axis.z * axis.x + axis.y * s,
                        oc * axis.x * axis.y + axis.z * s, oc * axis.y * axis.y + c, oc * axis.y * axis.z - axis.x * s,
                        oc * axis.z * axis.x - axis.y * s, oc * axis.y * axis.z + axis.x * s, oc * axis.z * axis.z + c
                    );
                    
                    rayDir = mul(rot, rayDir);
                }
                
                float3 col = float3(0, 0, 0);
                int numLayers = (int)_NumLayers;
                
                // 多层星场渲染
                for (int i = 0; i < 16; i++)
                {
                    if (i >= numLayers) break;
                    
                    float layerFraction = (float)i / (float)numLayers;
                    float depth = frac(layerFraction + _Time.y * _Velocity * _TimeScale * 0.1);
                    float fade = depth * smoothstep(1.0, _FadeDistance, depth);
                    
                    float3 layerColor = StarLayer3D(rayDir, depth);
                    col += layerColor * fade;
                }
                
                // 添加微妙的渐变背景
                float gradientFactor = 0.5 + 0.5 * rayDir.y;
                float3 bgColor = lerp(
                    float3(0.01, 0.02, 0.05) * _StarColorBrightness,
                    float3(0.02, 0.03, 0.08) * _StarColorBrightness,
                    gradientFactor
                );
                
                col += bgColor * 0.3;
                
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    
    FallBack "Skybox/Procedural"
}