using UnityEngine;
using System.Collections.Generic;

public class PlanetGenerator : MonoBehaviour
{
    [Header("星球生成设置")]
    [SerializeField] private GameObject m_planetPrefab;
    [SerializeField] private Material m_planetMaterial;
    [SerializeField] private int m_planetCount = 5;
    [SerializeField] private float m_spawnRadius = 20f;
    [SerializeField] private Vector2 m_sizeRange = new Vector2(1f, 3f);
    
    [Header("间距控制")]
    [SerializeField] private float m_minDistance = 5f;
    [SerializeField] private int m_maxAttempts = 50;
    [SerializeField] private bool m_debugPlacement = false;
    
    [Header("颜色主题")]
    [SerializeField] private PlanetColorTheme[] m_colorThemes;
    [SerializeField] private bool m_useRandomThemes = true;
    [SerializeField] private bool m_allowDuplicateThemes = false;
    
    [Header("随机颜色设置")]
    [SerializeField] private ColorPalette[] m_colorPalettes;
    [SerializeField] private float m_saturationRange = 0.3f;
    [SerializeField] private float m_brightnessRange = 0.2f;
    [SerializeField] private bool m_debugColorGeneration = false;
    
    private List<PlanetInfo> m_planetPositions = new List<PlanetInfo>();
    private List<PlanetColorTheme> m_usedThemes = new List<PlanetColorTheme>();
    
    [System.Serializable]
    public struct ColorPalette
    {
        public string name;
        public Color[] baseColors;
        public Color[] accentColors;
        public Color[] atmosphereColors;
    }
    
    [System.Serializable]
    public struct PlanetColorTheme
    {
        public string name;
        public Color baseColor;
        public Color secondaryColor;
        public Color accentColor;
        public Color poleColor;
        public Color atmosphereColor;
        public Color rimLightColor;
    }
    
    [System.Serializable]
    private struct PlanetInfo
    {
        public Vector3 position;
        public float radius;
        
        public PlanetInfo(Vector3 pos, float rad)
        {
            position = pos;
            radius = rad;
        }
    }
    
    void Start()
    {
        // 验证材质设置
        ValidateMaterialSetup();
        
        // 如果没有设置颜色主题，创建默认主题
        if (m_colorThemes == null || m_colorThemes.Length == 0)
        {
            CreateDefaultThemes();
        }
        
        // 如果没有设置调色板，创建默认调色板
        if (m_colorPalettes == null || m_colorPalettes.Length == 0)
        {
            CreateDefaultPalettes();
        }
        
        GeneratePlanets();
    }
    
    void ValidateMaterialSetup()
    {
        if (m_planetMaterial == null)
        {
            Debug.LogWarning("PlanetGenerator: 没有设置星球材质(m_planetMaterial)，将使用默认shader创建材质。" +
                           "建议在Inspector中设置使用CartoonPlanet shader的材质以获得最佳效果。");
        }
        else
        {
            if (m_planetMaterial.shader == null)
            {
                Debug.LogError("PlanetGenerator: 设置的材质没有有效的Shader！");
            }
            else if (m_debugColorGeneration)
            {
                Debug.Log($"PlanetGenerator: 使用材质 '{m_planetMaterial.name}' (Shader: {m_planetMaterial.shader.name})");
            }
        }
    }
    
