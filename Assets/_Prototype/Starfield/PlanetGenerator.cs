using UnityEngine;

public class PlanetGenerator : MonoBehaviour
{
    [Header("星球生成设置")]
    [SerializeField] private GameObject planetPrefab;
    [SerializeField] private Material planetMaterial;
    [SerializeField] private int planetCount = 5;
    [SerializeField] private float spawnRadius = 20f;
    [SerializeField] private Vector2 sizeRange = new Vector2(1f, 3f);
    
    [Header("颜色主题")]
    [SerializeField] private PlanetColorTheme[] colorThemes;
    
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
    
    void Start()
    {
        // 如果没有设置颜色主题，创建默认主题
        if (colorThemes == null || colorThemes.Length == 0)
        {
            CreateDefaultThemes();
        }
        
        GeneratePlanets();
    }
    
    void CreateDefaultThemes()
    {
        colorThemes = new PlanetColorTheme[]
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
    
    void GeneratePlanets()
    {
        for (int i = 0; i < planetCount; i++)
        {
            CreatePlanet(i);
        }
    }
    
    void CreatePlanet(int index)
    {
        // 创建球体
        GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        planet.name = $"CartoonPlanet_{index:00}";
        planet.transform.parent = transform;
        
        // 随机位置
        Vector3 randomPosition = Random.insideUnitSphere * spawnRadius;
        randomPosition.y = Mathf.Abs(randomPosition.y); // 保证在上方
        planet.transform.position = randomPosition;
        
        // 随机大小
        float randomSize = Random.Range(sizeRange.x, sizeRange.y);
        planet.transform.localScale = Vector3.one * randomSize;
        
        // 创建材质实例
        Renderer renderer = planet.GetComponent<Renderer>();
        Material materialInstance = new Material(FindShader());
        
        // 应用颜色主题
        PlanetColorTheme theme = colorThemes[index % colorThemes.Length];
        ApplyThemeToMaterial(materialInstance, theme);
        
        // 随机化一些参数
        RandomizeMaterialProperties(materialInstance);
        
        renderer.material = materialInstance;
                
        Debug.Log($"创建了星球: {planet.name} 使用主题: {theme.name}");
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
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", theme.baseColor);
        if (mat.HasProperty("_SecondaryColor"))
            mat.SetColor("_SecondaryColor", theme.secondaryColor);
        if (mat.HasProperty("_AccentColor"))
            mat.SetColor("_AccentColor", theme.accentColor);
        if (mat.HasProperty("_PoleColor"))
            mat.SetColor("_PoleColor", theme.poleColor);
        if (mat.HasProperty("_AtmosphereColor"))
            mat.SetColor("_AtmosphereColor", theme.atmosphereColor);
        if (mat.HasProperty("_RimLightColor"))
            mat.SetColor("_RimLightColor", theme.rimLightColor);
    }
    
    void RandomizeMaterialProperties(Material mat)
    {
        if (mat.HasProperty("_PatternScale"))
            mat.SetFloat("_PatternScale", Random.Range(5f, 15f));
        if (mat.HasProperty("_RotationSpeed"))
            mat.SetFloat("_RotationSpeed", Random.Range(0.05f, 0.2f));
        if (mat.HasProperty("_NoiseIntensity"))
            mat.SetFloat("_NoiseIntensity", Random.Range(0.2f, 0.5f));
        if (mat.HasProperty("_AtmosphereIntensity"))
            mat.SetFloat("_AtmosphereIntensity", Random.Range(1f, 2f));
    }
        
    [ContextMenu("重新生成星球")]
    public void RegeneratePlanets()
    {
        // 删除现有星球
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        
        // 重新生成
        GeneratePlanets();
    }
    
    void OnDrawGizmosSelected()
    {
        // 绘制生成范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
        
        // 绘制预览位置
        Gizmos.color = Color.green;
        for (int i = 0; i < planetCount; i++)
        {
            Gizmos.DrawWireSphere(transform.position + Vector3.up * i * 2, sizeRange.y);
        }
    }
} 