using System;
using UnityEngine;

namespace PlayerController.Modules.Gravity
{
    /// <summary>
    /// 重力物理组件 - 处理重力应用和地面物理
    /// </summary>
    [Serializable]
    [ModuleDisplayName("重力物理模块")]
    public class GravityPhysicsModule : AstronautModuleBase
    {
        private float m_CurrentGravityStrength;
        private Vector3 m_CurrentGravityDirection;
        private float m_GravityTransitionTimer;
        private bool m_IsTransitioningGravity;
        private bool m_WasInGravityField;

        /// <summary>
        /// 初始化重力物理模块
        /// </summary>
        /// <param name="data">宇航员数据</param>
        public override void Initialize(AstronautData data)
        {
            base.Initialize(data);
            m_CurrentGravityStrength = 0f;
            m_CurrentGravityDirection = Vector3.down;
            m_GravityTransitionTimer = 0f;
            m_IsTransitioningGravity = false;
            m_WasInGravityField = false;
            ApplySpacePhysics();
        }

        /// <summary>
        /// 固定帧更新，处理重力和物理属性
        /// </summary>
        public override void OnFixedUpdate()
        {
            ApplyGravityForce();
            UpdatePhysicsProperties();
            UpdateGravityTransition();
        }

        /// <summary>
        /// 应用重力
        /// </summary>
        private void ApplyGravityForce()
        {
            if (Data.isInGravityField)
            {
                Vector3 gravityForce = Data.gravityDirection * Data.gravityStrength;
                Data.rb.AddForce(gravityForce, ForceMode.Acceleration);
                m_CurrentGravityStrength = Data.gravityStrength;
                m_CurrentGravityDirection = Data.gravityDirection;
            }
            else
            {
                m_CurrentGravityStrength = 0f;
                m_CurrentGravityDirection = Vector3.down;
            }
        }

        /// <summary>
        /// 更新物理属性（地面、空中、太空）
        /// </summary>
        private void UpdatePhysicsProperties()
        {
            if (Data.isInGravityField)
            {
                if (Data.isOnGround)
                {
                    ApplyGroundPhysics();
                }
                else
                {
                    ApplyAirPhysics();
                }
            }
            else
            {
                ApplySpacePhysics();
            }
        }

        /// <summary>
        /// 应用地面物理属性
        /// </summary>
        private void ApplyGroundPhysics()
        {
            Data.rb.drag = Data.groundDrag;
            Data.rb.angularDrag = Data.groundAngularDrag;
            Data.rb.useGravity = false;
            LimitVerticalVelocityOnGround();
        }

        /// <summary>
        /// 限制地面上的垂直速度，防止穿透地面
        /// </summary>
        private void LimitVerticalVelocityOnGround()
        {
            Vector3 velocity = Data.rb.velocity;
            Vector3 gravityDir = Data.gravityDirection;
            float verticalSpeed = Vector3.Dot(velocity, gravityDir);
            Vector3 horizontalVelocity = velocity - gravityDir * verticalSpeed;
            if (verticalSpeed < 0 && Data.isOnGround)
            {
                Data.rb.velocity = horizontalVelocity;
            }
        }

        /// <summary>
        /// 应用空中物理属性
        /// </summary>
        private void ApplyAirPhysics()
        {
            Data.rb.drag = Data.airDrag;
            Data.rb.angularDrag = Data.airAngularDrag;
            Data.rb.useGravity = false;
        }

        /// <summary>
        /// 应用太空物理属性
        /// </summary>
        private void ApplySpacePhysics()
        {
            Data.rb.drag = 0f;
            Data.rb.angularDrag = 0f;
            Data.rb.useGravity = false;
        }

        /// <summary>
        /// 更新重力过渡状态
        /// </summary>
        private void UpdateGravityTransition()
        {
            if (Data.isInGravityField != m_WasInGravityField)
            {
                if (Data.isInGravityField)
                {
                    StartGravityTransition();
                }
                else
                {
                    EndGravityTransition();
                }
                m_WasInGravityField = Data.isInGravityField;
            }
            
            if (m_IsTransitioningGravity)
            {
                m_GravityTransitionTimer += Time.fixedDeltaTime;
                float progress = m_GravityTransitionTimer / Data.gravityTransitionTime;
                if (progress >= 1f)
                {
                    EndGravityTransition();
                }
                else
                {
                    SmoothGravityTransition(progress);
                }
            }
        }

        /// <summary>
        /// 开始重力过渡
        /// </summary>
        private void StartGravityTransition()
        {
            m_IsTransitioningGravity = true;
            m_GravityTransitionTimer = 0f;
            AstronautEvents.TriggerGravityTransitionStarted();
        }

        /// <summary>
        /// 结束重力过渡
        /// </summary>
        private void EndGravityTransition()
        {
            m_IsTransitioningGravity = false;
            m_GravityTransitionTimer = 0f;
            AstronautEvents.TriggerGravityTransitionCompleted();
        }

        /// <summary>
        /// 平滑重力过渡
        /// </summary>
        /// <param name="progress">过渡进度(0-1)</param>
        private void SmoothGravityTransition(float progress)
        {
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            if (Data.isInGravityField)
            {
                Data.rb.drag = Mathf.Lerp(0f, Data.airDrag, smoothProgress);
                Data.rb.angularDrag = Mathf.Lerp(0f, Data.airAngularDrag, smoothProgress);
            }
            else
            {
                Data.rb.drag = Mathf.Lerp(Data.airDrag, 0f, smoothProgress);
                Data.rb.angularDrag = Mathf.Lerp(Data.airAngularDrag, 0f, smoothProgress);
            }
        }

        /// <summary>
        /// 编辑器下可视化辅助
        /// </summary>
        public override void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            DrawGravityVectorGizmo();
            DrawGroundNormalGizmo();
        }

        /// <summary>
        /// 绘制重力向量Gizmo
        /// </summary>
        private void DrawGravityVectorGizmo()
        {
            if (Data.isInGravityField && m_CurrentGravityStrength > 0.1f)
            {
                Gizmos.color = Color.red;
                Vector3 start = Data.rb.transform.position;
                Vector3 end = start + m_CurrentGravityDirection * m_CurrentGravityStrength;
                Gizmos.DrawLine(start, end);
                float strengthIndicator = m_CurrentGravityStrength / 10f;
                Gizmos.DrawWireSphere(start, strengthIndicator);
            }
        }

        /// <summary>
        /// 绘制地面法线Gizmo
        /// </summary>
        private void DrawGroundNormalGizmo()
        {
            if (Data.isOnGround)
            {
                Gizmos.color = Color.blue;
                Vector3 start = Data.rb.transform.position;
                Vector3 end = start + Data.groundNormal * 2f;
                Gizmos.DrawLine(start, end);
            }
        }
    }
} 