    void CreateDefaultThemes()
    {
        m_colorThemes = new PlanetColorTheme[]
        {
            new PlanetColorTheme
            {
                name = "地球风格",
                baseColor = new Color(0.3f, 0.8f, 0.5f, 1f),
                secondaryColor = new Color(0.1f, 0.6f, 0.3f, 1f),
                accentColor = new Color(1f, 0.9f, 0.4f, 1f),
                poleColor = new Color(0.9f, 0.9f, 1f, 1f),
                atmosphereColor = new Color(0.4f, 0.8f, 1f, 1f),
                rimLightColor = new Color(1f, 0.8f, 0.6f, 1f)
            },
            new PlanetColorTheme
            {
                name = "火星风格",
                baseColor = new Color(0.8f, 0.4f, 0.2f, 1f),
                secondaryColor = new Color(0.6f, 0.3f, 0.1f, 1f),
                accentColor = new Color(1f, 0.6f, 0.3f, 1f),
                poleColor = new Color(1f, 0.8f, 0.7f, 1f),
                atmosphereColor = new Color(1f, 0.6f, 0.4f, 0.8f),
                rimLightColor = new Color(1f, 0.5f, 0.3f, 1f)
            },
            new PlanetColorTheme
            {
                name = "冰雪星球",
                baseColor = new Color(0.8f, 0.9f, 1f, 1f),
                secondaryColor = new Color(0.6f, 0.8f, 0.9f, 1f),
                accentColor = new Color(0.9f, 0.95f, 1f, 1f),
                poleColor = new Color(1f, 1f, 1f, 1f),
                atmosphereColor = new Color(0.7f, 0.9f, 1f, 1f),
                rimLightColor = new Color(0.8f, 0.9f, 1f, 1f)
            },
            new PlanetColorTheme
            {
                name = "毒气星球",
                baseColor = new Color(0.6f, 0.8f, 0.3f, 1f),
                secondaryColor = new Color(0.4f, 0.6f, 0.2f, 1f),
                accentColor = new Color(0.8f, 1f, 0.4f, 1f),
                poleColor = new Color(0.7f, 0.9f, 0.5f, 1f),
                atmosphereColor = new Color(0.5f, 0.8f, 0.2f, 1f),
                rimLightColor = new Color(0.6f, 1f, 0.3f, 1f)
            },
            new PlanetColorTheme
            {
                name = "熔岩星球",
                baseColor = new Color(0.2f, 0.1f, 0.1f, 1f),
                secondaryColor = new Color(0.6f, 0.2f, 0.1f, 1f),
                accentColor = new Color(1f, 0.4f, 0.1f, 1f),
                poleColor = new Color(0.4f, 0.2f, 0.2f, 1f),
                atmosphereColor = new Color(1f, 0.3f, 0.1f, 0.9f),
                rimLightColor = new Color(1f, 0.5f, 0.2f, 1f)
            }
        };
    }
    
    void CreateDefaultPalettes()
    {
        m_colorPalettes = new ColorPalette[]
        {
            new ColorPalette
            {
                name = "地球色调",
                baseColors = new Color[]
                {
                    new Color(0.2f, 0.6f, 0.3f, 1f), // 森林绿
                    new Color(0.1f, 0.4f, 0.8f, 1f), // 海洋蓝
                    new Color(0.6f, 0.4f, 0.2f, 1f), // 大地棕
                    new Color(0.3f, 0.7f, 0.4f, 1f)  // 草地绿
                },
                accentColors = new Color[]
                {
                    new Color(1f, 0.9f, 0.3f, 1f),   // 阳光黄
                    new Color(0.9f, 0.6f, 0.3f, 1f), // 沙漠橙
                    new Color(0.8f, 0.8f, 0.9f, 1f)  // 云朵白
                },
                atmosphereColors = new Color[]
                {
                    new Color(0.4f, 0.8f, 1f, 1f),   // 天空蓝
                    new Color(0.6f, 0.9f, 1f, 1f),   // 淡蓝
                    new Color(0.8f, 0.9f, 1f, 1f)    // 薄雾蓝
                }
            },
            new ColorPalette
            {
                name = "火星色调",
                baseColors = new Color[]
                {
                    new Color(0.8f, 0.3f, 0.1f, 1f), // 火星红
                    new Color(0.6f, 0.2f, 0.1f, 1f), // 深红
                    new Color(0.9f, 0.5f, 0.2f, 1f), // 橙红
                    new Color(0.7f, 0.4f, 0.2f, 1f)  // 锈红
                },
                accentColors = new Color[]
                {
                    new Color(1f, 0.6f, 0.2f, 1f),   // 暖橙
                    new Color(1f, 0.8f, 0.4f, 1f),   // 金黄
                    new Color(0.8f, 0.5f, 0.3f, 1f)  // 赤土
                },
                atmosphereColors = new Color[]
                {
                    new Color(1f, 0.4f, 0.2f, 0.8f), // 橙色大气
                    new Color(0.9f, 0.5f, 0.3f, 0.7f), // 暖橙大气
                    new Color(1f, 0.6f, 0.4f, 0.6f)  // 淡橙大气
                }
            },
            new ColorPalette
            {
                name = "异域色调",
                baseColors = new Color[]
                {
                    new Color(0.6f, 0.2f, 0.8f, 1f), // 紫色
                    new Color(0.2f, 0.8f, 0.6f, 1f), // 青绿
                    new Color(0.8f, 0.2f, 0.4f, 1f), // 品红
                    new Color(0.2f, 0.4f, 0.8f, 1f)  // 蓝紫
                },
                accentColors = new Color[]
                {
                    new Color(1f, 0.3f, 0.7f, 1f),   // 亮粉
                    new Color(0.3f, 1f, 0.8f, 1f),   // 荧光绿
                    new Color(0.7f, 0.3f, 1f, 1f)    // 亮紫
                },
                atmosphereColors = new Color[]
                {
                    new Color(0.5f, 0.3f, 1f, 1f),   // 紫色大气
                    new Color(0.3f, 1f, 0.7f, 1f),   // 绿色大气
                    new Color(1f, 0.3f, 0.6f, 1f)    // 粉色大气
                }
            }
        };
    }
    
