using UnityEngine;

namespace PlayerController.Modules
{
    /// <summary>
    /// 宇航员控制器模块的基础接口
    /// </summary>
    public interface IAstronautModule
    {
        bool Enabled { get; set; }
        /// <summary>
        /// 初始化模块
        /// </summary>
        void Initialize(AstronautData data);

        /// <summary>
        /// 每帧更新
        /// </summary>
        void OnUpdate();

        /// <summary>
        /// 物理更新
        /// </summary>
        void OnFixedUpdate();

        /// <summary>
        /// 后期更新，在所有Update之后调用
        /// </summary>
        void OnLateUpdate();

        /// <summary>
        /// GUI渲染
        /// </summary>
        void OnGUI();

        /// <summary>
        /// 绘制调试信息
        /// </summary>
        void OnDrawGizmos();

        /// <summary>
        /// 清理模块资源
        /// </summary>
        void OnDestroy();
    }

    /// <summary>
    /// 宇航员控制器模块的抽象基类，提供默认空实现
    /// </summary>
    public abstract class AstronautModuleBase : IAstronautModule
    {
        public bool Enabled { get; set; } = true;
        protected AstronautData Data { get; private set; }
        public virtual void Initialize(AstronautData data) { Data = data; }

        public virtual void OnUpdate() { }

        public virtual void OnFixedUpdate() { }

        public virtual void OnLateUpdate() { }

        public virtual void OnGUI() { }

        public virtual void OnDrawGizmos() { }
        public virtual void OnDestroy() { }
    }
}