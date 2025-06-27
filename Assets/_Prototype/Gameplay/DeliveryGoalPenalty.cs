using System.Collections.Generic;
using UnityEngine;

namespace GameProcedure
{
    /// <summary>
    /// 未达成指标时的惩罚逻辑：天体最多积累3个送货需求，超出后30秒星球被摧毁
    /// </summary>
    public class DeliveryGoalPenalty : MonoBehaviour
    {
        [Header("惩罚参数")]
        [SerializeField] private int maxRequestsPerBody = 3;
        [SerializeField] private float overflowDestroyDelay = 30f;

        // 记录每个天体的送货需求数和溢出计时
        private class BodyRequestInfo
        {
            public int requestCount = 0;
            public float overflowTimer = 0f;
            public bool isOverflow = false;
        }
        private Dictionary<GameObject, BodyRequestInfo> m_bodyRequests = new();

        // 新增送货需求
        public void AddDeliveryRequest(GameObject celestialBody)
        {
            if (!m_bodyRequests.ContainsKey(celestialBody))
                m_bodyRequests[celestialBody] = new BodyRequestInfo();
            var info = m_bodyRequests[celestialBody];
            info.requestCount++;
            if (info.requestCount > maxRequestsPerBody && !info.isOverflow)
            {
                info.isOverflow = true;
                info.overflowTimer = overflowDestroyDelay;
                Debug.Log($"天体{celestialBody.name}进入溢出状态，{overflowDestroyDelay}s后将被摧毁");
            }
        }

        private void Update()
        {
            foreach (var kv in m_bodyRequests)
            {
                var body = kv.Key;
                var info = kv.Value;
                if (info.isOverflow)
                {
                    info.overflowTimer -= Time.deltaTime;
                    if (info.overflowTimer <= 0)
                    {
                        DestroyCelestialBody(body);
                        info.isOverflow = false;
                    }
                }
            }
        }

        // 摧毁天体
        private void DestroyCelestialBody(GameObject celestialBody)
        {
            Debug.Log($"天体{celestialBody.name}被摧毁");
            Destroy(celestialBody);
        }
    }
} 