    PlanetColorTheme GenerateRandomTheme(int index)
    {
        PlanetColorTheme newTheme;
        int maxAttempts = 20;
        int attempts = 0;
        
        do
        {
            newTheme = CreateRandomTheme(index);
            attempts++;
            
            if (m_allowDuplicateThemes || IsThemeUnique(newTheme))
            {
                break;
            }
            
            if (m_debugColorGeneration)
            {
                Debug.Log($"主题重复，重新生成... (尝试 {attempts}/{maxAttempts})");
            }
            
        } while (attempts < maxAttempts);
        
        if (!m_allowDuplicateThemes)
        {
            m_usedThemes.Add(newTheme);
        }
        
        if (m_debugColorGeneration)
        {
            Debug.Log($"为星球 {index} 生成主题: {newTheme.name}");
            Debug.Log($"基色: {ColorToHex(newTheme.baseColor)}, 大气: {ColorToHex(newTheme.atmosphereColor)}");
        }
        
        return newTheme;
    }
    
    PlanetColorTheme CreateRandomTheme(int index)
    {
        // 随机选择调色板
        ColorPalette palette = m_colorPalettes[Random.Range(0, m_colorPalettes.Length)];
        
        // 生成基础颜色
        Color baseColor = GetRandomColorFromArray(palette.baseColors);
        baseColor = VaryColor(baseColor, m_saturationRange, m_brightnessRange);
        
        // 生成次要颜色（基于基础颜色的变化）
        Color secondaryColor = VaryColor(baseColor, m_saturationRange * 0.8f, m_brightnessRange * 0.6f);
        secondaryColor = Color.Lerp(secondaryColor, GetRandomColorFromArray(palette.baseColors), 0.3f);
        
        // 生成强调色
        Color accentColor = GetRandomColorFromArray(palette.accentColors);
        accentColor = VaryColor(accentColor, m_saturationRange * 0.5f, m_brightnessRange * 0.5f);
        
        // 生成极地颜色（通常更亮更冷）
        Color poleColor = Color.Lerp(baseColor, Color.white, 0.4f);
        poleColor = VaryColor(poleColor, m_saturationRange * 0.3f, m_brightnessRange * 0.3f);
        
        // 生成大气颜色
        Color atmosphereColor = GetRandomColorFromArray(palette.atmosphereColors);
        atmosphereColor = VaryColor(atmosphereColor, m_saturationRange * 0.6f, m_brightnessRange * 0.4f);
        
        // 生成边缘光颜色（通常与大气相关）
        Color rimLightColor = Color.Lerp(atmosphereColor, accentColor, 0.5f);
        rimLightColor = VaryColor(rimLightColor, m_saturationRange * 0.4f, m_brightnessRange * 0.3f);
        
        return new PlanetColorTheme
        {
            name = $"随机主题_{index:00}_{palette.name}",
            baseColor = baseColor,
            secondaryColor = secondaryColor,
            accentColor = accentColor,
            poleColor = poleColor,
            atmosphereColor = atmosphereColor,
            rimLightColor = rimLightColor
        };
    }
    
