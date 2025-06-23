﻿using System;
using UnityEngine;
using PlayerController.Modules;

namespace PlayerController.Modules
{
    /// <summary>
    /// 处理宇航员与目标的速度同步或自身稳定
    /// </summary>
    [Serializable]
    [ModuleDisplayName("速度同步模块")]
    public class SyncModule : AstronautModuleBase
    {
        private bool isStabilizing;
        public override void Initialize(AstronautData data)
        {
            base.Initialize(data);
            Data.lastSyncTime = -Data.syncCooldown;
        }
        
        public override void OnUpdate()
        {
            if (Data.isSyncRequested)
            {
                if (Data.currentTarget != null && Time.time > Data.lastSyncTime + Data.syncCooldown && !Data.isSyncing)
                {
                    StartVelocitySync();
                }
            }
            else
            {
                if (Data.isSyncing)
                {
                    Data.isSyncing = false;
                    AstronautEvents.TriggerSyncCancelled();
                }
            }
            
            if (Data.isStabilizeRequested)
            {
                if (!isStabilizing)
                {
                    StartSelfStabilization();
                }
            }
            else
            {
                if (isStabilizing)
                {
                    isStabilizing = false;
                    AstronautEvents.TriggerStabilizationCancelled();
                }
            }
        }
        
        public override void OnFixedUpdate()
        {
            if (Data.isSyncing && Data.currentFuel > 0)
            {
                SyncVelocityWithTarget();
            }
            
            if (isStabilizing && Data.currentFuel > 0)
            {
                StabilizeRotation();
            }
        }
        
        private void StartVelocitySync()
        {
            if (Data.currentTarget == null || Data.currentFuel <= 0) return;
            
            Rigidbody targetRb = Data.currentTarget.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                Data.targetVelocity = targetRb.velocity;
                
                Data.isSyncing = true;
                
                Data.lastSyncTime = Time.time;
                
                AstronautEvents.TriggerSyncStarted(Data.targetVelocity);
                
                Debug.Log($"开始同步速度，目标速度: {Data.targetVelocity.magnitude:F2} m/s");
            }
        }
        
        private void StartSelfStabilization()
        {
            if (Data.currentFuel <= 0) 
                return;
            
            isStabilizing = true;
            Debug.Log("开始稳定旋转状态");
        }
        
        private void StabilizeRotation()
        {
            if (!isStabilizing || Data.currentFuel <= 0) return;
            
            if (Data.rb.angularVelocity.magnitude > 0.01f)
            {
                Data.rb.angularVelocity = Vector3.Lerp(
                    Data.rb.angularVelocity, 
                    Vector3.zero, 
                    Data.syncSpeed * Time.fixedDeltaTime
                );
            }
            else
            {
                Data.rb.angularVelocity = Vector3.zero;
                
                isStabilizing = false;
                
                Debug.Log("旋转稳定完成!");
            }
        }
        
        private void SyncVelocityWithTarget()
        {
            if (!Data.isSyncing || Data.currentTarget == null) return;
            
            Rigidbody targetRb = Data.currentTarget.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                Data.targetVelocity = targetRb.velocity;
            }
            
            Vector3 velocityDiff = Data.targetVelocity - Data.rb.velocity;
            
            Vector3 adjustmentVelocity = velocityDiff * Data.syncSpeed * Time.fixedDeltaTime;
            
            Data.rb.velocity += adjustmentVelocity;
            
            if (Data.rb.angularVelocity.magnitude > 0.01f)
            {
                Data.rb.angularVelocity = Vector3.Lerp(
                    Data.rb.angularVelocity, 
                    Vector3.zero, 
                    Data.syncSpeed * Time.fixedDeltaTime
                );
            }
            else if (Data.rb.angularVelocity.magnitude <= 0.01f)
            {
                Data.rb.angularVelocity = Vector3.zero;
            }
            
            if (velocityDiff.magnitude < 0.1f)
            {
                Data.rb.velocity = Data.targetVelocity;
                
                AstronautEvents.TriggerSyncCompleted();
                
                Debug.Log("速度同步完成!");
            }
        }
    }
}