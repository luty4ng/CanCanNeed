Shader "CosmosTech/NoMansSkyStyleSkybox"
{
    Properties
    {
        [Header(Main Theme)]
        _ThemeColor ("Theme Color", Color) = (0.5, 0.8, 1.0, 1)
        _ThemeIntensity ("Theme Intensity", Range(0.5, 3)) = 1.5
        _ColorVariation ("Color Variation", Range(0, 1)) = 0.3
        
        [Header(Sky Background)]
        _BackgroundIntensity ("Background Intensity", Range(0.5, 3)) = 2.0
        _GradientPower ("Gradient Power", Range(0.5, 4)) = 1.8
        
        [Header(Nebula)]
        _NebulaIntensity ("Nebula Intensity", Range(0, 3)) = 1.5
        _NebulaScale ("Nebula Scale", Range(0.5, 8)) = 3.0
        _NebulaSpeed ("Nebula Animation Speed", Range(0, 1)) = 0.2
        _NebulaCoverage ("Nebula Coverage", Range(0.1, 0.9)) = 0.4
        
        [Header(Stars)]
        _StarDensity ("Star Density", Range(10, 500)) = 150
        _StarBrightness ("Star Brightness", Range(0, 5)) = 2.5
        _StarSize ("Star Size", Range(0.005, 0.05)) = 0.015
        _StarFlareIntensity ("Star Flare Intensity", Range(0, 3)) = 1.8
        _StarMovementSpeed ("Star Movement Speed", Range(0, 2)) = 0.1
        _StarMovementRange ("Star Movement Range", Range(0, 1)) = 0.3
        
        [Header(Cosmic Dust)]
        _DustIntensity ("Dust Intensity", Range(0, 1)) = 0.4
        _DustDensity ("Dust Density", Range(50, 1000)) = 400
        _DustSpeed ("Dust Movement Speed", Range(0, 1)) = 0.3
        

        
        [Header(Effects)]
        _CosmicRayCount ("Cosmic Ray Count", Range(0, 20)) = 8
        _CosmicRayIntensity ("Cosmic Ray Intensity", Range(0, 3)) = 1.5
        
        [Header(Animation)]
        _GlobalAnimationSpeed ("Global Animation Speed", Range(0, 3)) = 1.0
        _PulseIntensity ("Global Pulse Intensity", Range(0, 1)) = 0.2
        
        [Header(Advanced)]
        _Seed ("Random Seed", Range(0, 999)) = 123
        _NoiseQuality ("Noise Quality", Range(2, 6)) = 4
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Background" 
            "Queue" = "Background" 
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            // Simplified Properties
            half4 _ThemeColor;
            half _ThemeIntensity, _ColorVariation;
            half _BackgroundIntensity, _GradientPower;
            half _NebulaIntensity, _NebulaScale, _NebulaSpeed, _NebulaCoverage;
            half _StarDensity, _StarBrightness, _StarSize, _StarFlareIntensity;
            half _StarMovementSpeed, _StarMovementRange;
            half _DustIntensity, _DustDensity, _DustSpeed;
            half _CosmicRayCount, _CosmicRayIntensity;
            half _GlobalAnimationSpeed, _PulseIntensity;
            half _Seed, _NoiseQuality;
            
            float hash21(float2 p) {
                p = frac(p * float2(443.8975, 397.2973) + _Seed * 0.1);
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }
            
            float2x2 rotate2D(float angle) {
                float s = sin(angle);
                float c = cos(angle);
                return float2x2(c, -s, s, c);
            }
            
            float noise2D(float2 p) {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
                
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            float fbm(float2 p, int octaves) {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for(int i = 0; i < octaves; i++) {
                    value += amplitude * noise2D(p * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                    p = mul(p, rotate2D(0.5));
                }
                return value;
            }
            
            // Generate theme-based colors
            float3 getThemeColor(float variation, float brightness) {
                float time = _Time.y * _GlobalAnimationSpeed;
                float3 baseColor = _ThemeColor.rgb;
                
                // Create color variations based on theme
                float3 color1 = baseColor * float3(1.2, 0.8, 1.1); // Warmer variant
                float3 color2 = baseColor * float3(0.8, 1.1, 1.3); // Cooler variant
                float3 color3 = baseColor * float3(1.1, 1.2, 0.9); // Shifted variant
                
                float colorMix1 = sin(time * 0.3 + variation * 6.28) * 0.5 + 0.5;
                float colorMix2 = cos(time * 0.2 + variation * 4.71) * 0.5 + 0.5;
                
                float3 finalColor = lerp(color1, color2, colorMix1 * _ColorVariation);
                finalColor = lerp(finalColor, color3, colorMix2 * _ColorVariation * 0.5);
                
                return finalColor * brightness * _ThemeIntensity;
            }
            
            float3 generateCosmicBackground(float3 viewDir) {
                float height = viewDir.y;
                float time = _Time.y * _GlobalAnimationSpeed;
                
                // Create height-based color variations
                float3 horizonColor = getThemeColor(0.0, 1.0);
                float3 zenithColor = getThemeColor(0.33, 0.6);
                float3 nadirColor = getThemeColor(0.66, 0.8);
                
                float3 skyColor;
                if(height > 0) {
                    skyColor = lerp(horizonColor, zenithColor, pow(height, _GradientPower));
                } else {
                    skyColor = lerp(horizonColor, nadirColor, pow(-height, _GradientPower * 0.8));
                }
                
                return skyColor * _BackgroundIntensity;
            }
            
            float3 generateNebula(float3 viewDir) {
                float time = _Time.y * _GlobalAnimationSpeed;
                float2 uv = viewDir.xz / (abs(viewDir.y) + 0.2);
                
                float2 nebulaUV = uv * _NebulaScale + time * _NebulaSpeed * float2(0.1, 0.15);
                float nebula1 = fbm(nebulaUV, (int)_NoiseQuality);
                float nebula2 = fbm(nebulaUV * 1.7 + time * 0.08, (int)_NoiseQuality - 1);
                
                float finalNebula = fbm(nebulaUV + float2(nebula1, nebula2) * 0.3, (int)_NoiseQuality);
                float nebulaMask = smoothstep(_NebulaCoverage - 0.2, _NebulaCoverage + 0.3, finalNebula);
                nebulaMask = pow(nebulaMask, 2.0);
                
                // Use theme colors for nebula
                float3 nebulaColor1 = getThemeColor(nebula1, 1.0);
                float3 nebulaColor2 = getThemeColor(nebula2, 0.8);
                float3 finalNebulaColor = lerp(nebulaColor1, nebulaColor2, 0.6);
                
                float heightFactor = smoothstep(-0.3, 0.8, viewDir.y);
                return finalNebulaColor * nebulaMask * _NebulaIntensity * heightFactor;
            }
            
            float3 generateMovingStars(float2 uv) {
                float time = _Time.y * _GlobalAnimationSpeed;
                
                // Add global star movement
                float2 starMovement = float2(
                    sin(time * _StarMovementSpeed) * _StarMovementRange,
                    cos(time * _StarMovementSpeed * 0.7) * _StarMovementRange * 0.5
                );
                uv += starMovement;
                
                float2 gv = frac(uv * _StarDensity) - 0.5;
                float2 id = floor(uv * _StarDensity);
                
                float n = hash21(id);
                if(n < 0.85) return float3(0, 0, 0);
                
                // Individual star movement
                float2 starOffset = (hash21(id + 0.1) - 0.5) * 0.7;
                float2 localMovement = float2(
                    sin(time * _StarMovementSpeed * 2 + n * 6.28) * 0.1,
                    cos(time * _StarMovementSpeed * 1.5 + n * 4.71) * 0.1
                ) * _StarMovementRange;
                
                float2 p = gv - starOffset - localMovement;
                float dist = length(p);
                
                float starCore = smoothstep(_StarSize, 0.0, dist);
                float starGlow = smoothstep(_StarSize * 4, 0.0, dist) * 0.3;
                
                // Star flares
                float flareH = smoothstep(_StarSize * 15, 0.0, abs(p.x)) * smoothstep(_StarSize * 1.2, 0.0, abs(p.y));
                float flareV = smoothstep(_StarSize * 15, 0.0, abs(p.y)) * smoothstep(_StarSize * 1.2, 0.0, abs(p.x));
                float totalFlare = (flareH + flareV) * _StarFlareIntensity;
                
                // Theme-based star color
                float3 starColor = getThemeColor(n, 1.2);
                float pulse = 1.0 + sin(time * 2 + n * 6.28) * _PulseIntensity;
                
                return (starCore + starGlow + totalFlare) * starColor * _StarBrightness * pulse;
            }
            
            float3 generateCosmicDust(float2 uv) {
                float time = _Time.y * _GlobalAnimationSpeed;
                float2 dustUV = uv + time * _DustSpeed * float2(0.05, -0.03);
                
                float2 gv = frac(dustUV * _DustDensity) - 0.5;
                float2 id = floor(dustUV * _DustDensity);
                
                float n = hash21(id);
                if(n < 0.7) return float3(0, 0, 0);
                
                float2 dustPos = (hash21(id + 0.2) - 0.5) * 0.8;
                float dist = length(gv - dustPos);
                
                float dust = smoothstep(0.003, 0.0, dist);
                
                float3 dustColor = getThemeColor(n, 0.6);
                float shimmer = 1.0 + sin(time * 2.0 + n * 12.56) * 0.2;
                
                return dust * dustColor * _DustIntensity * shimmer;
            }
            

            
            float3 generateCosmicRays(float3 viewDir) {
                float3 result = float3(0, 0, 0);
                float time = _Time.y * _GlobalAnimationSpeed;
                
                for(int i = 0; i < (int)_CosmicRayCount; i++) {
                    float seed = i * 17.3 + _Seed;
                    float rayTime = time * 0.5 + seed;
                    float cycle = fmod(rayTime, 15.0);
                    
                    if(cycle > 3.0) continue;
                    
                    float3 rayStart = normalize(float3(
                        sin(seed * 2.1),
                        sin(seed * 1.7) * 0.5,
                        cos(seed * 2.3)
                    ));
                    
                    float3 rayEnd = rayStart + normalize(float3(
                        sin(seed * 1.9),
                        -0.3,
                        cos(seed * 2.1)
                    )) * 0.8;
                    
                    float3 rayPos = lerp(rayStart, rayEnd, cycle / 3.0);
                    float distToRay = distance(viewDir, normalize(rayPos));
                    
                    float ray = smoothstep(0.009, 0.0, distToRay);
                    ray *= (1.0 - cycle / 3.0);
                    ray *= smoothstep(0.0, 0.5, cycle / 3.0);
                    
                    float3 rayColor = getThemeColor(seed * 0.1, 1.0);
                    result += ray * rayColor * _CosmicRayIntensity;
                }
                
                return result;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDir = normalize(input.positionWS - _WorldSpaceCameraPos);
                float2 uv = viewDir.xz / (abs(viewDir.y) + 0.15);
                
                // Generate all elements
                float3 background = generateCosmicBackground(viewDir);
                float3 nebula = generateNebula(viewDir);
                float3 stars = generateMovingStars(uv);
                float3 dust = generateCosmicDust(uv);
                float3 cosmicRays = generateCosmicRays(viewDir);
                
                // Combine all elements
                float3 finalColor = background;
                finalColor += nebula;
                finalColor = lerp(finalColor, finalColor + dust, 0.8);
                finalColor += stars;
                finalColor += cosmicRays;
                
                // No Man's Sky style post-processing
                float luminance = dot(finalColor, float3(0.299, 0.587, 0.114));
                finalColor = lerp(float3(luminance, luminance, luminance), finalColor, 1.8);
                
                // Global pulse effect
                float globalPulse = 1.0 + sin(_Time.y * _GlobalAnimationSpeed * 0.5) * _PulseIntensity * 0.1;
                finalColor *= globalPulse;
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    
    FallBack "Skybox/Procedural"
} 