using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;

namespace ClientUtils
{
    public interface IEventInfo 
    {
        void Clear();
    }
    
    public class EventInfo<T> : IEventInfo
    {
        public UnityAction<T> Actions { get; set; }
        
        public EventInfo(UnityAction<T> action)
        {
            Actions = action;
        }
        
        public void Clear()
        {
            if (Actions == null) return;
            
            System.Delegate[] acts = Actions.GetInvocationList();
            for (int i = 0; i < acts.Length; i++)
            {
                Actions -= acts[i] as UnityAction<T>;
            }
        }
    }

    public class EventInfo<T0, T1> : IEventInfo
    {
        public UnityAction<T0, T1> Actions { get; set; }
        
        public EventInfo(UnityAction<T0, T1> action)
        {
            Actions = action;
        }
        
        public void Clear()
        {
            if (Actions == null) return;
            
            System.Delegate[] acts = Actions.GetInvocationList();
            for (int i = 0; i < acts.Length; i++)
            {
                Actions -= acts[i] as UnityAction<T0, T1>;
            }
        }
    }

    public class EventInfo : IEventInfo
    {
        public UnityAction Actions { get; set; }
        
        public EventInfo(UnityAction action)
        {
            Actions = action;
        }
        
        public void Clear()
        {
            if (Actions == null) return;
            
            System.Delegate[] acts = Actions.GetInvocationList();
            for (int i = 0; i < acts.Length; i++)
            {
                Actions -= acts[i] as UnityAction;
            }
        }
    }
    
    public class EventDispatcher
    {
        public EventDispatcher() => EventHelper.Instance.AddDispatcher("default" ,this);
        public EventDispatcher(string dispatcherName) => EventHelper.Instance.AddDispatcher(dispatcherName ,this);
        private readonly Dictionary<string, IEventInfo> m_events = new Dictionary<string, IEventInfo>();
        public void AddEventListener<T>(string name, UnityAction<T> action)
        {
            if (string.IsNullOrEmpty(name) || action == null)
                return;
                
            if (m_events.TryGetValue(name, out IEventInfo existingEvent))
            {
                if (existingEvent is EventInfo<T> typedEvent)
                {
                    typedEvent.Actions += action;
                }
                else
                {
                    Debug.LogError($"Event type mismatch for event '{name}'. Expected EventInfo<{typeof(T).Name}>, got {existingEvent.GetType().Name}");
                }
            }
            else
            {
                m_events.Add(name, new EventInfo<T>(action));
            }
        }

        public void AddEventListener<T0, T1>(string name, UnityAction<T0, T1> action)
        {
            if (string.IsNullOrEmpty(name) || action == null)
                return;
                
            if (m_events.TryGetValue(name, out IEventInfo existingEvent))
            {
                if (existingEvent is EventInfo<T0, T1> typedEvent)
                {
                    typedEvent.Actions += action;
                }
                else
                {
                    Debug.LogError($"Event type mismatch for event '{name}'. Expected EventInfo<{typeof(T0).Name}, {typeof(T1).Name}>, got {existingEvent.GetType().Name}");
                }
            }
            else
            {
                m_events.Add(name, new EventInfo<T0, T1>(action));
            }
        }

        public void AddEventListener(string name, UnityAction action)
        {
            if (string.IsNullOrEmpty(name) || action == null)
                return;
                
            if (m_events.TryGetValue(name, out IEventInfo existingEvent))
            {
                if (existingEvent is EventInfo typedEvent)
                {
                    typedEvent.Actions += action;
                }
                else
                {
                    Debug.LogError($"Event type mismatch for event '{name}'. Expected EventInfo, got {existingEvent.GetType().Name}");
                }
            }
            else
            {
                m_events.Add(name, new EventInfo(action));
            }
        }

        public void EventTrigger<T>(string name, T info)
        {
            if (string.IsNullOrEmpty(name) || !m_events.TryGetValue(name, out IEventInfo eventInfo))
                return;
                
            if (eventInfo is EventInfo<T> typedEvent && typedEvent.Actions != null)
            {
                typedEvent.Actions.Invoke(info);
            }
            else
            {
                Debug.LogError($"Event type mismatch for event '{name}'. Expected EventInfo<{typeof(T).Name}>, got {eventInfo.GetType().Name}");
            }
        }

        public void EventTrigger<T0, T1>(string name, T0 info1, T1 info2)
        {
            if (string.IsNullOrEmpty(name) || !m_events.TryGetValue(name, out IEventInfo eventInfo))
                return;
                
            if (eventInfo is EventInfo<T0, T1> typedEvent && typedEvent.Actions != null)
            {
                typedEvent.Actions.Invoke(info1, info2);
            }
            else
            {
                Debug.LogError($"Event type mismatch for event '{name}'. Expected EventInfo<{typeof(T0).Name}, {typeof(T1).Name}>, got {eventInfo.GetType().Name}");
            }
        }

