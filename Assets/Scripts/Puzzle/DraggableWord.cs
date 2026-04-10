using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using OutOfBounds.UI;
using OutOfBounds.DragSystem;

namespace OutOfBounds.Puzzle
{
    /// <summary>
    /// 可拖拽单词组件
    /// 用于文字解密玩法 - 单词可以从文本中脱离并变为物理对象
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class DraggableWord : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("单词设置")]
        [Tooltip("单词内容（用于显示）")]
        [SerializeField] private string wordText = "[SPACE]";

        [Header("视觉样式")]
        [Tooltip("单词背景图片")]
        [SerializeField] private Image backgroundImage;
        [Tooltip("普通状态颜色")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [Tooltip("悬停状态颜色")]
        [SerializeField] private Color hoverColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
        [Tooltip("已脱离状态颜色")]
        [SerializeField] private Color detachedColor = new Color(1f, 0.8f, 0.2f, 1f);

        [Header("脱离物理设置")]
        [Tooltip("脱离后是否启用重力")]
        [SerializeField] private bool enableGravityWhenDetached = true;
        [Tooltip("脱离后的重力值")]
        [SerializeField] private float detachedGravity = -20f;
        [Tooltip("脱离时的初始速度")]
        [SerializeField] private Vector2 initialVelocity = Vector2.zero;

        [Header("Inspector可拖拽资源")]
        [Tooltip("背景精灵（可选）")]
        [SerializeField] private Sprite backgroundSprite;
        [Tooltip("父级文本框（用于计算相对位置）")]
        [SerializeField] private RectTransform parentTextContainer;

        // 组件引用
        private RectTransform rectTransform;
        private Text label;
        private Canvas parentCanvas;
        private BoxCollider2D groundCollider;

        // 状态
        private bool isDetached = false;           // 是否已从文本脱离
        private bool isDragging = false;
        private bool hasAddedCollider = false;      // 是否已添加碰撞器
        private Vector2 velocity = Vector2.zero;
        private Vector2 dragOffset;
        private Vector2 lastPosition;

        // 事件
        public event System.Action<DraggableWord> OnWordDetached;      // 单词脱离时
        public event System.Action<DraggableWord> OnWordDropped;      // 单词放下时
        public event System.Action<DraggableWord, Vector2> OnWordMoved; // 单词移动时

        // 属性
        public bool IsDetached => isDetached;
        public string WordText => wordText;
        public RectTransform Rect => rectTransform;

        #region Unity 生命周期

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();

            // 确保有 Text 组件
            label = GetComponent<Text>();
            if (label == null)
            {
                label = gameObject.AddComponent<Text>();
                label.text = wordText;
                label.alignment = TextAnchor.MiddleCenter;
                label.fontSize = 24;
                label.color = Color.white;
            }

            // 设置初始样式
            SetupAppearance();
        }

        private void Start()
        {
            lastPosition = rectTransform.anchoredPosition;
        }

        private void Update()
        {
            if (isDetached && !isDragging)
            {
                // 应用物理模拟
                ApplyPhysics();
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 触发单词脱离（从文本中释放）
        /// </summary>
        public void DetachWord()
        {
            if (isDetached) return;

            isDetached = true;
            velocity = initialVelocity;

            // 改变颜色
            SetBackgroundColor(detachedColor);

            // 如果有父级，解绑
            if (transform.parent != null)
            {
                transform.SetParent(parentCanvas.transform, true);
            }

            // 通知事件
            OnWordDetached?.Invoke(this);

            Debug.Log($"[DraggableWord] 单词 '{wordText}' 已脱离文本");
        }

        /// <summary>
        /// 重置单词回到文本状态
        /// </summary>
        public void ResetWord()
        {
            isDetached = false;
            isDragging = false;
            velocity = Vector2.zero;

            // 移除碰撞器
            if (groundCollider != null)
            {
                Destroy(groundCollider);
                groundCollider = null;
            }
            hasAddedCollider = false;

            // 恢复颜色
            SetBackgroundColor(normalColor);

            // 通知事件
            OnWordDropped?.Invoke(this);

            Debug.Log($"[DraggableWord] 单词 '{wordText}' 已重置");
        }

        /// <summary>
        /// 设置单词内容
        /// </summary>
        public void SetWordText(string text)
        {
            wordText = text;
            if (label != null)
            {
                label.text = text;
            }
        }

        /// <summary>
        /// 获取当前速度
        /// </summary>
        public Vector2 GetVelocity() => velocity;

        /// <summary>
        /// 设置速度（用于外部影响）
        /// </summary>
        public void SetVelocity(Vector2 vel)
        {
            velocity = vel;
        }

        #endregion

        #region 私有方法

        private void SetupAppearance()
        {
            // 添加或配置背景图片
            if (backgroundImage == null)
            {
                var bgObj = new GameObject("Background");
                bgObj.transform.SetParent(transform);
                bgObj.transform.SetAsFirstSibling();

                backgroundImage = bgObj.AddComponent<Image>();
                backgroundImage.raycastTarget = false; // 让背景不阻挡点击

                var bgRect = bgObj.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
                bgRect.anchoredPosition = Vector2.zero;
            }

            // 应用精灵和颜色
            if (backgroundSprite != null)
            {
                backgroundImage.sprite = backgroundSprite;
            }
            backgroundImage.color = normalColor;
        }

        private void SetBackgroundColor(Color color)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = color;
            }
        }

        private void ApplyPhysics()
        {
            if (!enableGravityWhenDetached) return;

            // 简单的物理模拟
            float deltaTime = Time.deltaTime;

            // 应用重力
            velocity.y += detachedGravity * deltaTime;

            // 更新位置
            Vector2 newPosition = rectTransform.anchoredPosition + velocity * deltaTime;

            // 边界检测（如果存在父容器）
            if (parentTextContainer != null)
            {
                ClampToParent(ref newPosition);
            }

            rectTransform.anchoredPosition = newPosition;
            lastPosition = newPosition;

            // 检测是否落地（速度接近0且在下方）
            CheckIfLanded();
        }

        private void ClampToParent(ref Vector2 position)
        {
            if (parentTextContainer == null) return;

            Vector2 parentSize = parentTextContainer.sizeDelta;
            Vector2 halfSize = rectTransform.sizeDelta * 0.5f;

            // 简单的边界约束
            float minX = -parentSize.x * 0.5f + halfSize.x;
            float maxX = parentSize.x * 0.5f - halfSize.x;
            float minY = -parentSize.y * 0.5f + halfSize.y;
            float maxY = parentSize.y * 0.5f - halfSize.y;

            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.y = Mathf.Clamp(position.y, minY, maxY);
        }

        private void CheckIfLanded()
        {
            // 如果速度很小且在移动，视为落地
            if (velocity.magnitude < 0.5f && !hasAddedCollider)
            {
                // 添加碰撞器，让玩家可以踩在上面
                AddGroundCollider();
            }
        }

        private void AddGroundCollider()
        {
            if (groundCollider != null) return;

            groundCollider = gameObject.AddComponent<BoxCollider2D>();
            groundCollider.size = rectTransform.sizeDelta;
            groundCollider.offset = Vector2.zero;
            groundCollider.isTrigger = false; // 实体碰撞，让玩家能站在上面

            hasAddedCollider = true;
            Debug.Log($"[DraggableWord] 已添加碰撞器，尺寸: {groundCollider.size}");
        }

        #endregion

        #region IPointerClickHandler

        public void OnPointerClick(PointerEventData eventData)
        {
            // 如果还没脱离，点击可以触发脱离（作为备用触发方式）
            if (!isDetached && eventData.button == PointerEventData.InputButton.Left)
            {
                // 可以在这里实现点击脱离，或者留给拖拽
            }
        }

        #endregion

        #region IBeginDragHandler

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isDetached)
            {
                // 拖拽时自动脱离
                DetachWord();
            }

            isDragging = true;
            dragOffset = rectTransform.anchoredPosition - GetDragPosition(eventData);
        }

        #endregion

        #region IDragHandler

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            Vector2 newPosition = GetDragPosition(eventData) + dragOffset;
            rectTransform.anchoredPosition = newPosition;
            lastPosition = newPosition;

            // 拖拽时清空速度
            velocity = Vector2.zero;

            OnWordMoved?.Invoke(this, newPosition);
        }

        private Vector2 GetDragPosition(PointerEventData eventData)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint
            );
            return localPoint;
        }

        #endregion

        #region IEndDragHandler

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            velocity = Vector2.zero;

            OnWordDropped?.Invoke(this);
        }

        #endregion

        #region 编辑器支持

        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (label != null && !Application.isPlaying)
            {
                label.text = wordText;
            }
        }
        #endif

        #endregion
    }
}
