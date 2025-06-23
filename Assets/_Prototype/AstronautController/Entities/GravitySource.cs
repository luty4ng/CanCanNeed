using UnityEngine;

namespace PlayerController.Modules.Gravity
{
    /// <summary>
    /// 重力源组件 - 用于星球表面的重力
    /// </summary>
    public class GravitySource : MonoBehaviour
    {
        [Header("Gravity Settings")]
        [SerializeField] private float gravityStrength = 9.8f;    // 重力强度
        [SerializeField] private float gravityRadius = 100f;      // 重力影响半径
        [SerializeField] private bool useInverseSquare = true;    // 是否使用平方反比定律
        [SerializeField] private LayerMask affectedLayers = -1;   // 受影响的层
        
        [Header("Debug Visualization")]
        [SerializeField] private bool showGravityField = true;    // 显示重力场
        [SerializeField] private Color gravityFieldColor = Color.red; // 重力场颜色
        [SerializeField] private int fieldSegments = 16;          // 重力场分段数
        
        private void OnDrawGizmos()
        {
            if (!showGravityField) return;
            
            // 绘制重力场范围
            Gizmos.color = gravityFieldColor;
            Gizmos.DrawWireSphere(transform.position, gravityRadius);
            
            // 绘制重力方向指示器
            Gizmos.color = Color.yellow;
            Vector3 center = transform.position;
            
            for (int i = 0; i < fieldSegments; i++)
            {
                float angle = (360f / fieldSegments) * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * gravityRadius * 0.8f;
                Vector3 position = center + offset;
                Vector3 gravityDirection = GetGravityDirection(position);
                
                Gizmos.DrawRay(position, gravityDirection * 2f);
            }
        }
        
        /// <summary>
        /// 获取指定位置的重力方向
        /// </summary>
        /// <param name="position">目标位置</param>
        /// <returns>重力方向向量</returns>
        public Vector3 GetGravityDirection(Vector3 position)
        {
            Vector3 direction = (transform.position - position).normalized;
            return direction;
        }
        
        /// <summary>
        /// 获取指定位置的重力强度
        /// </summary>
        /// <param name="position">目标位置</param>
        /// <returns>重力强度</returns>
        public float GetGravityStrength(Vector3 position)
        {
            float distance = Vector3.Distance(transform.position, position);
            
            if (distance > gravityRadius)
                return 0f;
            
            if (useInverseSquare)
            {
                // 使用平方反比定律
                float normalizedDistance = distance / gravityRadius;
                return gravityStrength * (1f - normalizedDistance * normalizedDistance);
            }
            else
            {
                // 线性衰减
                float normalizedDistance = distance / gravityRadius;
                return gravityStrength * (1f - normalizedDistance);
            }
        }
        
        /// <summary>
        /// 检查指定位置是否在重力影响范围内
        /// </summary>
        /// <param name="position">目标位置</param>
        /// <returns>是否在影响范围内</returns>
        public bool IsInGravityRange(Vector3 position)
        {
            float distance = Vector3.Distance(transform.position, position);
            return distance <= gravityRadius;
        }
        
        /// <summary>
        /// 重力强度属性
        /// </summary>
        public float GravityStrength => gravityStrength;
        
        /// <summary>
        /// 重力半径属性
        /// </summary>
        public float GravityRadius => gravityRadius;
    }
} 