        public void EventTrigger(string name)
        {
            if (string.IsNullOrEmpty(name) || !m_events.TryGetValue(name, out IEventInfo eventInfo))
                return;
                
            if (eventInfo is EventInfo typedEvent && typedEvent.Actions != null)
            {
                typedEvent.Actions.Invoke();
            }
            else
            {
                Debug.LogError($"Event type mismatch for event '{name}'. Expected EventInfo, got {eventInfo.GetType().Name}");
            }
        }

        public void RemoveEventListener<T>(string name, UnityAction<T> action)
        {
            if (string.IsNullOrEmpty(name) || action == null || !m_events.TryGetValue(name, out IEventInfo eventInfo))
                return;
                
            if (eventInfo is EventInfo<T> typedEvent)
            {
                typedEvent.Actions -= action;
            }
            else
            {
                Debug.LogError($"Event type mismatch for event '{name}'. Expected EventInfo<{typeof(T).Name}>, got {eventInfo.GetType().Name}");
            }
        }

        public void RemoveEventListener<T0, T1>(string name, UnityAction<T0, T1> action)
        {
            if (string.IsNullOrEmpty(name) || action == null || !m_events.TryGetValue(name, out IEventInfo eventInfo))
                return;
                
            if (eventInfo is EventInfo<T0, T1> typedEvent)
            {
                typedEvent.Actions -= action;
            }
            else
            {
                Debug.LogError($"Event type mismatch for event '{name}'. Expected EventInfo<{typeof(T0).Name}, {typeof(T1).Name}>, got {eventInfo.GetType().Name}");
            }
        }

        public void RemoveEventListener(string name, UnityAction action)
        {
            if (string.IsNullOrEmpty(name) || action == null || !m_events.TryGetValue(name, out IEventInfo eventInfo))
                return;
                
            if (eventInfo is EventInfo typedEvent)
            {
                typedEvent.Actions -= action;
            }
            else
            {
                Debug.LogError($"Event type mismatch for event '{name}'. Expected EventInfo, got {eventInfo.GetType().Name}");
            }
        }

        public void ClearEventListener(string name)
        {
            if (string.IsNullOrEmpty(name) || !m_events.TryGetValue(name, out IEventInfo eventInfo))
                return;
                
            eventInfo.Clear();
        }

        public void ClearEventListener<T>(string name)
        {
            if (string.IsNullOrEmpty(name) || !m_events.TryGetValue(name, out IEventInfo eventInfo))
                return;
                
            if (eventInfo is EventInfo<T>)
            {
                eventInfo.Clear();
            }
            else
            {
                Debug.LogError($"Event type mismatch for event '{name}'. Expected EventInfo<{typeof(T).Name}>, got {eventInfo.GetType().Name}");
            }
        }

        public void ClearEventListener<T0, T1>(string name)
        {
            if (string.IsNullOrEmpty(name) || !m_events.TryGetValue(name, out IEventInfo eventInfo))
                return;
                
            if (eventInfo is EventInfo<T0, T1>)
            {
                eventInfo.Clear();
            }
            else
            {
                Debug.LogError($"Event type mismatch for event '{name}'. Expected EventInfo<{typeof(T0).Name}, {typeof(T1).Name}>, got {eventInfo.GetType().Name}");
            }
        }
        
        public void Clear()
        {
            m_events.Clear();
        }

        public Dictionary<string, IEventInfo> GetEvents()
        {
            return new Dictionary<string, IEventInfo>(m_events);
        }

        public bool HasEvent(string name)
        {
            return !string.IsNullOrEmpty(name) && m_events.ContainsKey(name);
        }

        public bool HasEvent<T>(string name)
        {
            return !string.IsNullOrEmpty(name) && 
                   m_events.TryGetValue(name, out IEventInfo eventInfo) && 
                   eventInfo is EventInfo<T>;
        }

        public bool HasEvent<T0, T1>(string name)
        {
            return !string.IsNullOrEmpty(name) && 
                   m_events.TryGetValue(name, out IEventInfo eventInfo) && 
                   eventInfo is EventInfo<T0, T1>;
        }
    }

    public class EventHelper
    {
        private static EventHelper m_instance;
        public static EventHelper Instance
        {
            get
            {
                if (m_instance == null)
                    m_instance = new EventHelper();
                return m_instance;
            }
        }

