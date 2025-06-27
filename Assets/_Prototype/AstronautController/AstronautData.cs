using UnityEngine;

[DisallowMultipleComponent]
public class AstronautData : MonoBehaviour
{
    [Header("通用设置")]
    public Camera playerCamera;
    public Rigidbody rb;

    [Header("输入模块设置")]
    public float rotationSpeed = 100f;
    public float rollSpeed = 120f;

    [Header("移动模块设置")]
    public float thrusterForce = 10f;
    public float maxVelocity = 20f;

    [Header("燃料模块设置")]
    public float maxFuel = 100f;
    public float fuelConsumptionRate = 1f;

    [Header("目标选择模块设置")]
    public float maxTargetDistance = 100f;
    public LayerMask targetLayers = -1;
    public Color targetHighlightColor = Color.red;

    [Header("自动巡航模块设置")]
    public float autoCruiseDistance = 5f;        // 自动巡航保持距离
    public float autoCruiseSpeed = 8f;           // 自动巡航速度
    public float autoCruiseAcceleration = 2f;    // 自动巡航加速度
    public float autoCruiseDeceleration = 3f;    // 自动巡航减速度
    public float autoCruiseRotationSpeed = 3f;   // 自动巡航旋转速度

    [Header("速度同步模块设置")]
    public float syncCooldown = 2f;
    public float syncSpeed = 2f;
    public float syncFuelMultiplier = 2f;

    [Header("重力检测模块设置")]
    public float gravityDetectionDistance = 10f;
    public LayerMask gravityLayers = -1;

    [Header("重力物理模块设置")]
    public float groundCheckDistance = 1.1f;
    public LayerMask groundLayers = -1;
    public float groundDrag = 1f;
    public float groundAngularDrag = 5f;
    public float airDrag = 0.1f;
    public float airAngularDrag = 0.05f;
    public float gravityTransitionTime = 0.5f;

    [Header("地面移动模块设置")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpForce = 16f;
    public float groundAlignmentSpeed = 5f;
    public float groundAlignmentThreshold = 0.1f;

    [Header("调试模块设置")]
    public Color velocityGizmoColor = Color.blue;
    public Color syncGizmoColor = Color.yellow;
    public float gizmoThickness = 2f;
    
    [Header("库存模块设置")]
    public int itemCapacity = 3;

    // 运行时状态
    [Header("运行时状态")]
    public GameObject currentTarget;
    public bool isSyncing;
    public bool isRolling;
    public bool isInGravityField;
    public Vector3 gravityDirection = Vector3.down;
    public float gravityStrength;
    public bool isOnGround;
    public bool isInWater;
    public float currentFuel;
    public bool isUsingThrusters;
    public Vector3 moveInput;
    public Vector2 lookInput;
    public float rollInput;
    public Renderer targetRenderer;
    public Material originalMaterial;
    public Color originalColor;
    public float lastSyncTime;
    public bool isSyncRequested;
    public bool isStabilizeRequested;
    public bool isJumpingRequested;
    public Vector3 targetVelocity;
    public Transform groundSurface;
    public Vector3 groundNormal = Vector3.up;
    public ControlMode currentControlMode = ControlMode.Space;
    public Transform gravitySource;
    public float groundDistance;
    public bool isAutoCruising;                  // 是否正在自动巡航
    public Vector3 autoCruiseTargetPosition;     // 自动巡航目标位置
    public Vector3 autoCruiseDirection;          // 自动巡航方向
    public float autoCruiseCurrentSpeed;         // 当前自动巡航速度

    // 库存输入状态
    [Header("库存输入状态")]
    public bool isDropItemRequested = false; // G键
    public bool isThrowItemCharging = false; // 是否正在蓄力
    public bool isThrowItemRequested = false; // 松开左键
    public float throwItemChargeTime = 0f;   // 蓄力时长
}

/// <summary>
/// 控制模式枚举
/// </summary>
public enum ControlMode
{
    Space,    // 太空模式
    Ground,   // 地面模式
    Water     // 水中模式 (预留)
}