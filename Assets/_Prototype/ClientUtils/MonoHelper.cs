using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ClientUtils
{
    public class SimpleMono : MonoBehaviour
    {
        private event UnityAction m_updateEvent;
        
        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            m_updateEvent?.Invoke();
        }

        public void AddUpdateListener(UnityAction func)
        {
            m_updateEvent += func;
        }

        public void RemoveUpdateListener(UnityAction func)
        {
            m_updateEvent -= func;
        }
    }
    
    public class MonoHelper
    {
        private static MonoHelper m_instance;
        public static MonoHelper Instance
        {
            get
            {
                if (m_instance == null)
                    m_instance = new MonoHelper();
                return m_instance;
            }
        }
        
        public SimpleMono Controller { get; private set; }
        public Dictionary<string, GameObject> GlobalObjects { get; private set; }
        
        public MonoHelper()
        {
            GameObject obj = new GameObject("SimpleMono");
            Controller = obj.AddComponent<SimpleMono>();
            GlobalObjects = new Dictionary<string, GameObject>();
        }

        public void AddUpdateListener(UnityAction func)
        {
            Controller?.AddUpdateListener(func);
        }

        public void RemoveUpdateListener(UnityAction func)
        {
            Controller?.RemoveUpdateListener(func);
        }

        public Coroutine StartCoroutine(IEnumerator routine)
        {
            return Controller?.StartCoroutine(routine);
        }

        public void StopCoroutine(Coroutine routine)
        {
            Controller?.StopCoroutine(routine);
        }

        public Coroutine StartCoroutine(string methodName)
        {
            return Controller?.StartCoroutine(methodName);
        }

        public void TryGetMonoObject(string name, out GameObject obj)
        {
            if (string.IsNullOrEmpty(name))
            {
                obj = null;
                return;
            }
            
            if (!GlobalObjects.ContainsKey(name))
            {
                GameObject foundObj = GameObject.Find(name);
                GlobalObjects.Add(name, foundObj);
            }
            obj = GlobalObjects[name];
        }
    }
}
