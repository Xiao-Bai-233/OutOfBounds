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
        [SerializeField] public Color fullHeartColor = Color.red;
        [SerializeField] public Color emptyHeartColor = Color.gray;

        // 心形UI元素列表
        private List<GameObject> heartUIs = new List<GameObject>();
        
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
            // 清除现有心形
            foreach (var heart in heartUIs)
            {
                if (heart != null)
                {
                    if (Application.isEditor && !Application.isPlaying)
                    {
                        // 在编辑模式下，使用DestroyImmediate
                        DestroyImmediate(heart);
                    }
                    else
                    {
                        // 在运行时，使用Destroy
                        Destroy(heart);
                    }
                }
            }
            heartUIs.Clear();

            // 确保healthBarParent存在
            if (healthBarParent == null)
            {
                Debug.LogError("[HealthBarManager] healthBarParent is null!");
                return;
            }

            // 创建新的心形
            for (int i = 0; i < maxHealth; i++)
            {
                GameObject heartObj;
                if (heartPrefab != null)
                {
                    heartObj = Instantiate(heartPrefab, healthBarParent);
                }
                else
                {
                    // 创建默认心形
                    heartObj = CreateDefaultHeart();
                }

                // 设置位置
                RectTransform rect = heartObj.GetComponent<RectTransform>();
                if (heartPrefab == null)
                {
                    // 没有预制体时，使用代码中的设置
                    rect.sizeDelta = new Vector2(heartSize, heartSize);
                }
                // 有预制体时，使用预制体的大小
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
                        image.color = i < currentHealth ? fullHeartColor : emptyHeartColor;
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

                    // 计算心形元素的大小
                    float size = heartSize;
                    if (heartPrefab != null)
                    {
                        // 有预制体时，使用预制体的大小
                        RectTransform prefabRect = heartPrefab.GetComponent<RectTransform>();
                        if (prefabRect != null)
                        {
                            size = prefabRect.sizeDelta.x;
                        }
                    }

                    // 使用UIPhysicsManager生成心形物理元素
                    if (UIPhysicsManager.Instance != null)
                    {
                        UIPhysicsElement heartElement = UIPhysicsManager.Instance.SpawnHeartElement(worldPosition, size);
                        if (heartElement != null)
                        {
                            // 确保心形元素受重力影响
                            heartElement.SetUseGravity(true);
                            
                            // 添加拖拽功能
                            DraggableUI draggable = heartElement.gameObject.AddComponent<DraggableUI>();
                            draggable.SetDraggable(true);
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