    Color GetRandomColorFromArray(Color[] colors)
    {
        if (colors == null || colors.Length == 0)
        {
            return Color.gray;
        }
        return colors[Random.Range(0, colors.Length)];
    }
    
    Color VaryColor(Color originalColor, float saturationVariation, float brightnessVariation)
    {
        // 转换到HSV进行调整
        Color.RGBToHSV(originalColor, out float h, out float s, out float v);
        
        // 随机调整饱和度和亮度
        s += Random.Range(-saturationVariation, saturationVariation);
        v += Random.Range(-brightnessVariation, brightnessVariation);
        
        // 确保值在有效范围内
        s = Mathf.Clamp01(s);
        v = Mathf.Clamp01(v);
        
        Color newColor = Color.HSVToRGB(h, s, v);
        newColor.a = originalColor.a; // 保持原始alpha值
        
        return newColor;
    }
    
    bool IsThemeUnique(PlanetColorTheme newTheme)
    {
        foreach (PlanetColorTheme usedTheme in m_usedThemes)
        {
            // 检查颜色相似度
            if (ColorsAreSimilar(newTheme.baseColor, usedTheme.baseColor, 0.2f) &&
                ColorsAreSimilar(newTheme.atmosphereColor, usedTheme.atmosphereColor, 0.3f))
            {
                return false;
            }
        }
        return true;
    }
    
    bool ColorsAreSimilar(Color color1, Color color2, float threshold)
    {
        float distance = Mathf.Sqrt(
            Mathf.Pow(color1.r - color2.r, 2) +
            Mathf.Pow(color1.g - color2.g, 2) +
            Mathf.Pow(color1.b - color2.b, 2)
        );
        return distance < threshold;
    }
    
    string ColorToHex(Color color)
    {
        return $"#{ColorUtility.ToHtmlStringRGB(color)}";
    }
    
    void GeneratePlanets()
    {
        m_planetPositions.Clear();
        m_usedThemes.Clear();
        
        for (int i = 0; i < m_planetCount; i++)
        {
            if (!CreatePlanet(i))
            {
                Debug.LogWarning($"无法为第{i + 1}个星球找到合适的位置，跳过创建");
            }
        }
        
        Debug.Log($"成功创建了 {m_planetPositions.Count}/{m_planetCount} 个星球");
    }
    
