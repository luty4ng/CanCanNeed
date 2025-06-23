using System;
using UnityEngine;

namespace PlayerController.Modules
{
    /// <summary>
    /// 处理宇航员的调试可视化
    /// </summary>
    [Serializable]
    [ModuleDisplayName("调试模块")]
    public class DebugModule : AstronautModuleBase
    {
        /// <summary>
        /// 绘制调试Gizmos
        /// </summary>
        public override void OnDrawGizmos()
        {
            if (Data.rb == null) return;

            DrawCurrentVelocityGizmo();
            DrawSyncTargetVelocityGizmo();
            DrawViewAxisGizmo();
        }

        /// <summary>
        /// 绘制当前速度向量Gizmo
        /// </summary>
        private void DrawCurrentVelocityGizmo()
        {
            DrawVelocityArrow(Data.rb.transform.position, Data.rb.velocity, Data.velocityGizmoColor);
        }

        /// <summary>
        /// 绘制同步目标速度向量Gizmo
        /// </summary>
        private void DrawSyncTargetVelocityGizmo()
        {
            if (Data.isSyncing && Data.currentTarget != null)
            {
                DrawVelocityArrow(Data.rb.transform.position, Data.targetVelocity, Data.syncGizmoColor);
            }
        }

        /// <summary>
        /// 绘制视线轴Gizmo
        /// </summary>
        private void DrawViewAxisGizmo()
        {
            if (Data.isRolling && Data.playerCamera != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(Data.playerCamera.transform.position, Data.playerCamera.transform.forward * 2f);
            }
        }

        /// <summary>
        /// 辅助方法：绘制速度箭头
        /// </summary>
        /// <param name="position">起点</param>
        /// <param name="velocity">速度向量</param>
        /// <param name="color">箭头颜色</param>
        private void DrawVelocityArrow(Vector3 position, Vector3 velocity, Color color)
        {
            float velocityMagnitude = velocity.magnitude;
            if (velocityMagnitude > 0.01f)
            {
                Vector3 velocityDirection = velocity.normalized;
                Gizmos.color = color;
                Gizmos.DrawLine(position, position + velocityDirection * velocityMagnitude);
                Vector3 arrowPos = position + velocityDirection * velocityMagnitude;
                float arrowSize = 0.5f;
                Vector3 right = Vector3.Cross(velocityDirection, Vector3.up).normalized;
                if (right.magnitude < 0.001f)
                {
                    right = Vector3.Cross(velocityDirection, Vector3.forward).normalized;
                }
                Vector3 up = Vector3.Cross(right, velocityDirection).normalized;
                Gizmos.DrawLine(arrowPos, arrowPos - velocityDirection * arrowSize + right * arrowSize * 0.5f);
                Gizmos.DrawLine(arrowPos, arrowPos - velocityDirection * arrowSize - right * arrowSize * 0.5f);
                Gizmos.DrawLine(arrowPos, arrowPos - velocityDirection * arrowSize + up * arrowSize * 0.5f);
                Gizmos.DrawLine(arrowPos, arrowPos - velocityDirection * arrowSize - up * arrowSize * 0.5f);
            }
        }
    }
}