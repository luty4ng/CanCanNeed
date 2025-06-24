using System;
using UnityEngine;
using PlayerController.Modules;

namespace PlayerController.Modules
{
    /// <summary>
    /// 处理宇航员的目标选择和管理模块
    /// </summary>
    [Serializable]
    [ModuleDisplayName("目标选择模块")]
    public class TargetingModule : AstronautModuleBase
    {
        #region 私有字段
        private bool m_IsTargetSelected;
        private float m_LastTargetCheckTime;
        private const float TargetCheckInterval = 0.05f; // 目标检测间隔，避免过于频繁的射线检测
        #endregion

        #region 自动定向相关字段
        private bool m_IsAutoAiming = false;
        private float m_AutoAimSpeed = 5f; // 可调节对准速度
        #endregion

        #region 自动巡航相关字段
        private bool m_IsAutoCruising = false;
        private Vector3 m_AutoCruiseTargetPosition;
        private Vector3 m_AutoCruiseDirection;
        private float m_AutoCruiseCurrentSpeed;
        private bool m_HasSyncedVelocity = false;
        private bool m_HasAlignedDirection = false;
        private bool m_IsAccelerating = false;
        private bool m_IsDecelerating = false;
        #endregion

        #region Unity生命周期方法
        /// <summary>
        /// 每帧更新目标检测和选择逻辑
        /// </summary>
        public override void OnUpdate()
        {
            if (!Enabled || Data?.playerCamera == null) return;

            HandleTargeting();
            
            if (Input.GetMouseButtonDown(1) && Data.currentTarget != null)
            {
                UnselectTarget();
            }

            // 自动定向逻辑
            if (Input.GetKeyDown(KeyCode.F) && Data.currentTarget != null)
            {
                m_IsAutoAiming = true;
            }
            if (m_IsAutoAiming)
            {
                AutoAimToTarget();
            }

            // 自动巡航逻辑
            HandleAutoCruise();
        }

        /// <summary>
        /// 清理模块资源，确保恢复目标原始材质
        /// </summary>
        public override void OnDestroy()
        {
            if (Data?.currentTarget != null && Data.targetRenderer != null)
            {
                UnselectTarget();
            }
        }

        /// <summary>
        /// 绘制调试Gizmos，可视化射线检测和目标状态
        /// </summary>
        public override void OnDrawGizmos()
        {
            if (Data?.playerCamera == null) return;

            DrawTargetingGizmos();
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 取消选择当前目标
        /// </summary>
        public void UnselectTarget()
        {
            m_IsAutoAiming = false; // 取消自动对准
            
            if (Data?.currentTarget == null) return;

            if (Data.isSyncing)
            {
                Data.isSyncing = false;
                AstronautEvents.TriggerSyncCancelled();
            }

            // 停止自动巡航
            if (Data.isAutoCruising)
            {
                Data.isAutoCruising = false;
                StopAutoCruise();
            }

            RestoreTargetMaterial();

            GameObject previousTarget = Data.currentTarget;
            ClearTargetData();

            AstronautEvents.TriggerTargetDeselected();

            Debug.Log($"已取消选择目标: {previousTarget.name}");

            m_IsAutoAiming = false; // 取消自动对准
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 处理目标检测和选择逻辑
        /// </summary>
        private void HandleTargeting()
        {
            if (Time.time - m_LastTargetCheckTime < TargetCheckInterval)
            {
                return;
            }
            m_LastTargetCheckTime = Time.time;

            if (TryDetectTarget(out RaycastHit hit))
            {
                HandleTargetDetection(hit);
            }
            else
            {
                HandleNoTargetDetection();
            }
        }

        /// <summary>
        /// 尝试检测目标
        /// </summary>
        /// <param name="hit">射线检测结果</param>
        /// <returns>是否检测到目标</returns>
        private bool TryDetectTarget(out RaycastHit hit)
        {
            hit = default;
            if (Data.playerCamera == null) return false;

            Ray ray = Data.playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            return Physics.Raycast(ray, out hit, Data.maxTargetDistance, Data.targetLayers);
        }

        /// <summary>
        /// 处理检测到目标的情况
        /// </summary>
        /// <param name="hit">射线检测结果</param>
        private void HandleTargetDetection(RaycastHit hit)
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                Debug.Log($"检测到物体: {hit.collider.gameObject.name} (距离: {hit.distance:F2})");
            }

            if (Input.GetMouseButtonDown(0))
            {
                SelectTarget(hit.collider.gameObject);
            }
        }

