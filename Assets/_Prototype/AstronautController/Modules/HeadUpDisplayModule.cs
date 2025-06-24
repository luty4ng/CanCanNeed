using System;
using UnityEngine;

namespace PlayerController.Modules
{
    /// <summary>
    /// 处理宇航员的UI显示
    /// </summary>
    [Serializable]
    [ModuleDisplayName("UI模块")]
    public class HeadUpDisplayModule : AstronautModuleBase
    {
        public override void OnGUI()
        {
            // 显示燃料信息
            GUI.Label(new Rect(10, 10, 200, 20), $"燃料: {Data.currentFuel:F1}%");
            GUI.Label(new Rect(10, 30, 200, 20), $"速度: {Data.rb.velocity.magnitude:F2} m/s");
            
            // 显示控制模式
            if (Data.isRolling)
            {
                GUI.color = Color.green;
                GUI.Label(new Rect(10, Screen.height - 40, 300, 20), "视线轴旋转模式 (R)");
            }
            
            // 显示准星
            float crosshairSize = 10;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(Screen.width / 2 - crosshairSize / 2, Screen.height / 2 - crosshairSize / 2, 
                crosshairSize, crosshairSize), Texture2D.whiteTexture);
            
            // 显示目标信息
            if (Data.currentTarget != null)
            {
                GUI.color = Data.targetHighlightColor;
                GUI.Label(new Rect(10, 50, 300, 20), $"目标: {Data.currentTarget.name}");
                
                // 如果目标有Rigidbody，显示其速度
                Rigidbody targetRb = Data.currentTarget.GetComponent<Rigidbody>();
                if (targetRb != null)
                {
                    GUI.Label(new Rect(10, 70, 300, 20), $"目标速度: {targetRb.velocity.magnitude:F2} m/s");
                    
                    // 如果正在同步，显示同步进度
                    if (Data.isSyncing)
                    {
                        float syncProgress = 1f - ((targetRb.velocity - Data.rb.velocity).magnitude / 
                                                   Mathf.Max(0.1f, targetRb.velocity.magnitude));
                        syncProgress = Mathf.Clamp01(syncProgress) * 100f;
                        
                        GUI.Label(new Rect(10, 90, 300, 20), $"同步进度: {syncProgress:F1}%");
                        GUI.Label(new Rect(10, 110, 300, 20), "正在同步速度...");
                    }
                    else
                    {
                        // 显示同步冷却
                        float cooldownRemaining = Mathf.Max(0, (Data.lastSyncTime + Data.syncCooldown) - Time.time);
                        if (cooldownRemaining > 0)
                        {
                            GUI.Label(new Rect(10, 90, 300, 20), $"同步冷却: {cooldownRemaining:F1}s");
                        }
                        else
                        {
                            GUI.Label(new Rect(10, 90, 300, 20), "按住空格同步速度");
                        }
                    }
                }
                
                GUI.Label(new Rect(10, 130, 300, 20), "右键点击取消选择");
                
                // 显示自动巡航状态
                if (Data.isAutoCruising)
                {
                    GUI.color = Color.cyan;
                    GUI.Label(new Rect(10, 150, 300, 20), "自动巡航中...");
                    GUI.Label(new Rect(10, 170, 300, 20), $"巡航速度: {Data.autoCruiseCurrentSpeed:F2} m/s");
                    GUI.Label(new Rect(10, 190, 300, 20), "按X键停止自动巡航");
                }
                else
                {
                    GUI.color = Color.white;
                    GUI.Label(new Rect(10, 150, 300, 20), "按X键开始自动巡航");
                }
            }
            else
            {
                GUI.color = Color.white;
                GUI.Label(new Rect(10, 50, 300, 20), "左键点击选择目标");
            }
            
            // 显示控制提示
            GUI.color = Color.white;
            GUI.Label(new Rect(10, Screen.height - 100, 300, 20), "WASD: 移动");
            GUI.Label(new Rect(10, Screen.height - 80, 300, 20), "Shift: 上升 | Ctrl: 下降");
            GUI.Label(new Rect(10, Screen.height - 60, 300, 20), "R+鼠标: 视线轴旋转");
            GUI.Label(new Rect(10, Screen.height - 40, 300, 20), "F: 自动对准 | X: 自动巡航");
        }
    }
}