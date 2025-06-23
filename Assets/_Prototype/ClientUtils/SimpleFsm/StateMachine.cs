using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClientUtils
{
    public interface IState
    {
        void Update();
        void OnEnter();
        void OnExit();
        void OnFixedUpdate();
    }

    public class AnyState : IState
    {
        public void Update() { }
        public void OnEnter() { }
        public void OnExit() { }
        public void OnFixedUpdate() { }
    }

    public class StateMachine
    {
        private class Transition
        {
            public string Id { get; }
            public System.Func<bool> Condition { get; }
            public IState From { get; }
            public IState To { get; }
            public bool IsActive { get; set; }
            public float Duration { get; set; }
            public float ExitTime { get; set; }
            public float Possibility { get; set; }

            public Transition(IState from, IState to, System.Func<bool> condition, float duration = 0, float exitTime = 0, float possibility = 1, bool active = true)
            {
                Id = $"{from}-{to}";
                From = from;
                To = to;
                Condition = condition;
                Duration = Mathf.Max(0, duration);
                ExitTime = Mathf.Max(0, exitTime);
                Possibility = Mathf.Clamp01(possibility);
                IsActive = active;
            }
        }

        private bool m_isExit = false;
        private float m_nextDuration = 0;
        private float m_startTime;
        private IState m_currentState;
        private readonly Dictionary<System.Type, List<Transition>> m_transitions = new Dictionary<System.Type, List<Transition>>();
        private List<Transition> m_currentTransitions = new List<Transition>();
        private readonly List<Transition> m_anyTransitions = new List<Transition>();
        private static readonly List<Transition> EmptyTransitions = new List<Transition>(0);

        public System.Type CurrentStateType => m_currentState?.GetType();
        public bool IsExiting => m_isExit;
        public IState CurrentState => m_currentState;

        public void Update()
        {
            if (Time.fixedTime - m_startTime < m_nextDuration && m_nextDuration > 0)
                return;

            var transition = GetTransition();
            if (transition != null && !m_isExit)
            {
                if (transition.Duration > 0)
                {
                    m_startTime = Time.fixedTime;
                    m_nextDuration = transition.Duration;
                }
                else
                {
                    m_nextDuration = 0;
                }

                if (transition.ExitTime <= 0)
                {
                    SetState(transition.To);
                }
                else
                {
                    m_isExit = true;
                    Debug.LogWarning("Delayed transition not supported without MonoHelper");
                    SetState(transition.To);
                }
            }

            m_currentState?.Update();
        }

        public void FixedUpdate()
        {
            m_currentState?.OnFixedUpdate();
        }

        public IState GetCurrentState()
        {
            return m_currentState;
        }

        public void SetState(IState state)
        {
            if (state == null)
            {
                Debug.LogWarning("Attempting to set null state");
                return;
            }

            if (state == m_currentState)
                return;

            m_currentState?.OnExit();
            m_currentState = state;

            m_transitions.TryGetValue(m_currentState.GetType(), out m_currentTransitions);
            if (m_currentTransitions == null)
                m_currentTransitions = EmptyTransitions;

            m_currentState.OnEnter();
        }

        public void AddTransition(IState from, IState to, System.Func<bool> predicate, float duration = 0, float exitTime = 0, float possibility = 1, bool active = true, bool randDuration = false)
        {
            if (from == null || to == null)
            {
                Debug.LogError("Cannot add transition with null states");
                return;
            }

            if (predicate == null)
            {
                Debug.LogError("Transition condition cannot be null");
                return;
            }

            if (randDuration)
                duration = Random.Range(0.5f, 3f);

            if (!m_transitions.ContainsKey(from.GetType()))
            {
                m_transitions.Add(from.GetType(), new List<Transition>());
            }
            m_transitions[from.GetType()].Add(new Transition(from, to, predicate, duration, exitTime, possibility, active));
        }

        public void AddAnyTransition(IState to, System.Func<bool> predicate, float duration = 0, float exitTime = 0, float possibility = 1, bool active = true, bool randDuration = false)
        {
            if (to == null)
            {
                Debug.LogError("Cannot add any transition with null target state");
                return;
            }

            if (predicate == null)
            {
                Debug.LogError("Transition condition cannot be null");
                return;
            }

            if (randDuration)
                duration = Random.Range(0.5f, 3f);

            IState anyState = new AnyState();
            m_anyTransitions.Add(new Transition(anyState, to, predicate, duration, exitTime, possibility, active));
        }

        public bool IsInState<T>() where T : IState
        {
            return m_currentState is T;
        }

        public T GetCurrentStateAs<T>() where T : class, IState
        {
            return m_currentState as T;
        }

        private Transition GetTransition()
        {
            foreach (var transition in m_anyTransitions)
            {
                if (!transition.IsActive)
                    continue;

                try
                {
                    if (transition.Condition())
                    {
                        if (Random.Range(0f, 1f) < transition.Possibility && transition.To != m_currentState)
                        {
                            return transition;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in transition condition: {e.Message}");
                }
            }

            foreach (var transition in m_currentTransitions)
            {
                if (!transition.IsActive)
                    continue;

                try
                {
                    if (transition.Condition())
                    {
                        if (Random.Range(0f, 1f) < transition.Possibility)
                        {
                            return transition;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in transition condition: {e.Message}");
                }
            }
            return null;
        }

        public void SetTransitionActive(string id, bool isActive)
        {
            if (string.IsNullOrEmpty(id))
                return;

            foreach (var transition in m_anyTransitions)
            {
                if (transition.Id == id)
                {
                    transition.IsActive = isActive;
                    return;
                }
            }

            foreach (var trans in m_transitions)
            {
                foreach (var transition in trans.Value)
                {
                    if (transition.Id == id)
                    {
                        transition.IsActive = isActive;
                        return;
                    }
                }
            }
        }

        public void SetTransitionDuration(string id, float duration)
        {
            if (string.IsNullOrEmpty(id))
                return;

            foreach (var transition in m_anyTransitions)
            {
                if (transition.Id == id)
                {
                    transition.Duration = Mathf.Max(0, duration);
                    return;
                }
            }

            foreach (var trans in m_transitions)
            {
                foreach (var transition in trans.Value)
                {
                    if (transition.Id == id)
                    {
                        transition.Duration = Mathf.Max(0, duration);
                        return;
                    }
                }
            }
        }

        public void SetTransitionExitTime(string id, float exitTime)
        {
            if (string.IsNullOrEmpty(id))
                return;

            foreach (var transition in m_anyTransitions)
            {
                if (transition.Id == id)
                {
                    transition.ExitTime = Mathf.Max(0, exitTime);
                    return;
                }
            }

            foreach (var trans in m_transitions)
            {
                foreach (var transition in trans.Value)
                {
                    if (transition.Id == id)
                    {
                        transition.ExitTime = Mathf.Max(0, exitTime);
                        return;
                    }
                }
            }
        }

        public void SetTransitionPossibility(string id, float possibility)
        {
            if (string.IsNullOrEmpty(id))
                return;

            foreach (var transition in m_anyTransitions)
            {
                if (transition.Id == id)
                {
                    transition.Possibility = Mathf.Clamp01(possibility);
                    return;
                }
            }

            foreach (var trans in m_transitions)
            {
                foreach (var transition in trans.Value)
                {
                    if (transition.Id == id)
                    {
                        transition.Possibility = Mathf.Clamp01(possibility);
                        return;
                    }
                }
            }
        }

        public void ClearTransitions()
        {
            m_transitions.Clear();
            m_anyTransitions.Clear();
            m_currentTransitions = EmptyTransitions;
        }

        public int GetTransitionCount()
        {
            int count = m_anyTransitions.Count;
            foreach (var trans in m_transitions)
            {
                count += trans.Value.Count;
            }
            return count;
        }
    }
}
