using UnityEngine;
using PlayerController.Modules;

namespace PlayerController.Modules.MotionStates
{
    public abstract class BaseMotionState : ClientUtils.IState
    {
        protected AstronautData Data { get; private set; }
        protected MotionModule MotionModule { get; private set; }

        public virtual void Initialize(AstronautData data, MotionModule motionModule)
        {
            Data = data;
            MotionModule = motionModule;
        }

        public virtual void OnEnter()
        {
            OnStateEnter();
        }

        public virtual void OnExit()
        {
            OnStateExit();
        }

        public virtual void Update()
        {
            OnStateUpdate();
        }

        public virtual void OnFixedUpdate()
        {
            OnStateFixedUpdate();
        }

        protected abstract void OnStateEnter();
        protected abstract void OnStateExit();
        protected abstract void OnStateUpdate();
        protected abstract void OnStateFixedUpdate();

        public abstract string StateName { get; }
    }
} 