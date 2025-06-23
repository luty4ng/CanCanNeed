using UnityEngine;
using ClientUtils;

public class StateA : IState
{
    public void Update() => Debug.Log("This is StateA");
    public void OnFixedUpdate() { }
    public void OnEnter() { }
    public void OnExit() { }
}

public class StateB : IState
{
    public void Update() => Debug.Log("This is StateB");
    public void OnFixedUpdate() { }
    public void OnEnter() { }
    public void OnExit() { }
}

public class StateMachineExample : MonoBehaviour
{
    private StateMachine m_stateMachine;
    [SerializeField] private string m_currentState;
    
    private StateA m_stateA;
    private StateB m_stateB;
    
    private void Awake()
    {
        InitializeStateMachine();
    }

    private void InitializeStateMachine()
    {
        m_stateMachine = new StateMachine();
        m_stateA = new StateA();
        m_stateB = new StateB();
        
        System.Func<bool> aToB = () => Random.Range(0f, 1f) >= 0.3f;
        System.Func<bool> bToA = () => Random.Range(0f, 1f) >= 0.6f;
        
        m_stateMachine.AddTransition(m_stateA, m_stateB, aToB);
        m_stateMachine.AddTransition(m_stateB, m_stateA, bToA);
        
        m_stateMachine.SetState(m_stateA);
    }

    private void Update()
    {
        m_stateMachine.Update();
        m_currentState = m_stateMachine.GetCurrentState()?.ToString() ?? "None";
    }
}