    bool CreatePlanet(int index)
    {
        // 先确定星球大小
        float planetSize = Random.Range(m_sizeRange.x, m_sizeRange.y);
        float planetRadius = planetSize * 0.5f; // 球体半径
        
        Vector3 planetPosition = Vector3.zero;
        bool positionFound = false;
        
        // 尝试找到不重叠的位置
        for (int attempt = 0; attempt < m_maxAttempts; attempt++)
        {
            // 生成随机位置
            Vector3 candidatePosition = GenerateRandomPosition();
            
            // 检查是否与现有星球重叠
            if (IsPositionValid(candidatePosition, planetRadius))
            {
                planetPosition = candidatePosition;
                positionFound = true;
                
                if (m_debugPlacement)
                {
                    Debug.Log($"星球 {index} 在第 {attempt + 1} 次尝试后找到位置: {planetPosition}");
                }
                break;
            }
        }
        
        if (!positionFound)
        {
            if (m_debugPlacement)
            {
                Debug.LogWarning($"星球 {index} 在 {m_maxAttempts} 次尝试后仍未找到合适位置");
            }
            return false;
        }
        
        // 创建星球GameObject
        GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        planet.name = $"CartoonPlanet_{index:00}";
        planet.transform.parent = transform;
        planet.transform.position = planetPosition;
        planet.transform.localScale = Vector3.one * planetSize;
        
        // 记录星球信息
        m_planetPositions.Add(new PlanetInfo(planetPosition, planetRadius));
        
        // 创建材质实例
        Renderer renderer = planet.GetComponent<Renderer>();
        Material materialInstance;
        
        if (m_planetMaterial != null)
        {
            // 使用指定的材质创建实例
            materialInstance = new Material(m_planetMaterial);
        }
        else
        {
            // 如果没有指定材质，尝试查找shader创建材质
            materialInstance = new Material(FindShader());
            Debug.LogWarning($"星球 {index} 没有设置材质，使用默认shader创建材质");
        }
        
        // 应用颜色主题
        PlanetColorTheme theme;
        if (m_useRandomThemes)
        {
            theme = GenerateRandomTheme(index);
        }
        else
        {
            theme = m_colorThemes[index % m_colorThemes.Length];
        }
        ApplyThemeToMaterial(materialInstance, theme);
        
        // 随机化一些参数
        RandomizeMaterialProperties(materialInstance);
        
        renderer.material = materialInstance;
        
        Debug.Log($"创建了星球: {planet.name} 使用主题: {theme.name}，位置: {planetPosition}");
        return true;
    }
    
    Vector3 GenerateRandomPosition()
    {
        Vector3 randomPosition = Random.insideUnitSphere * m_spawnRadius;
        randomPosition.y = Mathf.Abs(randomPosition.y); // 保证在上方
        return randomPosition;
    }
    
    bool IsPositionValid(Vector3 position, float radius)
    {
        foreach (PlanetInfo existingPlanet in m_planetPositions)
        {
            float distance = Vector3.Distance(position, existingPlanet.position);
            float requiredDistance = radius + existingPlanet.radius + m_minDistance;
            
            if (distance < requiredDistance)
            {
                if (m_debugPlacement)
                {
                    Debug.Log($"位置冲突: 距离={distance:F2}, 需要距离={requiredDistance:F2}");
                }
                return false;
            }
        }
        return true;
    }
    
    Shader FindShader()
    {
        // 尝试查找卡通星球shader
        Shader shader = Shader.Find("Custom/CartoonPlanet");
        if (shader == null)
        {
            // 如果找不到，使用标准shader
            shader = Shader.Find("Universal Render Pipeline/Lit");
            Debug.LogWarning("找不到 Custom/CartoonPlanet shader，使用默认shader");
        }
        return shader;
    }
    
    void ApplyThemeToMaterial(Material mat, PlanetColorTheme theme)
    {
        // 应用基础颜色
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", theme.baseColor);
        if (mat.HasProperty("_SecondaryColor"))
            mat.SetColor("_SecondaryColor", theme.secondaryColor);
        if (mat.HasProperty("_AccentColor"))
            mat.SetColor("_AccentColor", theme.accentColor);
        if (mat.HasProperty("_PoleColor"))
            mat.SetColor("_PoleColor", theme.poleColor);
        
        // 应用大气和边缘光颜色
        if (mat.HasProperty("_AtmosphereColor"))
            mat.SetColor("_AtmosphereColor", theme.atmosphereColor);
        
        // 兼容新旧shader的边缘光属性名
        if (mat.HasProperty("_RimColor"))
            mat.SetColor("_RimColor", theme.rimLightColor);
        else if (mat.HasProperty("_RimLightColor"))
            mat.SetColor("_RimLightColor", theme.rimLightColor);
        
        // 设置海洋颜色（如果材质支持）
        if (mat.HasProperty("_OceanColor"))
        {
            // 基于基础颜色生成海洋色调
            Color oceanColor = Color.Lerp(theme.baseColor, new Color(0.1f, 0.3f, 0.7f, 1f), 0.6f);
            mat.SetColor("_OceanColor", oceanColor);
        }
        
        if (m_debugColorGeneration)
        {
            Debug.Log($"应用主题到材质: {theme.name}");
        }
    }
    
