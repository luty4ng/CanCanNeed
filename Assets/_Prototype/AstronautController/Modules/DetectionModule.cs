using System;
using UnityEngine;

namespace PlayerController.Modules.Gravity
{
    /// <summary>
    /// 重力检测组件 - 仅被动响应重力环境的变更
    /// </summary>
    [Serializable]
    [ModuleDisplayName("重力检测模块")]
    public class DetectionModule : AstronautModuleBase
    {
        private bool m_WasInGravityField;
        private bool m_WasOnGround;

        /// <summary>
        /// 初始化检测模块
        /// </summary>
        /// <param name="data">宇航员数据</param>
        public override void Initialize(AstronautData data)
        {
            base.Initialize(data);
            m_WasInGravityField = false;
            m_WasOnGround = false;
            GravityPlate.OnPlayerEnterPlate += HandlePlateEnter;
            GravityPlate.OnPlayerExitPlate += HandlePlateExit;
        }

        /// <summary>
        /// 供环境对象（如GravityPlate/星球）调用，设置重力状态
        /// </summary>
        public void SetGravity(UnityEngine.Object source, Vector3 direction, float strength)
        {
            Data.isInGravityField = true;
            Data.gravityDirection = direction;
            Data.gravityStrength = strength;
            Data.gravitySource = source != null ? (source as Component)?.transform : null;
        }

        /// <summary>
        /// 供环境对象调用，清除重力状态
        /// </summary>
        public void ClearGravity(UnityEngine.Object source)
        {
            Data.isInGravityField = false;
            Data.gravityDirection = Vector3.down;
            Data.gravityStrength = 0f;
            Data.gravitySource = null;
        }

        /// <summary>
        /// 供GravityPlate事件调用：玩家进入重力板区域
        /// </summary>
        public void EnterPlateGravity(GravityPlate plate)
        {
            if (plate != null && plate.IsActive)
            {
                SetGravity(plate, plate.GravityDirection, plate.GravityStrength);
            }
        }

        /// <summary>
        /// 供GravityPlate事件调用：玩家离开重力板区域
        /// </summary>
        public void ExitPlateGravity(GravityPlate plate)
        {
            if (plate != null && plate.IsActive)
            {
                ClearGravity(plate);
            }
        }

        private void HandlePlateEnter(Collider other, GravityPlate plate)
        {
            EnterPlateGravity(plate);
        }

        private void HandlePlateExit(Collider other, GravityPlate plate)
        {
            ExitPlateGravity(plate);
        }

        public override void OnDestroy()
        {
            GravityPlate.OnPlayerEnterPlate -= HandlePlateEnter;
            GravityPlate.OnPlayerExitPlate -= HandlePlateExit;
        }

        /// <summary>
        /// 每帧更新检测
        /// </summary>
        public override void OnUpdate()
        {
            UpdateGroundDetection();
            UpdateStateEvents();
        }

        /// <summary>
        /// 检测地面
        /// </summary>
        private (bool, Transform, Vector3, float) DetectGround()
        {
            bool isOnGround = false;
            Transform groundSurface = null;
            Vector3 groundNormal = Vector3.up;
            float groundDistance = float.MaxValue;

            Vector3 rayStart = Data.transform.position;
            Vector3 rayDir = Data.isInGravityField ? Data.gravityDirection : Vector3.down;

            if (Physics.Raycast(rayStart, rayDir, out RaycastHit hit, Data.groundCheckDistance, Data.groundLayers))
            {
                isOnGround = true;
                groundSurface = hit.transform;
                groundNormal = hit.normal;
                groundDistance = hit.distance;
            }

            return (isOnGround, groundSurface, groundNormal, groundDistance);
        }

        private void UpdateGroundDetection()
        {
            var (onGround, surface, normal, distance) = DetectGround();
            Data.isOnGround = onGround;
            Data.groundSurface = surface;
            Data.groundNormal = normal;
            Data.groundDistance = distance;
        }

        private void UpdateStateEvents()
        {
            HandleGravityFieldStateChange();
            HandleGroundStateChange();
        }

        private void HandleGravityFieldStateChange()
        {
            if (Data.isInGravityField != m_WasInGravityField)
            {
                m_WasInGravityField = Data.isInGravityField;
                if (Data.isInGravityField)
                {
                    AstronautEvents.TriggerEnterGravityField(Data.gravityDirection, Data.gravityStrength);
                }
                else
                {
                    AstronautEvents.TriggerExitGravityField();
                }
            }
        }

        private void HandleGroundStateChange()
        {
            if (Data.isOnGround != m_WasOnGround)
            {
                m_WasOnGround = Data.isOnGround;
                if (Data.isOnGround)
                {
                    AstronautEvents.TriggerLandOnGround(Data.groundSurface);
                }
                else
                {
                    AstronautEvents.TriggerLeaveGround();
                }
            }
        }

        /// <summary>
        /// 编辑器下可视化辅助
        /// </summary>
        public override void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // 绘制重力检测范围（仅供调试，实际重力由环境驱动）
            Gizmos.color = Data.isInGravityField ? Color.green : Color.red;
            Gizmos.DrawWireSphere(Data.transform.position, 1.5f);

            // 绘制重力方向
            if (Data.isInGravityField)
            {
                Gizmos.color = Color.yellow;
                Vector3 start = Data.transform.position;
                Vector3 end = start + Data.gravityDirection * 2f;
                Gizmos.DrawLine(start, end);
                Gizmos.DrawSphere(end, 0.1f);
            }

            // 绘制地面检测
            Gizmos.color = Data.isOnGround ? Color.blue : Color.gray;
            Vector3 rayStart = Data.transform.position;
            Vector3 rayDir = Data.isInGravityField ? Data.gravityDirection : Vector3.down;
            Gizmos.DrawRay(rayStart, rayDir * Data.groundCheckDistance);
        }
    }
} 