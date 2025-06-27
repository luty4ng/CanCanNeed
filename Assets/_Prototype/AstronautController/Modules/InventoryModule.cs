using System.Collections.Generic;
using UnityEngine;
using PlayerController.Modules;

namespace Astronaut
{
    /// <summary>
    /// 玩家背包模块，支持物品携带、切换、忍下、蓄力扔出等功能
    /// </summary>
    public class InventoryModule : AstronautModuleBase
    {
        private List<GameObject> m_items = new();
        private int m_currentItemIndex = -1;

        private List<string> m_skills = new(); // 技能用字符串占位，实际可用ScriptableObject等
        public IReadOnlyList<GameObject> Items => m_items;
        public IReadOnlyList<string> Skills => m_skills;
        public int CurrentItemIndex => m_currentItemIndex;
        public GameObject CurrentItem => (m_currentItemIndex >= 0 && m_currentItemIndex < m_items.Count) ? m_items[m_currentItemIndex] : null;
        
        public bool AddItem(GameObject item)
        {
            if (m_items.Count >= Data.itemCapacity) return false;
            m_items.Add(item);
            if (m_currentItemIndex == -1) m_currentItemIndex = 0;
            return true;
        }

        // 移除当前物品
        public GameObject RemoveCurrentItem()
        {
            if (CurrentItem == null) return null;
            var item = CurrentItem;
            m_items.RemoveAt(m_currentItemIndex);
            if (m_items.Count == 0) m_currentItemIndex = -1;
            else if (m_currentItemIndex >= m_items.Count) m_currentItemIndex = m_items.Count - 1;
            return item;
        }

        // 切换物品（1-n）
        public void SwitchItem(int index)
        {
            if (index >= 0 && index < m_items.Count)
                m_currentItemIndex = index;
        }

        // 添加技能
        public void AddSkill(string skillId)
        {
            if (!m_skills.Contains(skillId))
                m_skills.Add(skillId);
        }

        // 移除技能
        public void RemoveSkill(string skillId)
        {
            m_skills.Remove(skillId);
        }

        // G键扔下当前物品（继承惯性）
        public void DropCurrentItemWithInertia(Transform playerTransform, Rigidbody playerRb)
        {
            var item = RemoveCurrentItem();
            if (item != null)
            {
                item.transform.position = playerTransform.position + playerTransform.forward * 1f;
                var rb = item.GetComponent<Rigidbody>();
                if (rb == null) rb = item.AddComponent<Rigidbody>();
                rb.velocity = playerRb.velocity; // 继承玩家惯性
            }
        }

        // 长按左键蓄力扔出当前物品
        public void ThrowCurrentItem(Transform playerTransform, Rigidbody playerRb, float chargeTime, float maxChargeTime, float maxThrowForce)
        {
            var item = RemoveCurrentItem();
            if (item != null)
            {
                item.transform.position = playerTransform.position + playerTransform.forward * 1f;
                var rb = item.GetComponent<Rigidbody>();
                if (rb == null) rb = item.AddComponent<Rigidbody>();
                float force = Mathf.Lerp(5f, maxThrowForce, Mathf.Clamp01(chargeTime / maxChargeTime));
                rb.velocity = playerRb.velocity; // 继承惯性
                rb.AddForce(playerTransform.forward * force, ForceMode.VelocityChange);
                // 玩家反作用力
                playerRb.AddForce(-playerTransform.forward * force, ForceMode.VelocityChange);
            }
        }

        // AstronautModuleBase生命周期方法（如需扩展可重写）
        public override void OnUpdate()
        {
            // G键扔下
            if (Data.isDropItemRequested)
            {
                DropCurrentItemWithInertia(Data.transform, Data.rb);
                Data.isDropItemRequested = false;
            }
            // 松开左键蓄力扔出
            if (Data.isThrowItemRequested)
            {
                // 这里maxChargeTime和maxThrowForce可根据需要参数化
                float maxChargeTime = 2f;
                float maxThrowForce = 20f;
                ThrowCurrentItem(Data.transform, Data.rb, Data.throwItemChargeTime, maxChargeTime, maxThrowForce);
                Data.isThrowItemRequested = false;
                Data.throwItemChargeTime = 0f;
            }
        }
        public override void OnFixedUpdate() { }
        public override void OnLateUpdate() { }
        public override void OnGUI() { }
        public override void OnDrawGizmos() { }
        public override void OnDestroy() { }
    }
} 