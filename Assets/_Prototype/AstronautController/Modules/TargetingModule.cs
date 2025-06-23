using System;
using UnityEngine;
using PlayerController.Modules;

namespace PlayerController.Modules
{
    /// <summary>
    /// 处理宇航员的目标选择和管理
    /// </summary>
    [Serializable]
    [ModuleDisplayName("目标选择模块")]
    public class TargetingModule : AstronautModuleBase
    {
        public override void OnUpdate()
        {
            // 处理目标检测和选择
            HandleTargeting();
            
            // 解除目标选择 (右键)
            if (Input.GetMouseButtonDown(1) && Data.currentTarget != null)
            {
                UnselectTarget();
            }
        }
        
        private void HandleTargeting()
        {
            // 射线检测潜在目标
            RaycastHit hit;
            Ray ray = Data.playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            
            // 检测是否有物体在准星位置
            if (Physics.Raycast(ray, out hit, Data.maxTargetDistance, Data.targetLayers))
            {
                // 调试信息：显示检测到的物体
                if (Input.GetKeyDown(KeyCode.T)) // 按T键显示调试信息
                {
                    Debug.Log($"检测到物体: {hit.collider.gameObject.name} (距离: {hit.distance:F2})");
                }
                
                // 如果左键点击，选择目标
                if (Input.GetMouseButtonDown(0))
                {
                    // 如果已有选中目标，先解除选择
                    if (Data.currentTarget != null)
                    {
                        UnselectTarget();
                    }
                    
                    // 选择新目标
                    SelectTarget(hit.collider.gameObject);
                }
            }
            else
            {
                // 调试信息：显示没有检测到物体
                if (Input.GetKeyDown(KeyCode.T))
                {
                    Debug.Log($"未检测到物体 (最大距离: {Data.maxTargetDistance}, 层掩码: {Data.targetLayers.value})");
                }
            }
        }
        
        private void SelectTarget(GameObject target)
        {
            Data.currentTarget = target;
            
            // 获取目标的渲染器组件
            Data.targetRenderer = target.GetComponent<Renderer>();
            if (Data.targetRenderer != null)
            {
                // 保存原始材质和颜色
                Data.originalMaterial = Data.targetRenderer.material;
                
                // 创建新材质实例以避免修改共享材质
                Material newMaterial = new Material(Data.originalMaterial);
                Data.originalColor = newMaterial.color;
                
                // 设置为高亮颜色
                newMaterial.color = Data.targetHighlightColor;
                Data.targetRenderer.material = newMaterial;
                
                Debug.Log($"已选择目标: {target.name}");
            }
            else
            {
                // 如果目标没有渲染器，尝试在子物体中查找
                Renderer[] childRenderers = target.GetComponentsInChildren<Renderer>();
                if (childRenderers.Length > 0)
                {
                    Data.targetRenderer = childRenderers[0]; // 使用第一个子物体的渲染器
                    Data.originalMaterial = Data.targetRenderer.material;
                    
                    Material newMaterial = new Material(Data.originalMaterial);
                    Data.originalColor = newMaterial.color;
                    
                    newMaterial.color = Data.targetHighlightColor;
                    Data.targetRenderer.material = newMaterial;
                    
                    Debug.Log($"已选择目标: {target.name} (子物体渲染器)");
                }
                else
                {
                    Debug.Log($"已选择目标: {target.name} (无可变色渲染器)");
                }
            }
            
            // 触发目标选择事件
            AstronautEvents.TriggerTargetSelected(target);
        }
        
        public void UnselectTarget()
        {
            if (Data.currentTarget == null) return;
            
            // 如果正在同步，停止同步
            if (Data.isSyncing)
            {
                Data.isSyncing = false;
                AstronautEvents.TriggerSyncCancelled();
            }
            
            // 恢复原始材质和颜色
            if (Data.targetRenderer != null)
            {
                // 如果我们创建了新的材质实例，需要恢复原始颜色
                Data.targetRenderer.material.color = Data.originalColor;
                
                Debug.Log($"已取消选择目标: {Data.currentTarget.name}");
            }
            
            GameObject previousTarget = Data.currentTarget;
            Data.currentTarget = null;
            Data.targetRenderer = null;
            
            // 触发目标取消选择事件
            AstronautEvents.TriggerTargetDeselected();
        }
        
        public override void OnDestroy()
        {
            // 确保在销毁时恢复所有目标的原始材质
            if (Data.currentTarget != null && Data.targetRenderer != null)
            {
                UnselectTarget();
            }
        }
        
        public override void OnDrawGizmos()
        {
            if (Data?.playerCamera == null) return;
            
            // 绘制射线
            Ray ray = Data.playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 rayStart = ray.origin;
            Vector3 rayEnd = ray.origin + ray.direction * Data.maxTargetDistance;
            
            // 射线颜色：绿色表示正常，红色表示检测到目标
            Gizmos.color = Data.currentTarget != null ? Color.red : Color.green;
            Gizmos.DrawLine(rayStart, rayEnd);
            
            // 绘制射线起点（小圆球）
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(rayStart, 0.1f);
            
            // 绘制射线终点（小圆球）
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(rayEnd, 0.1f);
            
            // 如果检测到目标，绘制到目标的连线
            if (Data.currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(rayStart, Data.currentTarget.transform.position);
                
                // 在目标周围绘制高亮圆圈
                Gizmos.color = Data.targetHighlightColor;
                Gizmos.DrawWireSphere(Data.currentTarget.transform.position, 0.5f);
            }
        }
    }
}