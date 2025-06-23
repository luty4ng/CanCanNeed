using System;
using UnityEngine;

namespace PlayerController.Modules.Gravity
{
    /// <summary>
    /// 地面移动组件 - 处理地面行走、跳跃、蹲伏等动作
    /// </summary>

    [Serializable]
    [ModuleDisplayName("地面移动模块")]
    public class GroundMovementModule : AstronautModuleBase
    {
        private bool isRunning;
        private bool wasGrounded;
        private bool isAligningToGround;
        private Quaternion targetGroundRotation;
        private RigidbodyConstraints originalConstraints;
        
        public override void Initialize(AstronautData data)
        {
            base.Initialize(data);
            // 保存原始Rigidbody约束
            originalConstraints = Data.rb.constraints;
            wasGrounded = Data.isOnGround;
            isRunning = false;
            wasGrounded = false;
            isAligningToGround = false;
            targetGroundRotation = Quaternion.identity;
        }

        public override void OnUpdate()
        {
            // 处理输入
            HandleInput();

            // 处理地面对齐
            HandleGroundAlignment();

            // 检查地面状态变化
            CheckGroundStateChange();
        }

        public override void OnFixedUpdate()
        {
            // 处理地面移动
            HandleGroundMovement();

            // 处理跳跃
            HandleJump();
        }

        private void HandleInput()
        {
            // 检测奔跑输入 (左Shift)
            isRunning = Input.GetKey(KeyCode.LeftShift);
        }

        private void HandleGroundMovement()
        {
            if (!Data.isInGravityField || !Data.isOnGround) return;

            // 获取移动输入
            Vector3 moveInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

            if (moveInput.magnitude > 0.1f)
            {
                // 计算移动速度
                float currentSpeed = GetCurrentSpeed();

                // 将输入转换为世界空间
                Vector3 moveDirection = Data.rb.transform.TransformDirection(moveInput.normalized);

                // 直接使用transform移动，避免rigidbody加速
                Vector3 movement = moveDirection * currentSpeed * Time.fixedDeltaTime;
                Data.rb.transform.position += movement;

                // 触发移动事件
                AstronautEvents.TriggerGroundMovement(moveDirection, currentSpeed);
            }
        }

        private void HandleJump()
        {
            if (!Data.isInGravityField || !Data.isOnGround) return;

            // 检测跳跃输入 (空格键)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                PerformJump();
            }
        }

        private void PerformJump()
        {
            // 计算跳跃方向 (垂直于地面)
            Vector3 jumpDirection = -Data.gravityDirection;

            // 应用跳跃力
            Vector3 jumpForceVector = jumpDirection * Data.jumpForce;
            Data.rb.AddForce(jumpForceVector, ForceMode.Impulse);

            // 触发跳跃事件
            AstronautEvents.TriggerJump(Data.jumpForce);

            Debug.Log($"跳跃! 力度: {Data.jumpForce}");
        }

        private float GetCurrentSpeed()
        {
            if (isRunning)
                return Data.runSpeed;
            else
                return Data.walkSpeed;
        }

        private void CheckGroundStateChange()
        {
            if (Data.isOnGround != wasGrounded)
            {
                wasGrounded = Data.isOnGround;

                if (Data.isOnGround)
                {
                    // 着地
                    AstronautEvents.TriggerLanded();
                    StartGroundAlignment();
                }
                else
                {
                    // 离地
                    AstronautEvents.TriggerLeftGround();
                    EndGroundAlignment();
                }
            }
        }

        private void HandleGroundAlignment()
        {
            if (!Data.isInGravityField || !Data.isOnGround) return;

            if (isAligningToGround)
            {
                // 平滑旋转到目标地面旋转
                Data.rb.transform.rotation = Quaternion.Slerp(
                    Data.rb.transform.rotation,
                    targetGroundRotation,
                    Data.groundAlignmentSpeed * Time.deltaTime
                );

                // 检查是否对齐完成
                float angleDifference = Quaternion.Angle(Data.transform.rotation, targetGroundRotation);
                if (angleDifference < Data.groundAlignmentThreshold)
                {
                    Data.rb.transform.rotation = targetGroundRotation;
                    isAligningToGround = false;
                }
            }
        }

        private void StartGroundAlignment()
        {
            if (!Data.isInGravityField) return;

            // 计算目标地面旋转（使玩家脚着地）
            Vector3 upDirection = -Data.gravityDirection;
            Vector3 forwardDirection = Data.rb.transform.forward;

            // 确保forward方向垂直于重力方向
            forwardDirection = Vector3.ProjectOnPlane(forwardDirection, upDirection).normalized;
            if (forwardDirection.magnitude < 0.1f)
            {
                // 如果forward方向几乎平行于重力方向，使用一个默认方向
                forwardDirection = Vector3.Cross(upDirection, Vector3.right).normalized;
                if (forwardDirection.magnitude < 0.1f)
                {
                    forwardDirection = Vector3.Cross(upDirection, Vector3.forward).normalized;
                }
            }

            targetGroundRotation = Quaternion.LookRotation(forwardDirection, upDirection);
            isAligningToGround = true;

            // 锁定旋转轴，防止翻滚
            LockRotationAxes();
        }

        private void EndGroundAlignment()
        {
            isAligningToGround = false;

            // 恢复原始约束
            UnlockRotationAxes();
        }

        private void LockRotationAxes()
        {
            // 锁定X和Z轴旋转，只允许Y轴旋转（左右转向）
            Data.rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        private void UnlockRotationAxes()
        {
            // 恢复原始约束
            Data.rb.constraints = originalConstraints;
        }

        public override void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // 绘制移动方向
            if (Data.isInGravityField && Data.isOnGround)
            {
                Vector3 moveInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
                if (moveInput.magnitude > 0.1f)
                {
                    Gizmos.color = Color.green;
                    Vector3 moveDirection = Data.rb.transform.TransformDirection(moveInput.normalized);
                    Vector3 start = Data.rb.transform.position;
                    Vector3 end = start + moveDirection * 2f;
                    Gizmos.DrawLine(start, end);
                    Gizmos.DrawSphere(end, 0.1f);
                }
            }

            // 绘制地面对齐状态
            if (isAligningToGround)
            {
                Gizmos.color = Color.cyan;
                Vector3 start = Data.rb.transform.position;
                Vector3 end = start + Data.rb.transform.up * 2f;
                Gizmos.DrawLine(start, end);
                Gizmos.DrawSphere(end, 0.1f);

                // 绘制目标旋转
                Gizmos.color = Color.magenta;
                Vector3 targetUp = targetGroundRotation * Vector3.up;
                Vector3 targetEnd = start + targetUp * 2f;
                Gizmos.DrawLine(start, targetEnd);
                Gizmos.DrawSphere(targetEnd, 0.1f);
            }
        }
    }
}