using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using OutOfBounds.Core;

namespace OutOfBounds.UI
{
    /// <summary>
    /// 血条管理器
    /// 管理玩家的生命值和心形UI元素
    /// </summary>
    [ExecuteInEditMode] // 允许在编辑模式下执行Update方法
    public class HealthBarManager : MonoBehaviour
    {
        public static HealthBarManager Instance { get; private set; }

    [Header("血条设置")]
    [SerializeField] public int maxHealth = 3;
    [SerializeField] public Transform healthBarParent;
    [SerializeField] public GameObject heartPrefab;

    [Header("防逃课 — 心形回收")]
    [Tooltip("伤害区域的 Transform — 心形离开此区域太远会被回收")]
    [SerializeField] private Transform damageAreaTransform;

    [Tooltip("心形有效范围半径")]
    [SerializeField] private float heartValidRange = 15f;

    [Tooltip("心形信号衰减起点（0=超范围立刻回收, 1=到最远才开始衰减）")]
    [Range(0f, 1f)]
    [SerializeField] private float heartSignalFadeStart = 0.5f;

        [Header("心形元素设置")]
        [SerializeField] public float heartSize = 50f;
        [SerializeField] public float heartSpacing = 10f;
        [SerializeField] public Sprite fullHeartSprite;
        [SerializeField] public Sprite emptyHeartSprite;
        [SerializeField] public Color fullHeartColor = Color.white; // 默认改为白色，方便Sprite原色显示
        [SerializeField] public Color emptyHeartColor = Color.white;

        // 心形UI元素列表
        private List<GameObject> heartUIs = new List<GameObject>();
        private Stack<GameObject> uiHeartPool = new Stack<GameObject>();
        
        // 编辑模式下的更新标志
        private bool needUpdateInEditMode = false;

        // 防止双重初始化的标志（HealthBarManager.Start + PlayerController.Start 重复调用）
        private bool hasInitialized = false;

        #region Unity 生命周期

        private int currentHealth;

        private void Awake()
        {
            // 单例
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 初始化生命值
            currentHealth = maxHealth;
        }

        private void Start()
        {
            // 创建初始心形UI（只创建一次，防止 PlayerController.InitializeHealthBar 重复调用）
            CreateHeartUI();
            hasInitialized = true;

            // 注册事件
            Events.OnPlayerHealthChanged.Subscribe(OnPlayerHealthChanged);
            Events.OnPlayerDead.Subscribe(OnPlayerDead);
        }

        private void OnValidate()
        {
            // 当在Inspector面板中调整值时，设置更新标志
            if (Application.isEditor && !Application.isPlaying)
            {
                // 确保healthBarParent存在
                if (healthBarParent != null)
                {
                    // 设置更新标志，在Update方法中处理
                    needUpdateInEditMode = true;
                }
            }
        }

        private void Update()
        {
            // 在编辑模式下，检查是否需要更新
            if (Application.isEditor && !Application.isPlaying && needUpdateInEditMode)
            {
                // 重置更新标志
                needUpdateInEditMode = false;
                
                // 重新创建心形UI，以适应maxHealth的变化
                CreateHeartUI();
            }
        }

        private void OnDestroy()
        {
            // 取消订阅事件
            Events.OnPlayerHealthChanged.Unsubscribe(OnPlayerHealthChanged);
            Events.OnPlayerDead.Unsubscribe(OnPlayerDead);
        }

        #endregion

        #region 生命值管理

        /// <summary>
        /// 初始化血条
        /// </summary>
        public void InitializeHealthBar(int health)
        {
            // 已经初始化过了，跳过（防止 PlayerController.Start 和 HealthBarManager.Start 双重调用）
            if (hasInitialized) return;
            
            maxHealth = health;
            currentHealth = health;
            CreateHeartUI();
        }

        /// <summary>
        /// 减少生命值
        /// </summary>
        public void TakeDamage(int damage)
        {
            int previousHealth = currentHealth;
            currentHealth = Mathf.Max(0, currentHealth - damage);
            UpdateHeartUI();
            Events.OnPlayerHealthChanged.Invoke(currentHealth);

            // 生成掉落的心形元素（即使生命值变为0）
            if (previousHealth > currentHealth)
            {
                SpawnFallingHeart();
            }

            if (currentHealth <= 0)
            {
                Events.OnPlayerDead.Invoke();
            }
        }

        /// <summary>
        /// 增加生命值
        /// </summary>
        public void Heal(int amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            UpdateHeartUI();
            Events.OnPlayerHealthChanged.Invoke(currentHealth);
        }

        /// <summary>
        /// 重置生命值
        /// </summary>
        public void ResetHealth()
        {
            currentHealth = maxHealth;
            UpdateHeartUI();
            Events.OnPlayerHealthChanged.Invoke(currentHealth);
        }

        #endregion

        #region UI管理