    void RandomizeMaterialProperties(Material mat)
    {
        // 表面图案参数
        if (mat.HasProperty("_PatternScale"))
            mat.SetFloat("_PatternScale", Random.Range(5f, 15f));
        if (mat.HasProperty("_PatternContrast"))
            mat.SetFloat("_PatternContrast", Random.Range(1.0f, 2.5f));
        if (mat.HasProperty("_NoiseIntensity"))
            mat.SetFloat("_NoiseIntensity", Random.Range(0.2f, 0.5f));
        
        // 海洋和云层参数
        if (mat.HasProperty("_OceanLevel"))
            mat.SetFloat("_OceanLevel", Random.Range(0.2f, 0.5f));
        if (mat.HasProperty("_CloudDensity"))
            mat.SetFloat("_CloudDensity", Random.Range(0.2f, 0.6f));
        
        // 动画参数
        if (mat.HasProperty("_RotationSpeed"))
            mat.SetFloat("_RotationSpeed", Random.Range(0.05f, 0.2f));
        if (mat.HasProperty("_CloudSpeed"))
            mat.SetFloat("_CloudSpeed", Random.Range(0.1f, 0.3f));
        if (mat.HasProperty("_PulseSpeed"))
            mat.SetFloat("_PulseSpeed", Random.Range(0.5f, 2.0f));
        
        // 大气层参数 - 兼容新旧shader
        if (mat.HasProperty("_AtmosphereGlow"))
            mat.SetFloat("_AtmosphereGlow", Random.Range(0.8f, 1.5f));
        else if (mat.HasProperty("_AtmosphereIntensity"))
            mat.SetFloat("_AtmosphereIntensity", Random.Range(1f, 2f));
        
        if (mat.HasProperty("_AtmosphereThickness"))
            mat.SetFloat("_AtmosphereThickness", Random.Range(0.05f, 0.12f));
        
        // 卡通风格参数
        if (mat.HasProperty("_ToonSteps"))
            mat.SetFloat("_ToonSteps", Random.Range(2f, 4f));
        if (mat.HasProperty("_CartoonStrength"))
            mat.SetFloat("_CartoonStrength", Random.Range(0.6f, 0.9f));
        
        if (m_debugColorGeneration)
        {
            Debug.Log($"随机化材质属性完成");
        }
    }
        
    [ContextMenu("重新生成星球")]
    public void RegeneratePlanets()
    {
        // 删除现有星球
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        
        // 清空位置记录和已使用主题
        m_planetPositions.Clear();
        m_usedThemes.Clear();
        
        // 重新生成
        GeneratePlanets();
    }
    
    [ContextMenu("切换到随机主题")]
    public void EnableRandomThemes()
    {
        m_useRandomThemes = true;
        RegeneratePlanets();
    }
    
    [ContextMenu("切换到预设主题")]
    public void EnablePresetThemes()
    {
        m_useRandomThemes = false;
        RegeneratePlanets();
    }
    
    [ContextMenu("生成新的调色板")]
    public void RegenerateColorPalettes()
    {
        CreateDefaultPalettes();
        if (m_useRandomThemes)
        {
            RegeneratePlanets();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // 绘制生成范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, m_spawnRadius);
        
        // 绘制现有星球的最小间距范围
        if (m_planetPositions != null && m_planetPositions.Count > 0)
        {
            Gizmos.color = Color.red;
            foreach (PlanetInfo planet in m_planetPositions)
            {
                // 绘制星球半径
                Gizmos.DrawWireSphere(planet.position, planet.radius);
                
                // 绘制最小间距范围
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(planet.position, planet.radius + m_minDistance);
            }
        }
        
        // 绘制预览位置（当没有实际星球时）
        if (m_planetPositions == null || m_planetPositions.Count == 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < m_planetCount; i++)
            {
                Gizmos.DrawWireSphere(transform.position + Vector3.up * i * 2, m_sizeRange.y);
            }
        }
    }
} 