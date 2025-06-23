using UnityEngine;
using UnityEngine.Rendering.Universal;

public class StarfieldRenderer : MonoBehaviour
{
    public Material starfieldMaterial;
    
    private Camera mainCamera;
    private RenderTexture renderTexture;
    
    void Start()
    {
        mainCamera = Camera.main;
        
        // 创建一个平面来渲染星空
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.parent = mainCamera.transform;
        
        // 将四边形放置在相机前方
        quad.transform.localPosition = new Vector3(0, 0, 1);
        quad.transform.localRotation = Quaternion.identity;
        
        // 调整四边形大小以覆盖整个视图
        float distance = 1f;
        float height = 2f * distance * Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * mainCamera.aspect;
        quad.transform.localScale = new Vector3(width, height, 1);
        quad.GetComponent<Renderer>().material = starfieldMaterial;
        mainCamera.nearClipPlane = 0.1f;
    }
}
