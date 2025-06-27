using System.Collections.Generic;
using System.Linq;
using GameLogic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameProcedure
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private string m_gameMainScene = "GameMain";
        [SerializeField] private string m_gameMenuName = "UIStartGameWindow";
        [SerializeField] private Canvas m_mainCanvas;
        [SerializeField] private List<UIViewModelBase> m_mainUiWindows;

        [Header("游戏参数")]
        [SerializeField] private float m_gameDuration = 600f; // 游戏时长（秒）
        [SerializeField] private float m_initialCargoWeight = 5f; // 初始运输重量（kg）
        [SerializeField] private float m_cargoWeightStep = 5f; // 每次递增重量（kg）
        [SerializeField] private float m_maxCargoWeight = 22f; // 最大运输重量（kg）
        [SerializeField] private int m_deliveriesToUnlock = 3; // 解锁能力所需运输次数

        [Header("测试")]
        [SerializeField] private List<GameObject> m_randomItemPool;

        [Button]
        public void GenerateRandomItemInInventory()
        {
            if (m_randomItemPool == null || m_randomItemPool.Count == 0)
            {
                Debug.LogWarning("Random item pool is empty!");
                return;
            }

            var player = AstronautController.Instance;
            if (player == null)
            {
                Debug.LogWarning("Player instance not found!");
                return;
            }

            int randomIndex = Random.Range(0, m_randomItemPool.Count);
            GameObject randomItem = m_randomItemPool[randomIndex];
            player.AddInventoryItem(randomItem);
        }

        public static GameManager Instance { get; private set; }
        public System.Action OnGameStarted;
        public System.Action OnGamePaused;
        public System.Action OnGameResumed;
        public System.Action OnGameEnded;
        
        private float m_gameTimer;
        private float m_currentCargoTarget;
        private float m_playerCarryWeight = 0f;
        private Dictionary<string, int> m_planetDeliverCount = new();
        private HashSet<string> m_unlockedAbilities = new();
        private List<GameObject> m_cachedUiInstances = new ();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                DontDestroyOnLoad(m_mainCanvas);
            }

            var startGameWindow = m_mainUiWindows.Where(x => x.name == m_gameMenuName).First();
            var uiInstance = Instantiate<UIStartGame>(startGameWindow as UIStartGame, m_mainCanvas.transform);
            m_cachedUiInstances.Add(uiInstance.gameObject);
        }
        

        private void Start()
        {
            m_gameTimer = m_gameDuration;
            m_currentCargoTarget = m_initialCargoWeight;
        }

        private void Update()
        {
            if (m_gameTimer > 0)
            {
                m_gameTimer -= Time.deltaTime;
                if (m_gameTimer <= 0)
                {
                    m_gameTimer = 0;
                    OnGameEnded?.Invoke();
                }
            }
        }

        // 玩家完成一次运输
        public void CompleteDelivery(string planetType, float carryWeight)
        {
            if (carryWeight >= m_initialCargoWeight && carryWeight <= m_maxCargoWeight)
            {
                // 运输成功
                m_currentCargoTarget = Mathf.Min(m_currentCargoTarget + m_cargoWeightStep, m_maxCargoWeight);
                if (!m_planetDeliverCount.ContainsKey(planetType))
                    m_planetDeliverCount[planetType] = 0;
                m_planetDeliverCount[planetType]++;
                if (m_planetDeliverCount[planetType] == m_deliveriesToUnlock)
                {
                    UnlockAbility(planetType);
                }
            }
        }

        // 解锁能力
        private void UnlockAbility(string planetType)
        {
            if (!m_unlockedAbilities.Contains(planetType))
            {
                m_unlockedAbilities.Add(planetType);
                // TODO: 触发能力解锁事件或逻辑
                Debug.Log($"解锁{planetType}类item能力");
            }
        }

        // 获取当前运输目标重量
        public float GetCurrentCargoTarget() => m_currentCargoTarget;
        public float GetInitialCargoWeight() => m_initialCargoWeight;
        public float GetCargoWeightStep() => m_cargoWeightStep;
        public float GetMaxCargoWeight() => m_maxCargoWeight;
        public int GetDeliveriesToUnlock() => m_deliveriesToUnlock;
        // 检查某类能力是否已解锁
        public bool IsAbilityUnlocked(string planetType) => m_unlockedAbilities.Contains(planetType);
        // 获取剩余时间
        public float GetTimeLeft() => m_gameTimer;


        public void ShowUI(System.Type uiType)
        {
            foreach (var ui in m_cachedUiInstances)
            {
                if (ui != null && ui.GetComponent(uiType) != null)
                    ui.SetActive(true);
            }
        }

        public void HideUI(System.Type uiType)
        {
            foreach (var ui in m_cachedUiInstances)
            {
                if (ui != null && ui.GetComponent(uiType) != null)
                    ui.SetActive(false);
            }
        }
    }
}