using System;
using UnityEngine;
using PlayerController.Modules;
using PlayerController.Modules.MotionStates;
using ClientUtils;

namespace PlayerController.Modules
{
    [Serializable]
    [ModuleDisplayName("移动模块")]
    public class MotionModule : AstronautModuleBase
    {
        private StateMachine m_stateMachine;
        private SpaceMotionState m_spaceMotionState;
        private GroundMotionState m_groundMotionState;
        private SwimmingMotionState m_swimmingMotionState;

        public override void Initialize(AstronautData data)
        {
            base.Initialize(data);
            
            InitializeStateMachine();
            SetupTransitions();
            
            SetInitialState();
        }

        private void InitializeStateMachine()
        {
            m_stateMachine = new StateMachine();
            
            m_spaceMotionState = new SpaceMotionState();
            m_groundMotionState = new GroundMotionState();
            m_swimmingMotionState = new SwimmingMotionState();
            
            m_spaceMotionState.Initialize(Data, this);
            m_groundMotionState.Initialize(Data, this);
            m_swimmingMotionState.Initialize(Data, this);
        }

        private void SetupTransitions()
        {
            SetupSpaceToGroundTransition();
            SetupGroundToSpaceTransition();
            SetupAnyToSpaceTransition();
            SetupAnyToGroundTransition();
            SetupSwimmingTransitions();
        }

        private void SetupSpaceToGroundTransition()
        {
            System.Func<bool> spaceToGroundCondition = () => 
                Data.isInGravityField && Data.isOnGround && !Data.isUsingThrusters;
            
            m_stateMachine.AddTransition(m_spaceMotionState, m_groundMotionState, spaceToGroundCondition);
        }

        private void SetupGroundToSpaceTransition()
        {
            System.Func<bool> groundToSpaceCondition = () => 
                !Data.isInGravityField || !Data.isOnGround;
            
            m_stateMachine.AddTransition(m_groundMotionState, m_spaceMotionState, groundToSpaceCondition);
        }

        private void SetupAnyToSpaceTransition()
        {
            System.Func<bool> anyToSpaceCondition = () => 
                !Data.isInGravityField;
            
            m_stateMachine.AddAnyTransition(m_spaceMotionState, anyToSpaceCondition);
        }

        private void SetupAnyToGroundTransition()
        {
            System.Func<bool> anyToGroundCondition = () => 
                Data.isInGravityField && Data.isOnGround && !Data.isUsingThrusters;
            
            m_stateMachine.AddAnyTransition(m_groundMotionState, anyToGroundCondition);
        }

        private void SetupSwimmingTransitions()
        {
            System.Func<bool> toSwimmingCondition = () => 
                Data.isInWater && !Data.isOnGround;
            
            System.Func<bool> fromSwimmingCondition = () => 
                !Data.isInWater;
            
            m_stateMachine.AddAnyTransition(m_swimmingMotionState, toSwimmingCondition);
            m_stateMachine.AddTransition(m_swimmingMotionState, m_spaceMotionState, fromSwimmingCondition);
            m_stateMachine.AddTransition(m_swimmingMotionState, m_groundMotionState, () => 
                Data.isInGravityField && Data.isOnGround);
        }

        private void SetInitialState()
        {
            if (Data.isInWater)
            {
                m_stateMachine.SetState(m_swimmingMotionState);
            }
            else if (Data.isInGravityField && Data.isOnGround)
            {
                m_stateMachine.SetState(m_groundMotionState);
            }
            else
            {
                m_stateMachine.SetState(m_spaceMotionState);
            }
        }

        public override void OnUpdate()
        {
            m_stateMachine.Update();
        }

        public override void OnFixedUpdate()
        {
            m_stateMachine.FixedUpdate();
        }

        public bool IsInSpaceMotion()
        {
            return m_stateMachine.IsInState<SpaceMotionState>();
        }

        public bool IsInGroundMotion()
        {
            return m_stateMachine.IsInState<GroundMotionState>();
        }

        public bool IsInSwimmingMotion()
        {
            return m_stateMachine.IsInState<SwimmingMotionState>();
        }

        public string GetCurrentMotionState()
        {
            return m_stateMachine.CurrentStateType?.Name ?? "None";
        }

        public void ForceStateToSpace()
        {
            m_stateMachine.SetState(m_spaceMotionState);
        }

        public void ForceStateToGround()
        {
            m_stateMachine.SetState(m_groundMotionState);
        }

        public void ForceStateToSwimming()
        {
            m_stateMachine.SetState(m_swimmingMotionState);
        }

        public override void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            DrawMotionStateInfo();
        }

        private void DrawMotionStateInfo()
        {
            string currentState = GetCurrentMotionState();
            
            Vector3 gizmoPosition = Data.transform.position + Vector3.up * 2f;
            
            if (IsInSpaceMotion())
            {
                Gizmos.color = Color.cyan;
            }
            else if (IsInGroundMotion())
            {
                Gizmos.color = Color.green;
            }
            else if (IsInSwimmingMotion())
            {
                Gizmos.color = Color.blue;
            }
            else
            {
                Gizmos.color = Color.white;
            }
            
            Gizmos.DrawWireSphere(gizmoPosition, 0.3f);
            
            Gizmos.color = Color.white;
            Gizmos.DrawLine(gizmoPosition, gizmoPosition + Vector3.up * 0.5f);
        }
    }
} 