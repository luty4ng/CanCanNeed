using PlayerController.Modules;
using PlayerController.Modules.Gravity;
using System.Collections.Generic;
using UnityEngine;
using Astronaut;

/// <summary>
/// 宇航员控制器 - 主MonoBehaviour组件
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AstronautData))]
public class AstronautController : MonoBehaviour
{
    private static AstronautController m_instance;
    public static AstronautController Instance => m_instance;
    // 数据与模块
    public AstronautData data;
    private List<IAstronautModule> modules = new List<IAstronautModule>();

    // 各功能模块
    private InputModule inputModule;
    private MotionModule motionModule;
    private FuelModule fuelModule;
    private TargetingModule targetingModule;
    private SyncModule syncModule;
    private HeadUpDisplayModule uiModule;
    private DebugModule debugModule;
    private DetectionModule gravityDetectionModule;
    private GravityPhysicsModule gravityPhysicsModule;
    private InventoryModule inventoryModule;

    #region Unity生命周期
    private void Awake()
    {
        m_instance = this;
        data = GetComponent<AstronautData>();
        data.playerCamera = GetComponentInChildren<Camera>();
        data.rb = GetComponent<Rigidbody>();
        InitModules();
        SubscribeToEvents();
    }

    private void Start()
    {
        foreach (var module in modules)
            module.Initialize(data);
    }

    private void Update()
    {
        foreach (var module in modules)
            if (module.Enabled) module.OnUpdate();
    }

    private void FixedUpdate()
    {
        foreach (var module in modules)
            if (module.Enabled) module.OnFixedUpdate();
    }

    private void LateUpdate()
    {
        foreach (var module in modules)
            if (module.Enabled) module.OnLateUpdate();
    }

