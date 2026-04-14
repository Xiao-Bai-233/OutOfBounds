using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using OutOfBounds.Core;
using OutOfBounds.DragSystem;

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

        [Header("心形元素设置")]
        [SerializeField] public float heartSize = 50f;
        [SerializeField] public float heartSpacing = 10f;
        [SerializeField] public Sprite fullHeartSprite;
        [SerializeField] public Sprite emptyHeartSprite;
        [SerializeField] public Color fullHeartColor = Color.white; // 默认改为白色，方便Sprite原色显示
        [SerializeField] public Color emptyHeartColor = Color.white;

        [Header("掉落心形光标设置")]
        [SerializeField] public CursorType heartHoverCursor = CursorType.Grab;
        [SerializeField] public CursorType heartDraggingCursor = CursorType.Grabbing;
        [SerializeField] public Texture2D heartCustomHoverCursor;
        [SerializeField] public Texture2D heartCustomDraggingCursor;

        [Header("掉落心形物理设置")]
        [SerializeField] private string heartLayerName = "UI_Physics";

        // 心形UI元素列表
        private List<GameObject> heartUIs = new List<GameObject>();
        private Stack<GameObject> uiHeartPool = new Stack<GameObject>();
        
        // 编辑模式下的更新标志
        private bool needUpdateInEditMode = false;

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
            // 创建初始心形UI
            CreateHeartUI();

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
            // 彻底清理：不仅清理列表，还要清理父物体下的所有子物体
            // 防止在编辑模式或重新开始游戏时产生残留
            if (healthBarParent != null)
            {
                // 收集所有子物体
                List<GameObject> children = new List<GameObject>();
                foreach (Transform child in healthBarParent)
                {
                    children.Add(child.gameObject);
                }

                // 销毁所有子物体
                foreach (var child in children)
                {
                    if (Application.isEditor && !Application.isPlaying)
                    {
                        DestroyImmediate(child);
                    }
                    else
                    {
                        // 运行时如果使用了池，可以回收，但这里为了彻底解决生成多份的问题，先直接销毁
                        Destroy(child);
                    }
                }
            }

            heartUIs.Clear();
            uiHeartPool.Clear(); // 同时也清空 UI 池，防止旧引用干扰

            // 确保healthBarParent存在
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

                // 始终设置层级和大小，确保 heartSize 变量生效
                heartObj.transform.SetParent(healthBarParent);
                RectTransform rect = heartObj.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(heartSize, heartSize);
                rect.anchoredPosition = new Vector2(i * (heartSize + heartSpacing), 0);

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
        /// 生成掉落的心形元素
        /// </summary>
        public void SpawnFallingHeart()
        {
            if (heartUIs.Count > currentHealth)
            {
                // 获取最后一个完整的心形UI位置
                GameObject heartUI = heartUIs[currentHealth];
                if (heartUI != null)
                {
                    // 转换为世界坐标
                    Vector3 worldPosition = heartUI.transform.position;

                    // 始终使用变量定义的 heartSize
                    float size = heartSize;

                    // 使用UIPhysicsManager生成心形物理元素
                    if (UIPhysicsManager.Instance != null)
                    {
                        UIPhysicsElement heartElement = UIPhysicsManager.Instance.SpawnHeartElement(worldPosition, size, null, fullHeartSprite);
                        if (heartElement != null)
                        {
                            // 设置 Layer
                            int layer = LayerMask.NameToLayer(heartLayerName);
                            if (layer != -1)
                            {
                                heartElement.gameObject.layer = layer;
                            }
                            else
                            {
                                Debug.LogWarning($"[HealthBarManager] 未找到名为 {heartLayerName} 的 Layer，请在标签与层设置中检查！");
                            }

                            // 确保心形元素受重力影响且不漂浮
                            heartElement.SetUseGravity(true);
                            heartElement.isFloating = false; 
                            
                            // 关键修复：先检查是否已有 DraggableUI（适配对象池）
                            DraggableUI draggable = heartElement.GetComponent<DraggableUI>();
                            if (draggable == null)
                            {
                                draggable = heartElement.gameObject.AddComponent<DraggableUI>();
                            }
                            
                            draggable.SetDraggable(true);
                            
                            // 重新配置参数，确保池中取出的对象设置是最新的
                            draggable.hoverCursor = heartHoverCursor;
                            draggable.draggingCursor = heartDraggingCursor;
                            draggable.customHoverCursor = heartCustomHoverCursor;
                            draggable.customDraggingCursor = heartCustomDraggingCursor;
                            draggable.autoSwitchCursor = true;

                            draggable.highlightOnHover = false;
                            draggable.normalColor = Color.white;
                            draggable.hoverColor = Color.white;
                            draggable.draggingColor = Color.white;
                        }
                    }
                }
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
