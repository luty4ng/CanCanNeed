using System;
using UnityEngine;
using PlayerController.Modules;

namespace PlayerController.Modules
{
    /// <summary>
    /// 处理宇航员的移动和旋转
    /// </summary>
    [Serializable]
    [ModuleDisplayName("移动模块")]
    public class MovementModule : AstronautModuleBase
    {
        private bool wasThrusterActive;
        private const float AUTO_STOP_THRESHOLD = 0.2f; // 自动停止的速度阈值 (m/s)
        private const float AUTO_STOP_RATE = 0.95f; // 自动停止的减速率
        private Vector3 lastCameraForward; // 记录上一帧相机的前方向
        
        public override void Initialize(AstronautData data)
        {
            base.Initialize(data);
            
            // 设置刚体属性以模拟太空环境
            Data.rb.useGravity = false;
            Data.rb.drag = 0f;
            Data.rb.angularDrag = 0f;
            
            // 初始化相机前方向
            if (Data.playerCamera != null)
            {
                lastCameraForward = Data.playerCamera.transform.forward;
            }
            
            this.wasThrusterActive = false;
            this.lastCameraForward = Vector3.forward;
        }
        
        public override void OnUpdate()
        {
            // 处理旋转输入
            HandleRotation();
        }
        
        public override void OnFixedUpdate()
        {
            // 处理移动输入
            HandleMovement();
            
            // 处理低速自动停止
            HandleAutoStop();
        }
        
        private void HandleMovement()
        {
            if (Data.currentFuel <= 0) return;

            bool isUsingThrusters = Data.moveInput.magnitude > 0.1f;
            
            // 检测推进器状态变化
            if (isUsingThrusters != wasThrusterActive)
            {
                wasThrusterActive = isUsingThrusters;
                Data.isUsingThrusters = isUsingThrusters;
                AstronautEvents.TriggerThrusterStateChanged(isUsingThrusters);
            }

            if (isUsingThrusters)
            {
                // 完全基于相机方向计算移动方向
                Vector3 forward = Data.playerCamera.transform.forward;
                Vector3 right = Data.playerCamera.transform.right;
                Vector3 up = Data.playerCamera.transform.up;
                
                // 计算最终的移动方向，包括前后左右和上下移动
                Vector3 thrusterDirection = forward * Data.moveInput.z + 
                                           right * Data.moveInput.x + 
                                           up * Data.moveInput.y; // y轴用于上下移动(Shift/Ctrl)
                
                // 确保方向向量归一化
                if (thrusterDirection.magnitude > 0.1f)
                {
                    thrusterDirection.Normalize();
                }
                
                // 应用推进力
                Data.rb.AddForce(thrusterDirection * Data.thrusterForce, ForceMode.Acceleration);
                
                // 限制最大速度
                Vector3 currentVelocity = Data.rb.velocity;
                if (currentVelocity.magnitude > Data.maxVelocity)
                {
                    Data.rb.velocity = currentVelocity.normalized * Data.maxVelocity;
                }
            }
        }
        
        private void HandleAutoStop()
        {
            // 如果玩家正在使用推进器或正在同步速度，不应用自动停止
            if (Data.isUsingThrusters || Data.isSyncing) return;
            
            // 获取当前速度
            Vector3 currentVelocity = Data.rb.velocity;
            float currentSpeed = currentVelocity.magnitude;
            
            // 如果速度低于阈值，应用自动减速
            if (currentSpeed > 0 && currentSpeed < AUTO_STOP_THRESHOLD)
            {
                // 逐渐减小速度
                Vector3 newVelocity = currentVelocity * AUTO_STOP_RATE;
                
                // 如果速度非常小，直接设为零
                if (newVelocity.magnitude < 0.01f)
                {
                    newVelocity = Vector3.zero;
                }
                
                // 应用新的速度
                Data.rb.velocity = newVelocity;
            }
        }
        
        private void HandleRotation()
        {
            if (Data.currentFuel <= 0) return;

            if (!Data.isRolling)
            {
                // 检查是否在地面模式下
                bool isInGroundMode = Data.isInGravityField && Data.isOnGround;
                
                if (isInGroundMode)
                {
                    // 地面模式：只旋转相机，限制俯仰角
                    HandleGroundRotation();
                }
                else
                {
                    // 太空模式：相机和身体一起旋转，无俯仰角限制
                    HandleSpaceRotation();
                }
                
                // 更新相机前方向
                lastCameraForward = Data.playerCamera.transform.forward;
            }
            else
            {
                // 视线轴旋转模式
                if (Mathf.Abs(Data.rollInput) > 0.01f)
                {
                    // 保存相机的当前前方向
                    Vector3 cameraForward = Data.playerCamera.transform.forward;
                    
                    // 创建一个旋转，围绕相机的前方向轴
                    Quaternion rollRotation = Quaternion.AngleAxis(Data.rollInput, cameraForward);
                    
                    // 应用旋转到宇航员身体
                    Data.rb.transform.rotation = rollRotation * Data.rb.transform.rotation;
                    
                    // 确保相机仍然看向相同的方向
                    // 计算相机旋转前后的方向差异
                    Vector3 newCameraForward = Data.playerCamera.transform.forward;
                    if (Vector3.Dot(newCameraForward, lastCameraForward) < 0.999f)
                    {
                        // 如果方向有明显变化，调整相机的局部旋转以保持原来的视线方向
                        Quaternion correctionRotation = Quaternion.FromToRotation(newCameraForward, lastCameraForward);
                        Data.playerCamera.transform.rotation = correctionRotation * Data.playerCamera.transform.rotation;
                    }
                }
            }
        }
        
        private void HandleGroundRotation()
        {
            // 地面模式：只旋转相机，身体保持直立
            
            // 应用摄像机俯仰角旋转
            Vector3 currentRotation = Data.playerCamera.transform.localEulerAngles;
            float newRotationX = currentRotation.x - Data.lookInput.y;
            
            // 限制俯仰角范围
            if (newRotationX > 180f) newRotationX -= 360f;
            newRotationX = Mathf.Clamp(newRotationX, -85f, 85f);
            
            Data.playerCamera.transform.localEulerAngles = new Vector3(newRotationX, currentRotation.y, 0f);

            // 只应用水平旋转到身体
            Data.rb.transform.Rotate(Vector3.up * Data.lookInput.x);
        }
        
        private void HandleSpaceRotation()
        {
            // 太空模式：相机和身体一起旋转，无俯仰角限制
            
            // 应用水平旋转（Y轴）
            Data.rb.transform.Rotate(Vector3.up * Data.lookInput.x);
            
            // 应用垂直旋转（X轴）- 身体和相机一起旋转
            Data.rb.transform.Rotate(Vector3.right * -Data.lookInput.y);
        }
    }
}