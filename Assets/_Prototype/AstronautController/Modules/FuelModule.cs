using System;
using UnityEngine;
using PlayerController.Modules;

namespace PlayerController.Modules
{
    /// <summary>
    /// 处理宇航员的燃料管理
    /// </summary>
    [Serializable]
    [ModuleDisplayName("燃料模块")]
    public class FuelModule : AstronautModuleBase
    {
        private bool m_HasSentCriticalWarning;
        private float m_PreviousFuel;
        
        // 燃料临界值百分比
        private const float FuelCriticalPercentage = 20f;

        /// <summary>
        /// 初始化燃料模块
        /// </summary>
        /// <param name="data">宇航员数据</param>
        public override void Initialize(AstronautData data)
        {
            base.Initialize(data);
            Data.currentFuel = Data.maxFuel;
            m_PreviousFuel = Data.currentFuel;
            m_HasSentCriticalWarning = false;
        }
        
        /// <summary>
        /// 每帧更新燃料状态
        /// </summary>
        public override void OnUpdate()
        {
            m_PreviousFuel = Data.currentFuel;
            UpdateFuelConsumption();
            HandleFuelEvents();
        }
        
        /// <summary>
        /// 处理燃料相关事件
        /// </summary>
        private void HandleFuelEvents()
        {
            if (!Mathf.Approximately(m_PreviousFuel, Data.currentFuel))
            {
                AstronautEvents.TriggerFuelChanged(Data.currentFuel, Data.maxFuel);
                if (m_PreviousFuel > 0 && Data.currentFuel <= 0)
                {
                    AstronautEvents.TriggerFuelEmpty();
                }
                float fuelPercentage = GetFuelPercentage();
                if (fuelPercentage <= FuelCriticalPercentage && !m_HasSentCriticalWarning)
                {
                    m_HasSentCriticalWarning = true;
                    AstronautEvents.TriggerFuelCritical(fuelPercentage);
                }
                else if (fuelPercentage > FuelCriticalPercentage && m_HasSentCriticalWarning)
                {
                    m_HasSentCriticalWarning = false;
                }
            }
        }
        
        /// <summary>
        /// 更新燃料消耗
        /// </summary>
        private void UpdateFuelConsumption()
        {
            ConsumeThrusterFuel();
            ConsumeSyncFuel();
        }
        
        /// <summary>
        /// 推进器燃料消耗
        /// </summary>
        private void ConsumeThrusterFuel()
        {
            if (Data.isUsingThrusters && Data.currentFuel > 0)
            {
                Data.currentFuel -= Data.fuelConsumptionRate * Time.deltaTime;
                Data.currentFuel = Mathf.Max(0f, Data.currentFuel);
            }
        }
        
        /// <summary>
        /// 同步过程中的燃料消耗
        /// </summary>
        private void ConsumeSyncFuel()
        {
            if (Data.isSyncing && Data.currentFuel > 0)
            {
                Data.currentFuel -= Data.fuelConsumptionRate * Data.syncFuelMultiplier * Time.deltaTime;
                Data.currentFuel = Mathf.Max(0f, Data.currentFuel);
                
                if (Data.currentFuel <= 0)
                {
                    Data.isSyncing = false;
                    AstronautEvents.TriggerSyncCancelled();
                }
            }
        }
        
        /// <summary>
        /// 添加燃料
        /// </summary>
        /// <param name="amount">添加的燃料量</param>
        public void AddFuel(float amount)
        {
            float previous = Data.currentFuel;
            Data.currentFuel = Mathf.Min(Data.currentFuel + amount, Data.maxFuel);
            
            if (!Mathf.Approximately(previous, Data.currentFuel))
            {
                AstronautEvents.TriggerFuelChanged(Data.currentFuel, Data.maxFuel);
            }
        }
        
        /// <summary>
        /// 获取当前燃料百分比
        /// </summary>
        /// <returns>燃料百分比(0-100)</returns>
        public float GetFuelPercentage()
        {
            return (Data.currentFuel / Data.maxFuel) * 100f;
        }
    }
}