        /// <summary>
        /// 创建心形UI元素
        /// </summary>
        private void CreateHeartUI()
        {
            // 先彻底清理所有旧心形
            if (healthBarParent != null)
            {
                List<GameObject> children = new List<GameObject>();
                foreach (Transform child in healthBarParent)
                    children.Add(child.gameObject);

                foreach (var child in children)
                    DestroyImmediate(child);  // 立即销毁，不等帧结束
            }

            heartUIs.Clear();
            uiHeartPool.Clear();

            if (healthBarParent == null)
            {
                Debug.LogError("[HealthBarManager] healthBarParent is null!");
                return;
            }

            // 创建或从池中获取心形
            for (int i = 0; i < maxHealth; i++)
            {
                GameObject heartObj = null;
                
                if (uiHeartPool.Count > 0 && Application.isPlaying)
                {
                    heartObj = uiHeartPool.Pop();
                    heartObj.SetActive(true);
                }
                else if (heartPrefab != null)
                {
                    heartObj = Instantiate(heartPrefab, healthBarParent);
                }
                else
                {
                    heartObj = CreateDefaultHeart();
                }

                // 始终设置层级和大小
                heartObj.transform.SetParent(healthBarParent, false);
                RectTransform rect = heartObj.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(heartSize, heartSize);
                rect.anchorMin = new Vector2(0, 0.5f);  // 左中对齐
                rect.anchorMax = new Vector2(0, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.localPosition = new Vector3(i * (heartSize + heartSpacing), 0, 0);

                heartUIs.Add(heartObj);
            }

            // 更新UI
            UpdateHeartUI();
        }

        /// <summary>
        /// 创建默认心形UI
        /// </summary>
        private GameObject CreateDefaultHeart()
        {
            GameObject heartObj = new GameObject($"Heart_{heartUIs.Count}");
            heartObj.transform.SetParent(healthBarParent);

            // 添加Image组件
            Image image = heartObj.AddComponent<Image>();
            image.color = fullHeartColor;

            // 添加RectTransform
            RectTransform rect = heartObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(heartSize, heartSize);

            return heartObj;
        }

        /// <summary>
        /// 更新心形UI显示
        /// </summary>
        private void UpdateHeartUI()
        {
            for (int i = 0; i < heartUIs.Count; i++)
            {
                if (heartUIs[i] != null)
                {
                    Image image = heartUIs[i].GetComponent<Image>();
                    if (image != null)
                    {
                        bool isFull = i < currentHealth;
                        image.color = isFull ? fullHeartColor : emptyHeartColor;
                        
                        // 应用对应的 Sprite
                        if (isFull)
                        {
                            if (fullHeartSprite != null) image.sprite = fullHeartSprite;
                        }
                        else
                        {
                            if (emptyHeartSprite != null) image.sprite = emptyHeartSprite;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 生成掉落的心形元素 — 配置全部由预制体自己管理
        /// 生成的心形会挂载 UIContextConstraint，离开伤害区域后被回收
        /// </summary>
        public void SpawnFallingHeart()
        {
            if (heartUIs.Count > currentHealth)
            {
                GameObject heartUI = heartUIs[currentHealth];
                if (heartUI != null && UIPhysicsManager.Instance != null)
                {
                    var spawnedElement = UIPhysicsManager.Instance.SpawnHeartElement(
                        heartUI.transform.position, 0f, null, null);
                    
                    if (spawnedElement != null)
                    {
                        // ★ 清理对象池中残留的旧 UIContextConstraint（避免上一轮约束干扰）
                        var oldConstraint = spawnedElement.GetComponent<OutOfBounds.Puzzle.UIContextConstraint>();
                        if (oldConstraint != null)
                            Destroy(oldConstraint);

                        // 只在 damageAreaTransform 已配置时才添加新约束
                        if (damageAreaTransform != null)
                        {
                            var constraint = spawnedElement.gameObject.AddComponent<OutOfBounds.Puzzle.UIContextConstraint>();
                            constraint.SetupSignalSource(damageAreaTransform, heartValidRange,
                                OutOfBounds.Core.ContextConstraintType.SignalSource, heartSignalFadeStart);
                            constraint.OnConstraintViolated += OnHeartOutOfContext;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 心形离开伤害上下文后的回收处理
        /// </summary>
        private void OnHeartOutOfContext(OutOfBounds.Puzzle.UIContextConstraint constraint)
        {
            var physicsElement = constraint.GetComponent<UIPhysicsElement>();
            if (physicsElement != null && UIPhysicsManager.Instance != null)
            {
                Debug.Log($"[HealthBarManager] 心形离开伤害区域，回收中...");
                UIPhysicsManager.Instance.RecycleHeartElement(physicsElement);
            }
        }

        #endregion

        #region 事件处理

        private void OnPlayerHealthChanged(int health)
        {
            currentHealth = health;
            UpdateHeartUI();
        }

        private void OnPlayerDead()
        {
            // 玩家死亡时的处理
            UpdateHeartUI();
        }

        #endregion

        #region 公共属性

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;

        #endregion
    }
}