        private readonly Dictionary<string, EventDispatcher> m_dispatchers = new Dictionary<string, EventDispatcher>();
        private EventDispatcher m_defaultDispatcher;

        private EventHelper()
        {
            m_defaultDispatcher = new EventDispatcher();
        }

        public EventDispatcher GetDispatcher(string name = "default")
        {
            if (string.IsNullOrEmpty(name))
                name = "default";

            if (!m_dispatchers.TryGetValue(name, out EventDispatcher dispatcher))
            {
                dispatcher = new EventDispatcher();
                m_dispatchers.Add(name, dispatcher);
            }

            return dispatcher;
        }

        public EventDispatcher DefaultDispatcher => m_defaultDispatcher;

        public void AddDispatcher(string name, EventDispatcher dispatcher)
        {
            if (string.IsNullOrEmpty(name))
                return;

            if (!m_dispatchers.ContainsKey(name))    
                m_dispatchers.Add(name, dispatcher);
        }

        public void RemoveDispatcher(string name)
        {
            if (string.IsNullOrEmpty(name) || name == "default")
                return;

            if (m_dispatchers.TryGetValue(name, out EventDispatcher dispatcher))
            {
                dispatcher.Clear();
                m_dispatchers.Remove(name);
            }
        }

        public void ClearAllDispatchers()
        {
            foreach (var dispatcher in m_dispatchers.Values)
            {
                dispatcher.Clear();
            }
            m_dispatchers.Clear();
            m_defaultDispatcher.Clear();
        }

        public List<string> GetDispatcherNames()
        {
            return new List<string>(m_dispatchers.Keys);
        }

        public bool HasDispatcher(string name)
        {
            return !string.IsNullOrEmpty(name) && m_dispatchers.ContainsKey(name);
        }

        public int GetDispatcherCount()
        {
            return m_dispatchers.Count + 1; // +1 for default dispatcher
        }

        public void AddEventListener<T>(string eventName, UnityAction<T> action, string dispatcherName = "default")
        {
            GetDispatcher(dispatcherName).AddEventListener(eventName, action);
        }

        public void AddEventListener<T0, T1>(string eventName, UnityAction<T0, T1> action, string dispatcherName = "default")
        {
            GetDispatcher(dispatcherName).AddEventListener(eventName, action);
        }

        public void AddEventListener(string eventName, UnityAction action, string dispatcherName = "default")
        {
            GetDispatcher(dispatcherName).AddEventListener(eventName, action);
        }

        public void EventTrigger<T>(string eventName, T info, string dispatcherName = "default")
        {
            GetDispatcher(dispatcherName).EventTrigger(eventName, info);
        }

        public void EventTrigger<T0, T1>(string eventName, T0 info1, T1 info2, string dispatcherName = "default")
        {
            GetDispatcher(dispatcherName).EventTrigger(eventName, info1, info2);
        }

        public void EventTrigger(string eventName, string dispatcherName = "default")
        {
            GetDispatcher(dispatcherName).EventTrigger(eventName);
        }

        public void RemoveEventListener<T>(string eventName, UnityAction<T> action, string dispatcherName = "default")
        {
            GetDispatcher(dispatcherName).RemoveEventListener(eventName, action);
        }

        public void RemoveEventListener<T0, T1>(string eventName, UnityAction<T0, T1> action, string dispatcherName = "default")
        {
            GetDispatcher(dispatcherName).RemoveEventListener(eventName, action);
        }

        public void RemoveEventListener(string eventName, UnityAction action, string dispatcherName = "default")
        {
            GetDispatcher(dispatcherName).RemoveEventListener(eventName, action);
        }

        public void ClearEventListener(string eventName, string dispatcherName = "default")
        {
            GetDispatcher(dispatcherName).ClearEventListener(eventName);
        }

        public void ClearEventListener<T>(string eventName, string dispatcherName = "default")
        {
            GetDispatcher(dispatcherName).ClearEventListener<T>(eventName);
        }

        public void ClearEventListener<T0, T1>(string eventName, string dispatcherName = "default")
        {
            GetDispatcher(dispatcherName).ClearEventListener<T0, T1>(eventName);
        }

        public bool HasEvent(string eventName, string dispatcherName = "default")
        {
            return GetDispatcher(dispatcherName).HasEvent(eventName);
        }

        public bool HasEvent<T>(string eventName, string dispatcherName = "default")
        {
            return GetDispatcher(dispatcherName).HasEvent<T>(eventName);
        }

        public bool HasEvent<T0, T1>(string eventName, string dispatcherName = "default")
        {
            return GetDispatcher(dispatcherName).HasEvent<T0, T1>(eventName);
        }
    }
}