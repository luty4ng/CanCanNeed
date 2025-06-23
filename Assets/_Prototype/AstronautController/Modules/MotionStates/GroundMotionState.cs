using UnityEngine;
using PlayerController.Modules;

namespace PlayerController.Modules.MotionStates
{
    public class GroundMotionState : BaseMotionState
    {
        private bool m_isRunning;
        private bool m_wasGrounded;
        private bool m_isAligningToGround;
        private Quaternion m_targetGroundRotation;
        private RigidbodyConstraints m_originalConstraints;
        private float m_cameraPitch;

        public override string StateName => "GroundMotion";

        protected override void OnStateEnter()
        {
            m_originalConstraints = Data.rb.constraints;
            m_wasGrounded = Data.isOnGround;
            m_isRunning = false;
            m_isAligningToGround = false;
            m_targetGroundRotation = Quaternion.identity;
            
            Data.rb.useGravity = true;
            Data.rb.drag = 1f;
            Data.rb.angularDrag = 0.05f;
            if (Data.playerCamera != null)
            {
                m_cameraPitch = Data.playerCamera.transform.localEulerAngles.x;
            }
        }

        protected override void OnStateExit()
        {
            m_isAligningToGround = false;
        }

        protected override void OnStateUpdate()
        {
            HandleInput();
            HandleCameraLook();
            HandleGroundAlignment();
            CheckGroundStateChange();
        }

        protected override void OnStateFixedUpdate()
        {
            HandleGroundMovement();
            HandleJump();
        }

        private void HandleInput()
        {
            m_isRunning = Input.GetKey(KeyCode.LeftShift);
        }

        private void HandleGroundMovement()
        {
            if (!Data.isInGravityField || !Data.isOnGround) return;

            Vector3 moveInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

            if (moveInput.magnitude > 0.1f)
            {
                float currentSpeed = GetCurrentSpeed();
                Vector3 moveDirection = Data.rb.transform.TransformDirection(moveInput.normalized);
                Vector3 movement = moveDirection * currentSpeed * Time.fixedDeltaTime;
                Data.transform.position += movement;

                AstronautEvents.TriggerGroundMovement(moveDirection, currentSpeed);
            }
        }

        private void HandleJump()
        {
            // if (!Data.isInGravityField || !Data.isOnGround) return;

            if (Data.isJumpingRequested)
                PerformJump();
        }

        private void PerformJump()
        {
            Vector3 jumpDirection = -Data.gravityDirection;
            Vector3 jumpForceVector = jumpDirection * (Data.jumpForce + Data.gravityStrength);
            Data.rb.AddForce(jumpForceVector, ForceMode.Impulse);
            AstronautEvents.TriggerJump(Data.jumpForce);
            Debug.Log($"跳跃! 力度: {Data.jumpForce}");
        }

        private float GetCurrentSpeed()
        {
            return m_isRunning ? Data.runSpeed : Data.walkSpeed;
        }

        private void CheckGroundStateChange()
        {
            if (Data.isOnGround != m_wasGrounded)
            {
                m_wasGrounded = Data.isOnGround;

                if (Data.isOnGround)
                {
                    AstronautEvents.TriggerLanded();
                    StartGroundAlignment();
                }
                else
                {
                    AstronautEvents.TriggerLeftGround();
                    EndGroundAlignment();
                }
            }
        }

        private void HandleGroundAlignment()
        {
            if (!Data.isInGravityField || !Data.isOnGround) return;

            if (m_isAligningToGround)
            {
                Data.rb.transform.rotation = Quaternion.Slerp(
                    Data.rb.transform.rotation,
                    m_targetGroundRotation,
                    Data.groundAlignmentSpeed * Time.deltaTime
                );

                float angleDifference = Quaternion.Angle(Data.rb.transform.rotation, m_targetGroundRotation);
                if (angleDifference < Data.groundAlignmentThreshold)
                {
                    Data.rb.transform.rotation = m_targetGroundRotation;
                    m_isAligningToGround = false;
                    UnlockRotationAxes();
                }
            }
        }

        private void StartGroundAlignment()
        {
            if (!Data.isInGravityField) return;

            Vector3 upDirection = -Data.gravityDirection;
            Vector3 forwardDirection = Data.rb.transform.forward;

            forwardDirection = Vector3.ProjectOnPlane(forwardDirection, upDirection).normalized;
            if (forwardDirection.magnitude < 0.1f)
            {
                forwardDirection = Vector3.Cross(upDirection, Vector3.right).normalized;
                if (forwardDirection.magnitude < 0.1f)
                {
                    forwardDirection = Vector3.Cross(upDirection, Vector3.forward).normalized;
                }
            }

            m_targetGroundRotation = Quaternion.LookRotation(forwardDirection, upDirection);
            m_isAligningToGround = true;

            LockRotationAxes();
        }

        private void EndGroundAlignment()
        {
            m_isAligningToGround = false;
            UnlockRotationAxes();
        }

        private void LockRotationAxes()
        {
            Data.rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        private void UnlockRotationAxes()
        {
            Data.rb.constraints = m_originalConstraints;
        }

        private void HandleCameraLook()
        {
            if (Data.playerCamera == null) return;

            float yaw = Data.lookInput.x;
            Data.rb.transform.Rotate(Vector3.up * yaw);

            float pitchDelta = -Data.lookInput.y;
            m_cameraPitch += pitchDelta;
            m_cameraPitch = Mathf.Clamp(m_cameraPitch, -80f, 80f);

            Vector3 camEuler = Data.playerCamera.transform.localEulerAngles;
            Data.playerCamera.transform.localEulerAngles = new Vector3(m_cameraPitch, 0, 0);
        }
    }
} 