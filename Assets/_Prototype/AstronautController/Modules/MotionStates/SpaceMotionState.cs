using UnityEngine;
using PlayerController.Modules;

namespace PlayerController.Modules.MotionStates
{
    public class SpaceMotionState : BaseMotionState
    {
        private bool m_wasThrusterActive;
        private const float AUTO_STOP_THRESHOLD = 0.2f;
        private const float AUTO_STOP_RATE = 0.95f;
        private Vector3 m_lastCameraForward;

        public override string StateName => "SpaceMotion";

        protected override void OnStateEnter()
        {
            Data.rb.useGravity = false;
            Data.rb.drag = 0f;
            Data.rb.angularDrag = 0f;
            
            if (Data.playerCamera != null)
            {
                m_lastCameraForward = Data.playerCamera.transform.forward;
            }
            
            m_wasThrusterActive = false;
            m_lastCameraForward = Vector3.forward;
        }

        protected override void OnStateExit()
        {
            Data.isUsingThrusters = false;
            AstronautEvents.TriggerThrusterStateChanged(false);
        }

        protected override void OnStateUpdate()
        {
            HandleRotation();
        }

        protected override void OnStateFixedUpdate()
        {
            HandleMovement();
            HandleAutoStop();
        }

        private void HandleMovement()
        {
            if (Data.currentFuel <= 0) return;

            // 在自动巡航状态下，禁用手动移动控制
            if (Data.isAutoCruising)
            {
                // 如果之前在使用推进器，现在停止
                if (m_wasThrusterActive)
                {
                    m_wasThrusterActive = false;
                    Data.isUsingThrusters = false;
                    AstronautEvents.TriggerThrusterStateChanged(false);
                }
                return;
            }

            bool isUsingThrusters = Data.moveInput.magnitude > 0.1f;
            
            if (isUsingThrusters != m_wasThrusterActive)
            {
                m_wasThrusterActive = isUsingThrusters;
                Data.isUsingThrusters = isUsingThrusters;
                AstronautEvents.TriggerThrusterStateChanged(isUsingThrusters);
            }

            if (isUsingThrusters)
            {
                Vector3 forward = Data.playerCamera.transform.forward;
                Vector3 right = Data.playerCamera.transform.right;
                Vector3 up = Data.playerCamera.transform.up;
                
                Vector3 thrusterDirection = forward * Data.moveInput.z + 
                                           right * Data.moveInput.x + 
                                           up * Data.moveInput.y;
                
                if (thrusterDirection.magnitude > 0.1f)
                {
                    thrusterDirection.Normalize();
                }
                
                Data.rb.AddForce(thrusterDirection * Data.thrusterForce, ForceMode.Acceleration);
                
                Vector3 currentVelocity = Data.rb.velocity;
                if (currentVelocity.magnitude > Data.maxVelocity)
                {
                    Data.rb.velocity = currentVelocity.normalized * Data.maxVelocity;
                }
            }
        }
        
        private void HandleAutoStop()
        {
            // 如果玩家正在使用推进器、正在同步速度或正在自动巡航，不应用自动停止
            if (Data.isUsingThrusters || Data.isSyncing || Data.isAutoCruising) return;
            
            Vector3 currentVelocity = Data.rb.velocity;
            float currentSpeed = currentVelocity.magnitude;
            
            if (currentSpeed > 0 && currentSpeed < AUTO_STOP_THRESHOLD)
            {
                Vector3 newVelocity = currentVelocity * AUTO_STOP_RATE;
                
                if (newVelocity.magnitude < 0.01f)
                {
                    newVelocity = Vector3.zero;
                }
                
                Data.rb.velocity = newVelocity;
            }
        }
        
        private void HandleRotation()
        {
            if (Data.currentFuel <= 0) return;

            // 在自动巡航状态下，相机可以移动但不改变朝向
            if (Data.isAutoCruising)
            {
                // 允许相机在自动巡航时进行有限的视角调整，但不影响朝向
                if (!Data.isRolling)
                {
                    // 只允许小幅度的视角调整，不影响整体朝向
                    float limitedLookX = Data.lookInput.x * 0.1f; // 限制旋转幅度
                    float limitedLookY = Data.lookInput.y * 0.1f;
                    
                    Data.rb.transform.Rotate(Vector3.up * limitedLookX);
                    Data.rb.transform.Rotate(Vector3.right * -limitedLookY);
                }
                else
                {
                    // 视线轴旋转模式在自动巡航时完全禁用
                    // 不执行任何旋转操作
                }
                return;
            }

            if (!Data.isRolling)
            {
                Data.rb.transform.Rotate(Vector3.up * Data.lookInput.x);
                Data.rb.transform.Rotate(Vector3.right * -Data.lookInput.y);
                
                m_lastCameraForward = Data.playerCamera.transform.forward;
            }
            else
            {
                if (Mathf.Abs(Data.rollInput) > 0.01f)
                {
                    Vector3 cameraForward = Data.playerCamera.transform.forward;
                    Quaternion rollRotation = Quaternion.AngleAxis(Data.rollInput, cameraForward);
                    Data.rb.transform.rotation = rollRotation * Data.rb.transform.rotation;
                    
                    Vector3 newCameraForward = Data.playerCamera.transform.forward;
                    if (Vector3.Dot(newCameraForward, m_lastCameraForward) < 0.999f)
                    {
                        Quaternion correctionRotation = Quaternion.FromToRotation(newCameraForward, m_lastCameraForward);
                        Data.playerCamera.transform.rotation = correctionRotation * Data.playerCamera.transform.rotation;
                    }
                }
            }
        }
    }
} 