using UnityEngine;
using System;
using System.Collections.Generic;

namespace PlayerController.Modules.Gravity
{
    /// <summary>
    /// 重力板组件 - 用于人造重力场
    /// </summary>
    public class GravityPlate : MonoBehaviour
    {
        [Header("Gravity Plate Settings")]
        
        [SerializeField] private Vector3 gravityDirection = Vector3.down; // 重力方向
        [SerializeField] private float gravityStrength = 9.8f;           // 重力强度
        [SerializeField] private Vector3 gravityBoxSize = new Vector3(10f, 10f, 10f); // 重力影响区域尺寸
        [SerializeField] private Vector3 gravityBoxCenterOffset = Vector3.zero; // 重力影响区域中心偏移 
        [SerializeField] private bool isActive = true;                   // 是否激活
        [SerializeField] private LayerMask affectedLayers = -1;          // 受影响的层
        
        public static event Action<Collider, GravityPlate> OnPlayerEnterPlate;
        public static event Action<Collider, GravityPlate> OnPlayerExitPlate;
        
        private HashSet<Collider> m_CurrentPlayers = new HashSet<Collider>();
        
        private void Start()
        {
            // 确保重力方向是标准化的
            gravityDirection = gravityDirection.normalized;
        }
        
        private void FixedUpdate()
        {
            if (!isActive) return;
            // 只保留OverlapBox批量检测
            Vector3 center = transform.position + gravityBoxCenterOffset;
            Collider[] hits = Physics.OverlapBox(center, gravityBoxSize * 0.5f, transform.rotation, affectedLayers);
            var newPlayers = new HashSet<Collider>(hits);
            foreach (var col in newPlayers)
            {
                if (!m_CurrentPlayers.Contains(col))
                {
                    OnPlayerEnterPlate?.Invoke(col, this);
                    Debug.Log($"物体 {col.name} 进入重力板范围");
                }
            }
            foreach (var col in m_CurrentPlayers)
            {
                if (!newPlayers.Contains(col))
                {
                    OnPlayerExitPlate?.Invoke(col, this);
                    Debug.Log($"物体 {col.name} 离开重力板范围");
                }
            }
            m_CurrentPlayers = newPlayers;
        }
        
        /// <summary>
        /// 激活重力板
        /// </summary>
        public void Activate()
        {
            if (isActive) return;
            
            isActive = true;
            Debug.Log($"重力板 {name} 已激活");
        }
        
        /// <summary>
        /// 停用重力板
        /// </summary>
        public void Deactivate()
        {
            if (!isActive) return;
            
            isActive = false;
            Debug.Log($"重力板 {name} 已停用");
            // 离开事件全部触发
            foreach (var col in m_CurrentPlayers)
            {
                OnPlayerExitPlate?.Invoke(col, this);
            }
            m_CurrentPlayers.Clear();
        }
        
        /// <summary>
        /// 切换重力板状态
        /// </summary>
        public void Toggle()
        {
            if (isActive)
                Deactivate();
            else
                Activate();
        }
        
        /// <summary>
        /// 设置重力方向
        /// </summary>
        /// <param name="direction">新的重力方向</param>
        public void SetGravityDirection(Vector3 direction)
        {
            gravityDirection = direction.normalized;
        }
        
        /// <summary>
        /// 设置重力强度
        /// </summary>
        /// <param name="strength">新的重力强度</param>
        public void SetGravityStrength(float strength)
        {
            gravityStrength = Mathf.Max(0f, strength);
        }
        
        /// <summary>
        /// 设置重力影响区域尺寸
        /// </summary>
        /// <param name="size">新的重力影响区域尺寸</param>
        public void SetGravityBoxSize(Vector3 size)
        {
            gravityBoxSize = new Vector3(
                Mathf.Max(0f, size.x),
                Mathf.Max(0f, size.y),
                Mathf.Max(0f, size.z)
            );
        }
        
        /// <summary>
        /// 设置重力影响区域为立方体
        /// </summary>
        /// <param name="size">立方体的边长</param>
        public void SetGravityBoxSize(float size)
        {
            gravityBoxSize = new Vector3(size, size, size);
        }
        
        /// <summary>
        /// 设置重力影响区域中心偏移
        /// </summary>
        /// <param name="offset">中心偏移量</param>
        public void SetGravityBoxCenterOffset(Vector3 offset)
        {
            gravityBoxCenterOffset = offset;
        }
        
        private void OnDrawGizmos()
        {
            // 绘制重力场范围
            Gizmos.color = isActive ? Color.blue : Color.gray;
            Gizmos.DrawWireCube(transform.position + gravityBoxCenterOffset, gravityBoxSize);
            
            // 绘制重力方向
            if (isActive)
            {
                Gizmos.color = Color.yellow;
                Vector3 start = transform.position;
                Vector3 end = start + gravityDirection * 3f;
                Gizmos.DrawLine(start, end);
                
                // 绘制箭头
                Vector3 arrowTip = end;
                Vector3 arrowBase = end - gravityDirection * 0.5f;
                Vector3 right = Vector3.Cross(gravityDirection, Vector3.up).normalized;
                if (right.magnitude < 0.001f)
                {
                    right = Vector3.Cross(gravityDirection, Vector3.forward).normalized;
                }
                Vector3 up = Vector3.Cross(right, gravityDirection).normalized;
                
                Gizmos.DrawLine(arrowTip, arrowBase + right * 0.3f);
                Gizmos.DrawLine(arrowTip, arrowBase - right * 0.3f);
                Gizmos.DrawLine(arrowTip, arrowBase + up * 0.3f);
                Gizmos.DrawLine(arrowTip, arrowBase - up * 0.3f);
            }
        }
        
        /// <summary>
        /// 检查指定位置是否在重力影响范围内
        /// </summary>
        /// <param name="position">目标位置</param>
        /// <returns>是否在影响范围内</returns>
        public bool IsInGravityRange(Vector3 position)
        {
            if (!isActive) return false;
            
            // 将世界坐标转换为本地坐标，考虑中心偏移
            Vector3 localPosition = transform.InverseTransformPoint(position - gravityBoxCenterOffset);
            
            // 检查是否在包围盒范围内
            Vector3 halfSize = gravityBoxSize * 0.5f;
            return Mathf.Abs(localPosition.x) <= halfSize.x &&
                   Mathf.Abs(localPosition.y) <= halfSize.y &&
                   Mathf.Abs(localPosition.z) <= halfSize.z;
        }
        
        // 公共属性
        public Vector3 GravityDirection => gravityDirection;
        public float GravityStrength => gravityStrength;
        public Vector3 GravityBoxSize => gravityBoxSize;
        public Vector3 GravityBoxCenterOffset => gravityBoxCenterOffset;
        public bool IsActive => isActive;
    }
} 