        /// <summary>
        /// 处理未检测到目标的情况
        /// </summary>
        private void HandleNoTargetDetection()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                Debug.Log($"未检测到物体 (最大距离: {Data.maxTargetDistance}, 层掩码: {Data.targetLayers.value})");
            }
        }

        /// <summary>
        /// 选择指定目标
        /// </summary>
        /// <param name="target">要选择的目标游戏对象</param>
        private void SelectTarget(GameObject target)
        {
            if (target == null) return;

            if (Data.currentTarget != null)
            {
                UnselectTarget();
            }

            Data.currentTarget = target;
            m_IsTargetSelected = true;

            ApplyTargetHighlight();

            AstronautEvents.TriggerTargetSelected(target);

            Debug.Log($"已选择目标: {target.name}");
        }

        /// <summary>
        /// 应用目标高亮效果
        /// </summary>
        private void ApplyTargetHighlight()
        {
            if (Data.currentTarget == null) return;

            Data.targetRenderer = Data.currentTarget.GetComponent<Renderer>();
            
            if (Data.targetRenderer != null)
            {
                ApplyRendererHighlight(Data.targetRenderer);
            }
            else
            {
                ApplyChildRendererHighlight();
            }
        }

        /// <summary>
        /// 为指定渲染器应用高亮效果
        /// </summary>
        /// <param name="renderer">要高亮的渲染器</param>
        private void ApplyRendererHighlight(Renderer renderer)
        {
            if (renderer == null) return;

            Data.originalMaterial = renderer.material;
            
            Material newMaterial = new Material(Data.originalMaterial);
            Data.originalColor = newMaterial.color;
            
            newMaterial.color = Data.targetHighlightColor;
            renderer.material = newMaterial;
        }

        /// <summary>
        /// 为子物体渲染器应用高亮效果
        /// </summary>
        private void ApplyChildRendererHighlight()
        {
            Renderer[] childRenderers = Data.currentTarget.GetComponentsInChildren<Renderer>();
            if (childRenderers.Length > 0)
            {
                Data.targetRenderer = childRenderers[0];
                ApplyRendererHighlight(Data.targetRenderer);
                Debug.Log($"已选择目标: {Data.currentTarget.name} (子物体渲染器)");
            }
            else
            {
                Debug.Log($"已选择目标: {Data.currentTarget.name} (无可变色渲染器)");
            }
        }

        /// <summary>
        /// 恢复目标原始材质
        /// </summary>
        private void RestoreTargetMaterial()
        {
            if (Data.targetRenderer != null)
            {
                Data.targetRenderer.material.color = Data.originalColor;
            }
        }

        /// <summary>
        /// 清除目标相关数据
        /// </summary>
        private void ClearTargetData()
        {
            Data.currentTarget = null;
            Data.targetRenderer = null;
            m_IsTargetSelected = false;
        }

        /// <summary>
        /// 绘制目标选择相关的调试Gizmos
        /// </summary>
        private void DrawTargetingGizmos()
        {
            Ray ray = Data.playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 rayStart = ray.origin;
            Vector3 rayEnd = ray.origin + ray.direction * Data.maxTargetDistance;

            Gizmos.color = Data.currentTarget != null ? Color.red : Color.green;
            Gizmos.DrawLine(rayStart, rayEnd);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(rayStart, 0.1f);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(rayEnd, 0.1f);

            if (Data.currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(rayStart, Data.currentTarget.transform.position);

                Gizmos.color = Data.targetHighlightColor;
                Gizmos.DrawWireSphere(Data.currentTarget.transform.position, 0.5f);
            }
        }

        /// <summary>
        /// 自动将相机缓慢对准已选中的目标
        /// </summary>
        private void AutoAimToTarget()
        {
            if (Data.currentTarget == null || Data.playerCamera == null)
            {
                m_IsAutoAiming = false;
                return;
            }

            Transform camTransform = Data.playerCamera.transform;
            Vector3 targetPos = Data.currentTarget.transform.position;
            Vector3 dirToTarget = (targetPos - camTransform.position).normalized;
            if (dirToTarget.sqrMagnitude < 0.0001f)
            {
                m_IsAutoAiming = false;
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(dirToTarget, Vector3.up);
            camTransform.rotation = Quaternion.RotateTowards(
                camTransform.rotation,
                targetRotation,
                m_AutoAimSpeed * Time.deltaTime * 100f // 速度可调
            );

            // 判断是否已基本对准
            float angle = Quaternion.Angle(camTransform.rotation, targetRotation);
            if (angle < 0.5f)
            {
                camTransform.rotation = targetRotation;
                m_IsAutoAiming = false;
            }
        }

        /// <summary>
        /// 处理自动巡航逻辑
        /// </summary>
        private void HandleAutoCruise()
        {
            // 检查是否应该开始或停止自动巡航
            if (Data.isAutoCruising && !m_IsAutoCruising)
            {
                StartAutoCruise();
            }
            else if (!Data.isAutoCruising && m_IsAutoCruising)
            {
                StopAutoCruise();
            }

            // 执行自动巡航逻辑
            if (m_IsAutoCruising)
            {
                ExecuteAutoCruise();
            }
        }

        /// <summary>
        /// 开始自动巡航
        /// </summary>
        private void StartAutoCruise()
        {
            if (Data.currentTarget == null) return;

            m_IsAutoCruising = true;
            Data.isAutoCruising = true; // 确保Data状态同步
            m_HasSyncedVelocity = false;
            m_HasAlignedDirection = false;
            m_IsAccelerating = false;
            m_IsDecelerating = false;
            m_AutoCruiseCurrentSpeed = 0f;
            Data.autoCruiseCurrentSpeed = 0f; // 同步到Data

            // 计算目标位置（在目标前方保持指定距离）
            Vector3 targetPos = Data.currentTarget.transform.position;
            Vector3 directionToTarget = (targetPos - Data.transform.position).normalized;
            m_AutoCruiseTargetPosition = targetPos - directionToTarget * Data.autoCruiseDistance;
            m_AutoCruiseDirection = directionToTarget;
            Data.autoCruiseTargetPosition = m_AutoCruiseTargetPosition; // 同步到Data
            Data.autoCruiseDirection = m_AutoCruiseDirection; // 同步到Data

            AstronautEvents.TriggerAutoCruiseStarted(Data.currentTarget);
            Debug.Log($"开始自动巡航到目标: {Data.currentTarget.name}");
        }

        /// <summary>
        /// 停止自动巡航
        /// </summary>
        private void StopAutoCruise()
        {
            m_IsAutoCruising = false;
            Data.isAutoCruising = false; // 确保Data状态同步
            m_HasSyncedVelocity = false;
            m_HasAlignedDirection = false;
            m_IsAccelerating = false;
            m_IsDecelerating = false;
            m_AutoCruiseCurrentSpeed = 0f;
            Data.autoCruiseCurrentSpeed = 0f; // 同步到Data

            AstronautEvents.TriggerAutoCruiseStopped();
            Debug.Log("停止自动巡航");
        }

        /// <summary>
        /// 执行自动巡航
        /// </summary>
        private void ExecuteAutoCruise()
        {
            if (Data.currentTarget == null)
            {
                StopAutoCruise();
                return;
            }

            // 更新目标位置（跟随目标移动）
            Vector3 targetPos = Data.currentTarget.transform.position;
            Vector3 directionToTarget = (targetPos - Data.transform.position).normalized;
            m_AutoCruiseTargetPosition = targetPos - directionToTarget * Data.autoCruiseDistance;

            // 阶段1：速度同步
            if (!m_HasSyncedVelocity)
            {
                SyncVelocityWithTarget();
            }
            // 阶段2：方向对准
            else if (!m_HasAlignedDirection)
            {
                AlignDirectionToTarget();
            }
            // 阶段3：加速前进
            else if (!m_IsAccelerating && !m_IsDecelerating)
            {
                AccelerateToTarget();
            }
            // 阶段4：减速停止
            else if (m_IsAccelerating)
            {
                DecelerateToTarget();
            }
        }

        /// <summary>
        /// 与目标同步速度
        /// </summary>
        private void SyncVelocityWithTarget()
        {
            if (Data.currentTarget == null) return;

            Rigidbody targetRb = Data.currentTarget.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                Vector3 velocityDiff = targetRb.velocity - Data.rb.velocity;
                Vector3 adjustmentVelocity = velocityDiff * Data.syncSpeed * Time.deltaTime;
                Data.rb.velocity += adjustmentVelocity;

                if (velocityDiff.magnitude < 0.1f)
                {
                    Data.rb.velocity = targetRb.velocity;
                    m_HasSyncedVelocity = true;
                    Debug.Log("速度同步完成，开始方向对准");
                }
            }
            else
            {
                // 如果目标没有Rigidbody，直接进入下一阶段
                m_HasSyncedVelocity = true;
            }
        }

        /// <summary>
        /// 对准目标方向
        /// </summary>
        private void AlignDirectionToTarget()
        {
            Vector3 directionToTarget = (m_AutoCruiseTargetPosition - Data.transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, Vector3.up);
            
            Data.transform.rotation = Quaternion.RotateTowards(
                Data.transform.rotation,
                targetRotation,
                Data.autoCruiseRotationSpeed * Time.deltaTime * 100f
            );

            float angle = Quaternion.Angle(Data.transform.rotation, targetRotation);
            if (angle < 1f)
            {
                Data.transform.rotation = targetRotation;
                m_HasAlignedDirection = true;
                m_IsAccelerating = true;
                Debug.Log("方向对准完成，开始加速");
            }
        }

        /// <summary>
        /// 加速向目标前进
        /// </summary>
        private void AccelerateToTarget()
        {
            Vector3 directionToTarget = (m_AutoCruiseTargetPosition - Data.transform.position).normalized;
            
            // 计算到目标的距离
            float distanceToTarget = Vector3.Distance(Data.transform.position, m_AutoCruiseTargetPosition);
            
            // 根据距离决定是否开始减速
            float decelerationDistance = (m_AutoCruiseCurrentSpeed * m_AutoCruiseCurrentSpeed) / (2f * Data.autoCruiseDeceleration);
            
            if (distanceToTarget <= decelerationDistance)
            {
                m_IsAccelerating = false;
                m_IsDecelerating = true;
                Debug.Log("开始减速");
                return;
            }

            // 加速
            m_AutoCruiseCurrentSpeed += Data.autoCruiseAcceleration * Time.deltaTime;
            m_AutoCruiseCurrentSpeed = Mathf.Min(m_AutoCruiseCurrentSpeed, Data.autoCruiseSpeed);
            Data.autoCruiseCurrentSpeed = m_AutoCruiseCurrentSpeed; // 同步到Data
            
            // 应用速度
            Data.rb.velocity = directionToTarget * m_AutoCruiseCurrentSpeed;
        }

        /// <summary>
        /// 减速到目标位置
        /// </summary>
        private void DecelerateToTarget()
        {
            Vector3 directionToTarget = (m_AutoCruiseTargetPosition - Data.transform.position).normalized;
            float distanceToTarget = Vector3.Distance(Data.transform.position, m_AutoCruiseTargetPosition);
            
            // 减速
            m_AutoCruiseCurrentSpeed -= Data.autoCruiseDeceleration * Time.deltaTime;
            m_AutoCruiseCurrentSpeed = Mathf.Max(m_AutoCruiseCurrentSpeed, 0f);
            Data.autoCruiseCurrentSpeed = m_AutoCruiseCurrentSpeed; // 同步到Data
            
            // 应用速度
            Data.rb.velocity = directionToTarget * m_AutoCruiseCurrentSpeed;
            
            // 检查是否到达目标位置
            if (distanceToTarget < 0.5f && m_AutoCruiseCurrentSpeed < 0.1f)
            {
                Data.rb.velocity = Vector3.zero;
                m_IsDecelerating = false;
                m_IsAccelerating = false;
                Debug.Log("自动巡航完成，已到达目标位置");
            }
        }
        #endregion
    }
}