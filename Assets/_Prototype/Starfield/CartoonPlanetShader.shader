Shader "Custom/CartoonPlanet"
{
    Properties
    {
        [Header(Planet Colors)]
        _BaseColor ("Base Color", Color) = (0.3, 0.8, 0.5, 1)
        _SecondaryColor ("Secondary Color", Color) = (0.1, 0.6, 0.3, 1)
        _AccentColor ("Accent Color", Color) = (1.0, 0.9, 0.4, 1)
        _PoleColor ("Pole Color", Color) = (0.9, 0.9, 1.0, 1)
        
        [Header(Surface Pattern)]
        _PatternScale ("Pattern Scale", Range(1, 20)) = 8
        _PatternContrast ("Pattern Contrast", Range(0.1, 3)) = 1.5
        _PatternSharpness ("Pattern Sharpness", Range(0.1, 5)) = 2.0
        _NoiseIntensity ("Surface Noise", Range(0, 1)) = 0.3
        
        [Header(Atmosphere)]
        _AtmosphereColor ("Atmosphere Color", Color) = (0.4, 0.8, 1.0, 1)
        _AtmosphereThickness ("Atmosphere Thickness", Range(0.01, 0.5)) = 0.1
        _AtmosphereIntensity ("Atmosphere Intensity", Range(0, 3)) = 1.5
        _AtmosphereFalloff ("Atmosphere Falloff", Range(0.5, 5)) = 2.0
        
        [Header(Toon Lighting)]
        [HDR]
        _AmbientColor("Ambient Color", Color) = (0.4,0.4,0.4,1)
        _ToonSteps ("Toon Lighting Steps", Range(2, 8)) = 4
        _LightIntensity ("Light Intensity", Range(0.5, 2)) = 1.2
        _ShadowSoftness ("Shadow Softness", Range(0.001, 0.1)) = 0.005
        
        [Header(Specular)]
        [HDR]
        _SpecularColor("Specular Color", Color) = (0.9,0.9,0.9,1)
        _Glossiness("Glossiness", Float) = 32
        _SpecularSoftness("Specular Softness", Range(0.001, 0.1)) = 0.005
        
        [Header(Rim Lighting)]
        [HDR]
        _RimLightColor ("Rim Light Color", Color) = (1.0, 0.8, 0.6, 1)
        _RimAmount("Rim Amount", Range(0, 1)) = 0.716
        _RimThreshold("Rim Threshold", Range(0, 1)) = 0.1
        
        [Header(Animation)]
        _RotationSpeed ("Rotation Speed", Range(0, 2)) = 0.1
        _AtmosphereSpeed ("Atmosphere Animation", Range(0, 1)) = 0.3
        _PulseIntensity ("Pulse Intensity", Range(0, 1)) = 0.2
        
        [Header(Effects)]
        _EmissionIntensity ("Emission Intensity", Range(0, 2)) = 0.3
        _Metallic ("Metallic", Range(0, 1)) = 0.1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.6
        
        [Header(Cartoon Style)]
        _ColorSteps ("Color Quantization Steps", Range(3, 16)) = 8
        _Saturation ("Color Saturation", Range(0.5, 2)) = 1.3
        _Contrast ("Color Contrast", Range(0.5, 2)) = 1.2
        _Brightness ("Color Brightness", Range(0.5, 1.5)) = 1.1
        _CartoonStrength ("Cartoon Effect Strength", Range(0, 1)) = 0.7
        _OutlineThickness ("Outline Thickness", Range(0, 0.1)) = 0.02
        _OutlineColor ("Outline Color", Color) = (0.1, 0.1, 0.1, 1)
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            // Properties
            half4 _BaseColor, _SecondaryColor, _AccentColor, _PoleColor;
            half _PatternScale, _PatternContrast, _PatternSharpness, _NoiseIntensity;
            half4 _AtmosphereColor;
            half _AtmosphereThickness, _AtmosphereIntensity, _AtmosphereFalloff;
            half4 _AmbientColor;
            half _ToonSteps, _LightIntensity, _ShadowSoftness;
            half4 _SpecularColor;
            half _Glossiness, _SpecularSoftness;
            half4 _RimLightColor;
            half _RimAmount, _RimThreshold;
            half _RotationSpeed, _AtmosphereSpeed, _PulseIntensity;
            half _EmissionIntensity, _Metallic, _Smoothness;
            half _ColorSteps, _Saturation, _Contrast, _Brightness, _CartoonStrength;
            half _OutlineThickness;
            half4 _OutlineColor;
            
            // Hash function for procedural patterns
            float hash(float2 p) {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            
            // Noise function
            float noise(float2 p) {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            // Fractal Brownian Motion
            float fbm(float2 p, int octaves) {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for(int i = 0; i < octaves; i++) {
                    value += amplitude * noise(p * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                return value;
            }
            
            // Generate planet surface pattern
            float3 generatePlanetSurface(float2 uv, float3 worldPos) {
                float time = _Time.y;
                
                // Add rotation
                float2 rotatedUV = uv + float2(time * _RotationSpeed, 0);
                
                // Create continent-like patterns
                float2 patternUV = rotatedUV * _PatternScale;
                float continents = fbm(patternUV, 4);
                float details = fbm(patternUV * 3.2 + time * 0.1, 3) * _NoiseIntensity;
                
                // Sharpen the pattern for cartoon look
                continents = pow(continents + details, _PatternSharpness);
                continents = smoothstep(0.3, 0.7, continents);
                
                // Create latitude-based variation (polar regions)
                float latitude = abs(uv.y - 0.5) * 2;
                float poleInfluence = smoothstep(0.6, 1.0, latitude);
                
                // Mix colors based on patterns
                float3 landColor = lerp(_BaseColor.rgb, _SecondaryColor.rgb, continents);
                landColor = lerp(landColor, _AccentColor.rgb, details * 0.5);
                landColor = lerp(landColor, _PoleColor.rgb, poleInfluence);
                
                return landColor;
            }
            
            // 基于Roystan风格的卡通光照函数
            float toonLighting(float NdotL) {
                // 使用smoothstep创建清晰的光暗分界，参考Roystan shader
                float lightIntensity = smoothstep(0, _ShadowSoftness, NdotL);
                return lightIntensity;
            }
            
            // 卡通风格高光计算
            float toonSpecular(float3 normal, float3 lightDir, float3 viewDir, float lightIntensity) {
                // 计算半向量用于Blinn-Phong高光
                float3 halfVector = normalize(lightDir + viewDir);
                float NdotH = dot(normal, halfVector);
                
                // 应用光泽度，平方以允许艺术家使用更小的值
                float specularIntensity = pow(NdotH * lightIntensity, _Glossiness * _Glossiness);
                
                // 使用smoothstep创建清晰的高光边缘
                float specularIntensitySmooth = smoothstep(0.005, _SpecularSoftness, specularIntensity);
                
                return specularIntensitySmooth;
            }
            
            // 改进的卡通风格边缘光照
            float toonRimLighting(float3 normal, float3 viewDir, float NdotL) {
                float rimDot = 1.0 - dot(viewDir, normal);
                
                // 只在受光面显示边缘光，使用NdotL的幂次来平滑混合
                float rimIntensity = rimDot * pow(NdotL, _RimThreshold);
                
                // 使用smoothstep创建清晰的边缘光边界
                rimIntensity = smoothstep(_RimAmount - 0.01, _RimAmount + 0.01, rimIntensity);
                
                return rimIntensity;
            }
            
            // RGB to HSV conversion
            float3 rgb2hsv(float3 c) {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }
            
            // HSV to RGB conversion
            float3 hsv2rgb(float3 c) {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
            }
            
            // 改进的卡通风格颜色处理 - 减少条纹
            float3 cartoonizeColor(float3 color) {
                // 调整亮度和对比度
                color = (color - 0.5) * _Contrast + 0.5;
                color = color * _Brightness;
                
                // 转换到HSV进行饱和度调整
                float3 hsv = rgb2hsv(color);
                hsv.y *= _Saturation; // 增强饱和度
                color = hsv2rgb(hsv);
                
                // 改进的颜色量化 - 使用更平滑的过渡
                if (_ColorSteps > 12) {
                    // 如果步数很高，只做轻微量化
                    color = floor(color * _ColorSteps + 0.5) / _ColorSteps;
                } else {
                    // 低步数时使用更柔和的量化
                    float3 quantized = floor(color * _ColorSteps) / _ColorSteps;
                    float3 nextQuantized = floor(color * _ColorSteps + 1.0) / _ColorSteps;
                    
                    // 基于原始颜色的明度决定混合比例
                    float luminance = dot(color, float3(0.299, 0.587, 0.114));
                    float mixFactor = smoothstep(0.0, 1.0, frac(luminance * _ColorSteps));
                    color = lerp(quantized, nextQuantized, mixFactor * 0.3); // 30%的混合比例
                }
                
                // 确保颜色在有效范围内
                color = saturate(color);
                
                return color;
            }
            
            // 检测边缘用于描边效果
            float getOutlineFactor(float3 normalWS, float3 viewDir) {
                float dotNV = dot(normalWS, viewDir);
                float outline = 1.0 - abs(dotNV);
                outline = smoothstep(1.0 - _OutlineThickness, 1.0, outline);
                return outline;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = input.uv;
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Get main light
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float3 normalWS = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);
                
                // Generate surface colors
                float3 surfaceColor = generatePlanetSurface(input.uv, input.positionWS);
                
                // 基于Roystan风格的卡通光照计算
                float NdotL = dot(normalWS, lightDir);
                
                // 主要光照计算 - 使用改进的toon lighting
                float lightIntensity = toonLighting(NdotL);
                float3 light = lightIntensity * mainLight.color * _LightIntensity;
                
                // 环境光照
                float3 ambient = _AmbientColor.rgb;
                
                // 高光计算
                float specular = toonSpecular(normalWS, lightDir, viewDir, lightIntensity);
                float3 specularColor = specular * _SpecularColor.rgb;
                
                // 改进的边缘光照
                float rim = toonRimLighting(normalWS, viewDir, NdotL);
                float3 rimColor = rim * _RimLightColor.rgb;
                
                // 组合所有光照效果
                float3 finalColor = surfaceColor * (light + ambient) + specularColor + rimColor;
                
                // 添加脉冲发光效果
                float pulse = 1.0 + sin(_Time.y * 2.0) * _PulseIntensity * 0.1;
                finalColor += surfaceColor * _EmissionIntensity * 0.1 * pulse;
                
                // 保存原始颜色用于混合
                float3 originalColor = finalColor;
                
                // 应用卡通风格化处理
                float3 cartoonColor = cartoonizeColor(finalColor);
                
                // 根据强度参数混合原始颜色和卡通颜色
                finalColor = lerp(originalColor, cartoonColor, _CartoonStrength);
                
                // 添加描边效果
                float outlineFactor = getOutlineFactor(normalWS, viewDir);
                finalColor = lerp(finalColor, _OutlineColor.rgb, outlineFactor * _OutlineColor.a * _CartoonStrength);
                
                // 最终的颜色增强 - 根据卡通强度调整
                finalColor = pow(finalColor, lerp(1.0, 0.8, _CartoonStrength)); // 条件性提亮
                finalColor = saturate(finalColor * lerp(1.0, 1.1, _CartoonStrength)); // 条件性过曝
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
        
        // Atmosphere pass
        Pass
        {
            Name "Atmosphere"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Front
            
            HLSLPROGRAM
            #pragma vertex vertAtmo
            #pragma fragment fragAtmo
            #pragma target 3.0
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct AttributesAtmo
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct VaryingsAtmo
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            half4 _AtmosphereColor;
            half _AtmosphereThickness, _AtmosphereIntensity, _AtmosphereFalloff;
            half _AtmosphereSpeed;
            half _ColorSteps, _Saturation, _Brightness, _CartoonStrength;
            
            VaryingsAtmo vertAtmo(AttributesAtmo input)
            {
                VaryingsAtmo output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                // Expand vertex for atmosphere
                float3 expandedPos = input.positionOS.xyz + input.normalOS * _AtmosphereThickness;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(expandedPos);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = vertexInput.positionCS;
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                
                return output;
            }
            
            half4 fragAtmo(VaryingsAtmo input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);
                
                // Fresnel effect for atmosphere
                float fresnel = 1.0 - dot(normalWS, viewDir);
                fresnel = pow(fresnel, _AtmosphereFalloff);
                
                // Animate atmosphere
                float time = _Time.y * _AtmosphereSpeed;
                float animation = sin(time + fresnel * 10.0) * 0.1 + 0.9;
                
                float3 atmosphereColor = _AtmosphereColor.rgb * _AtmosphereIntensity * animation;
                
                // 对大气层颜色也应用卡通化处理，但强度更温和
                float3 originalAtmo = atmosphereColor;
                atmosphereColor = atmosphereColor * _Brightness;
                atmosphereColor = floor(atmosphereColor * _ColorSteps) / _ColorSteps;
                atmosphereColor = saturate(atmosphereColor * 1.2); // 让大气层更鲜艳
                
                // 根据卡通强度混合大气层效果
                atmosphereColor = lerp(originalAtmo, atmosphereColor, _CartoonStrength * 0.5);
                
                float alpha = fresnel * _AtmosphereColor.a;
                
                return half4(atmosphereColor, alpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
} 