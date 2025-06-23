using UnityEngine;

public class MultiLayerStarfield : MonoBehaviour
{
    [Header("远景星空")]
    public Material skyboxMaterial;
    
    [Header("中景星云")]
    public int nebulaCount = 10;
    public GameObject[] nebulaPrefabs;
    public float nebulaDistance = 500f;
    
    [Header("近景星星")]
    public int starCount = 2000;
    public GameObject starPrefab;
    public float starfieldRadius = 200f;
    
    void Start()
    {
        // 设置 Skybox
        RenderSettings.skybox = skyboxMaterial;
        
        // 生成星云
        for (int i = 0; i < nebulaCount; i++)
        {
            if (nebulaPrefabs.Length > 0)
            {
                GameObject nebulaPrefab = nebulaPrefabs[Random.Range(0, nebulaPrefabs.Length)];
                Vector3 randomDir = Random.onUnitSphere;
                Vector3 position = randomDir * nebulaDistance;
                
                GameObject nebula = Instantiate(nebulaPrefab, position, Quaternion.LookRotation(randomDir));
                nebula.transform.parent = transform;
            }
        }
        
        // 生成星星
        for (int i = 0; i < starCount; i++)
        {
            Vector3 randomPos = Random.insideUnitSphere * starfieldRadius;
            GameObject star = Instantiate(starPrefab, randomPos, Quaternion.identity);
            
            float scale = Random.Range(0.05f, 0.3f);
            star.transform.localScale = new Vector3(scale, scale, scale);
            
            // 随机星星亮度
            if (star.GetComponent<Light>())
            {
                star.GetComponent<Light>().intensity = Random.Range(0.1f, 1.0f);
            }
            
            star.transform.parent = transform;
        }
    }
}
