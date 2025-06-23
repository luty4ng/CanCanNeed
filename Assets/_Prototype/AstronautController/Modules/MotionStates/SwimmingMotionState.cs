using UnityEngine;
using PlayerController.Modules;

namespace PlayerController.Modules.MotionStates
{
    public class SwimmingMotionState : BaseMotionState
    {
        private bool m_isUnderwater;
        private float m_swimSpeed = 3f;
        private float m_underwaterDrag = 2f;
        private float m_waterResistance = 0.8f;

        public override string StateName => "SwimmingMotion";

        protected override void OnStateEnter()
        {
            Data.rb.useGravity = false;
            Data.rb.drag = m_underwaterDrag;
            Data.rb.angularDrag = 0.5f;
            
            m_isUnderwater = true;
            
            // AstronautEvents.TriggerSwimmingStateChanged(true);
        }

        protected override void OnStateExit()
        {
            Data.rb.useGravity = true;
            Data.rb.drag = 1f;
            Data.rb.angularDrag = 0.05f;
            
            m_isUnderwater = false;
            
            // AstronautEvents.TriggerSwimmingStateChanged(false);
        }

        protected override void OnStateUpdate()
        {
            HandleSwimmingRotation();
        }

        protected override void OnStateFixedUpdate()
        {
            HandleSwimmingMovement();
        }

        private void HandleSwimmingMovement()
        {
            if (Data.currentFuel <= 0) return;

            Vector3 moveInput = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0);
            
            if (moveInput.magnitude > 0.1f)
            {
                Vector3 forward = Data.playerCamera.transform.forward;
                Vector3 right = Data.playerCamera.transform.right;
                Vector3 up = Data.playerCamera.transform.up;
                
                Vector3 swimDirection = forward * moveInput.y + 
                                       right * moveInput.x + 
                                       up * Input.GetAxis("Jump");
                
                if (swimDirection.magnitude > 0.1f)
                {
                    swimDirection.Normalize();
                }
                
                Vector3 swimForce = swimDirection * m_swimSpeed;
                Data.rb.AddForce(swimForce, ForceMode.Acceleration);
                
                Vector3 currentVelocity = Data.rb.velocity;
                if (currentVelocity.magnitude > Data.maxVelocity * m_waterResistance)
                {
                    Data.rb.velocity = currentVelocity.normalized * Data.maxVelocity * m_waterResistance;
                }
            }
        }

        private void HandleSwimmingRotation()
        {
            if (Data.currentFuel <= 0) return;

            if (!Data.isRolling)
            {
                Data.rb.transform.Rotate(Vector3.up * Data.lookInput.x);
                Data.rb.transform.Rotate(Vector3.right * -Data.lookInput.y);
            }
            else
            {
                if (Mathf.Abs(Data.rollInput) > 0.01f)
                {
                    Vector3 cameraForward = Data.playerCamera.transform.forward;
                    Quaternion rollRotation = Quaternion.AngleAxis(Data.rollInput, cameraForward);
                    Data.rb.transform.rotation = rollRotation * Data.rb.transform.rotation;
                }
            }
        }
    }
} 