    private void OnGUI()
    {
        foreach (var module in modules)
            if (module.Enabled) module.OnGUI();
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || modules == null || modules.Count == 0) return;
        foreach (var module in modules)
            if (module.Enabled) module.OnDrawGizmos();
    }

    private void OnDestroy()
    {
        foreach (var module in modules)
            module.OnDestroy();
        UnsubscribeFromEvents();
    }
    #endregion

    #region 模块初始化
    private void InitModules()
    {
        inputModule = new InputModule();
        motionModule = new MotionModule();
        fuelModule = new FuelModule();
        targetingModule = new TargetingModule();
        syncModule = new SyncModule();
        uiModule = new HeadUpDisplayModule();
        debugModule = new DebugModule();
        gravityDetectionModule = new DetectionModule();
        gravityPhysicsModule = new GravityPhysicsModule();
        inventoryModule = new InventoryModule();

        modules.Clear();
        modules.Add(inputModule);
        modules.Add(motionModule);
        modules.Add(fuelModule);
        modules.Add(targetingModule);
        modules.Add(syncModule);
        modules.Add(uiModule);
        modules.Add(debugModule);
        modules.Add(gravityDetectionModule);
        modules.Add(gravityPhysicsModule);
        modules.Add(inventoryModule);
    }
    #endregion

    #region 事件订阅
    private void SubscribeToEvents()
    {
        AstronautEvents.OnFuelEmpty += HandleFuelEmpty;
        AstronautEvents.OnSyncCompleted += HandleSyncCompleted;
        AstronautEvents.OnEnterGravityField += HandleEnterGravityField;
        AstronautEvents.OnExitGravityField += HandleExitGravityField;
        AstronautEvents.OnGravityTransitionStarted += HandleGravityTransitionStarted;
        AstronautEvents.OnGravityTransitionCompleted += HandleGravityTransitionCompleted;
        AstronautEvents.OnLandOnGround += HandleLandOnGround;
        AstronautEvents.OnLeaveGround += HandleLeaveGround;
        AstronautEvents.OnLanded += HandleLanded;
        AstronautEvents.OnLeftGround += HandleLeftGround;
        AstronautEvents.OnGroundMovement += HandleGroundMovement;
        AstronautEvents.OnJump += HandleJump;
        AstronautEvents.OnCrouchStarted += HandleCrouchStarted;
        AstronautEvents.OnCrouchEnded += HandleCrouchEnded;
        AstronautEvents.OnControlModeChanged += HandleControlModeChanged;
    }

    private void UnsubscribeFromEvents()
    {
        AstronautEvents.OnFuelEmpty -= HandleFuelEmpty;
        AstronautEvents.OnSyncCompleted -= HandleSyncCompleted;
        AstronautEvents.OnEnterGravityField -= HandleEnterGravityField;
        AstronautEvents.OnExitGravityField -= HandleExitGravityField;
        AstronautEvents.OnGravityTransitionStarted -= HandleGravityTransitionStarted;
        AstronautEvents.OnGravityTransitionCompleted -= HandleGravityTransitionCompleted;
        AstronautEvents.OnLandOnGround -= HandleLandOnGround;
        AstronautEvents.OnLeaveGround -= HandleLeaveGround;
        AstronautEvents.OnLanded -= HandleLanded;
        AstronautEvents.OnLeftGround -= HandleLeftGround;
        AstronautEvents.OnGroundMovement -= HandleGroundMovement;
        AstronautEvents.OnJump -= HandleJump;
        AstronautEvents.OnCrouchStarted -= HandleCrouchStarted;
        AstronautEvents.OnCrouchEnded -= HandleCrouchEnded;
        AstronautEvents.OnControlModeChanged -= HandleControlModeChanged;
    }
    #endregion

    #region 事件处理
    private void HandleFuelEmpty() => Debug.Log("燃料耗尽!");
    private void HandleSyncCompleted() => Debug.Log("速度同步完成!");
    private void HandleEnterGravityField(Vector3 gravityDirection, float gravityStrength) => Debug.Log($"进入重力场! 方向: {gravityDirection}, 强度: {gravityStrength}");
    private void HandleExitGravityField() => Debug.Log("离开重力场!");
    private void HandleGravityTransitionStarted() => Debug.Log("重力过渡开始!");
    private void HandleGravityTransitionCompleted() => Debug.Log("重力过渡完成!");
    private void HandleLandOnGround(Transform groundSurface) => Debug.Log($"着地! 地面: {groundSurface?.name ?? "未知"}");
    private void HandleLeaveGround() => Debug.Log("离地!");
    private void HandleLanded() => Debug.Log("着地完成!");
    private void HandleLeftGround() => Debug.Log("离地完成!");
    private void HandleGroundMovement(Vector3 moveDirection, float speed) { /* 可扩展地面移动反馈 */ }
    private void HandleJump(float jumpForce) => Debug.Log($"跳跃! 力度: {jumpForce}");
    private void HandleCrouchStarted() => Debug.Log("开始蹲伏!");
    private void HandleCrouchEnded() => Debug.Log("结束蹲伏!");
    private void HandleControlModeChanged(ControlMode newMode) => Debug.Log($"控制模式切换: {newMode}");
    #endregion

    #region 公共接口
    public void AddFuel(float amount) => fuelModule?.AddFuel(amount);
    public float GetFuelPercentage() => fuelModule != null ? fuelModule.GetFuelPercentage() : 0f;
    public GameObject GetCurrentTarget() => data != null ? data.currentTarget : null;
    public void UnselectTarget() => targetingModule?.UnselectTarget();
    public Vector3 GetGravityDirection() => data != null ? data.gravityDirection : Vector3.down;
    public float GetGravityStrength() => data != null ? data.gravityStrength : 0f;
    public bool IsOnGround() => data != null && data.isOnGround;
    public ControlMode GetCurrentControlMode() => data != null ? data.currentControlMode : ControlMode.Space;
    public Transform GetGroundSurface() => data != null ? data.groundSurface : null;
    public Vector3 GetGroundNormal() => data != null ? data.groundNormal : Vector3.up;
    public T GetModule<T>() where T : class, IAstronautModule
    {
        foreach (var module in modules)
            if (module is T targetModule) return targetModule;
        return null;
    }
    public List<IAstronautModule> GetAllModules() => new List<IAstronautModule>(modules);
    public bool IsModuleEnabled<T>() where T : class, IAstronautModule => GetModule<T>() != null;
    public void SetModuleEnabled<T>(bool enabled) where T : class, IAstronautModule
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("运行时不建议动态启用/禁用模块");
            return;
        }
        // 编辑器下可扩展
    }

    public void AddInventoryItem(GameObject prefab)
    {
        GameObject instance = Object.Instantiate(prefab);
        if(!inventoryModule.AddItem(instance))
            Destroy(instance);
    }
    #endregion
}