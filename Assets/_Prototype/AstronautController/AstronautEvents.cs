using System;
using UnityEngine;
using PlayerController.Modules;

/// <summary>
/// 宇航员控制器的关键事件系统，只处理重要状态变化
/// </summary>
public static class AstronautEvents
{
    // 燃料相关事件
    public static event Action<float, float> OnFuelChanged; // 当前燃料, 最大燃料
    public static event Action OnFuelEmpty;                 // 燃料耗尽
    public static event Action<float> OnFuelCritical;       // 燃料低于临界值 (参数: 当前百分比)
    
    // 目标选择事件
    public static event Action<GameObject> OnTargetSelected;    // 选择目标
    public static event Action OnTargetDeselected;              // 取消选择目标
    public static event Action OnStabilizationCanceled;              // 取消选择目标
    
    // 同步状态事件
    public static event Action<Vector3> OnSyncStarted;      // 开始同步 (参数: 目标速度)
    public static event Action OnSyncCompleted;             // 同步完成
    public static event Action OnSyncCancelled;             // 同步取消
    
    // 移动状态事件
    public static event Action<bool> OnThrusterStateChanged; // 推进器状态变化 (参数: 是否激活)
    public static event Action<bool> OnRollingStateChanged;  // 视线轴旋转状态变化 (参数: 是否激活)
    
    // 重力场相关事件
    public static event Action<Vector3, float> OnEnterGravityField;    // 进入重力场 (重力方向, 重力强度)
    public static event Action OnExitGravityField;                     // 离开重力场
    public static event Action OnGravityTransitionStarted;             // 重力过渡开始
    public static event Action OnGravityTransitionCompleted;           // 重力过渡完成
    
    // 地面相关事件
    public static event Action<Transform> OnLandOnGround;              // 着地 (地面表面)
    public static event Action OnLeaveGround;                          // 离地
    public static event Action OnLanded;                               // 着地完成
    public static event Action OnLeftGround;                           // 离地完成
    public static event Action<Vector3, float> OnGroundMovement;       // 地面移动 (移动方向, 速度)
    public static event Action<float> OnJump;                          // 跳跃 (跳跃力度)
    public static event Action OnCrouchStarted;                        // 蹲伏开始
    public static event Action OnCrouchEnded;                          // 蹲伏结束
    
    // 控制模式切换事件
    public static event Action<ControlMode> OnControlModeChanged;      // 控制模式变化
    
    // 自动巡航事件
    public static event Action<GameObject> OnAutoCruiseStarted;        // 开始自动巡航
    public static event Action OnAutoCruiseStopped;                    // 停止自动巡航
    public static event Action<float> OnAutoCruiseDistanceChanged;     // 自动巡航距离变化
    
    // 触发事件的方法
    public static void TriggerFuelChanged(float current, float max)
    {
        OnFuelChanged?.Invoke(current, max);
    }
    
    public static void TriggerFuelEmpty()
    {
        OnFuelEmpty?.Invoke();
    }
    
    public static void TriggerFuelCritical(float percentage)
    {
        OnFuelCritical?.Invoke(percentage);
    }
    
    public static void TriggerTargetSelected(GameObject target)
    {
        OnTargetSelected?.Invoke(target);
    }
    
    public static void TriggerTargetDeselected()
    {
        OnTargetDeselected?.Invoke();
    }
    
    public static void TriggerStabilizationCancelled()
    {
        OnStabilizationCanceled?.Invoke();
    }
    
    public static void TriggerSyncStarted(Vector3 targetVelocity)
    {
        OnSyncStarted?.Invoke(targetVelocity);
    }
    
    public static void TriggerSyncCompleted()
    {
        OnSyncCompleted?.Invoke();
    }
    
    public static void TriggerSyncCancelled()
    {
        OnSyncCancelled?.Invoke();
    }
    
    public static void TriggerThrusterStateChanged(bool isActive)
    {
        OnThrusterStateChanged?.Invoke(isActive);
    }
    
    public static void TriggerRollingStateChanged(bool isActive)
    {
        OnRollingStateChanged?.Invoke(isActive);
    }
    
    // 重力场事件触发方法
    public static void TriggerEnterGravityField(Vector3 gravityDirection, float gravityStrength)
    {
        OnEnterGravityField?.Invoke(gravityDirection, gravityStrength);
    }
    
    public static void TriggerExitGravityField()
    {
        OnExitGravityField?.Invoke();
    }
    
    public static void TriggerGravityTransitionStarted()
    {
        OnGravityTransitionStarted?.Invoke();
    }
    
    public static void TriggerGravityTransitionCompleted()
    {
        OnGravityTransitionCompleted?.Invoke();
    }
    
    // 地面事件触发方法
    public static void TriggerLandOnGround(Transform groundSurface)
    {
        OnLandOnGround?.Invoke(groundSurface);
    }
    
    public static void TriggerLeaveGround()
    {
        OnLeaveGround?.Invoke();
    }
    
    public static void TriggerLanded()
    {
        OnLanded?.Invoke();
    }
    
    public static void TriggerLeftGround()
    {
        OnLeftGround?.Invoke();
    }
    
    public static void TriggerGroundMovement(Vector3 moveDirection, float speed)
    {
        OnGroundMovement?.Invoke(moveDirection, speed);
    }
    
    public static void TriggerJump(float jumpForce)
    {
        OnJump?.Invoke(jumpForce);
    }
    
    public static void TriggerCrouchStarted()
    {
        OnCrouchStarted?.Invoke();
    }
    
    public static void TriggerCrouchEnded()
    {
        OnCrouchEnded?.Invoke();
    }
    
    // 控制模式事件触发方法
    public static void TriggerControlModeChanged(ControlMode newMode)
    {
        OnControlModeChanged?.Invoke(newMode);
    }
    
    // 自动巡航事件触发方法
    public static void TriggerAutoCruiseStarted(GameObject target)
    {
        OnAutoCruiseStarted?.Invoke(target);
    }
    
    public static void TriggerAutoCruiseStopped()
    {
        OnAutoCruiseStopped?.Invoke();
    }
    
    public static void TriggerAutoCruiseDistanceChanged(float distance)
    {
        OnAutoCruiseDistanceChanged?.Invoke(distance);
    }
}