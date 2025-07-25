﻿using System;
using UnityEngine;

namespace PlayerController.Modules
{
    /// <summary>
    /// 处理宇航员的输入控制
    /// </summary>
    [Serializable]
    [ModuleDisplayName("输入模块")]
    public class InputModule : AstronautModuleBase
    {
        private bool previousRollingState;
        public override void Initialize(AstronautData data)
        {
            base.Initialize(data);
            Cursor.lockState = CursorLockMode.Locked;
            previousRollingState = false;
        }
        
        public override void OnUpdate()
        {
            // 获取移动输入 (使用Shift键上升，Ctrl键下降)
            Data.moveInput = new Vector3(
                Input.GetAxis("Horizontal"),
                (Input.GetKey(KeyCode.LeftShift) ? 1 : 0) - (Input.GetKey(KeyCode.LeftControl) ? 1 : 0),
                Input.GetAxis("Vertical")
            );
            
            // 检测是否按下R键（视线轴旋转模式）
            bool isRolling = Input.GetKey(KeyCode.R);
            
            // 检测视线轴旋转状态变化
            if (isRolling != previousRollingState)
            {
                previousRollingState = isRolling;
                Data.isRolling = isRolling;
                AstronautEvents.TriggerRollingStateChanged(isRolling);
            }
            
            // 获取鼠标输入
            if (!Data.isRolling)
            {
                // 正常视角控制
                Data.lookInput = new Vector2(
                    Input.GetAxis("Mouse X") * Data.rotationSpeed * Time.deltaTime,
                    Input.GetAxis("Mouse Y") * Data.rotationSpeed * Time.deltaTime
                );
                Data.rollInput = 0f;
            }
            else
            {
                // 视线轴旋转模式
                Data.rollInput = Input.GetAxis("Mouse X") * Data.rollSpeed * Time.deltaTime;
                Data.lookInput = Vector2.zero;
            }
            
            // 检测同步请求 (空格键用于同步速度)
            Data.isSyncRequested = Input.GetKey(KeyCode.Space) && Data.currentTarget != null;
            Data.isStabilizeRequested = Input.GetKey(KeyCode.Space) && !Data.isOnGround;
            Data.isJumpingRequested = Input.GetKey(KeyCode.Space) && Data.isOnGround && Data.isInGravityField;
            
            // 检测自动巡航请求 (X键用于自动巡航)
            if (Input.GetKeyDown(KeyCode.X) && Data.currentTarget != null)
            {
                Data.isAutoCruising = !Data.isAutoCruising;
                if (Data.isAutoCruising)
                {
                    Debug.Log("开始自动巡航");
                }
                else
                {
                    Debug.Log("停止自动巡航");
                }
            }

            // --- 库存相关输入 ---
            // G键扔下物品
            Data.isDropItemRequested = Input.GetKeyDown(KeyCode.G);
            // 左键蓄力扔出
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                Data.isThrowItemCharging = true;
                Data.throwItemChargeTime = 0f;
                Data.isThrowItemRequested = false;
            }
            if (Input.GetKey(KeyCode.Mouse0) && Data.isThrowItemCharging)
            {
                Data.throwItemChargeTime += Time.deltaTime;
            }
            if (Input.GetKeyUp(KeyCode.Mouse0) && Data.isThrowItemCharging)
            {
                Data.isThrowItemCharging = false;
                Data.isThrowItemRequested = true;
            }
        }
        
        public override void OnDestroy()
        {
            // 解锁鼠标
            Cursor.lockState = CursorLockMode.None;
        }
    }
}