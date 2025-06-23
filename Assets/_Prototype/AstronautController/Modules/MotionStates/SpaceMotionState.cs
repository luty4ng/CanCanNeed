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
            if (Data.isUsingThrusters || Data.isSyncing) return;
            
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