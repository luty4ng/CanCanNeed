using UnityEngine;

namespace GameProcedure
{
    /// <summary>
    /// 达成指标时的奖励逻辑：星球/轨道创造射弹，射弹命中天体后触发功能
    /// </summary>
    public class DeliveryGoalReward : MonoBehaviour
    {
        [Header("奖励参数")]
        [SerializeField] private GameObject projectilePrefab; // 射弹预制体
        [SerializeField] private float projectileSpeed = 20f;

        // 创建射弹并发射到目标天体
        public void CreateAndLaunchProjectile(Vector3 startPos, Transform target, System.Action onHit)
        {
            if (projectilePrefab == null || target == null) return;
            var projectile = Instantiate(projectilePrefab, startPos, Quaternion.identity);
            var projComp = projectile.AddComponent<RewardProjectile>();
            projComp.Init(target, projectileSpeed, onHit);
        }
    }

    /// <summary>
    /// 奖励射弹逻辑，命中天体后触发回调
    /// </summary>
    public class RewardProjectile : MonoBehaviour
    {
        private Transform m_target;
        private float m_speed;
        private System.Action m_onHit;
        private bool m_hit;

        public void Init(Transform target, float speed, System.Action onHit)
        {
            m_target = target;
            m_speed = speed;
            m_onHit = onHit;
        }

        private void Update()
        {
            if (m_target == null || m_hit) return;
            transform.position = Vector3.MoveTowards(transform.position, m_target.position, m_speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, m_target.position) < 0.5f)
            {
                m_hit = true;
                m_onHit?.Invoke();
                Destroy(gameObject);
            }
        }
    }
} 