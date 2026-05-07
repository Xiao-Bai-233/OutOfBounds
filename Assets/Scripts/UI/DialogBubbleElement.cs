using UnityEngine;
using UnityEngine.UI;
using OutOfBounds.UI;
using OutOfBounds.DragSystem;

namespace OutOfBounds.UI
{
    /// <summary>
    /// 物理对话气泡 - 尺寸参数全部在 Inspector 中可调
    /// </summary>
    public class DialogBubbleElement : UIPhysicsElement
    {
        [Header("对话气泡设置")]
        [SerializeField] private Text contentText;
        [SerializeField] private float dragRotationStrength = 8f;

        [Header("气泡尺寸（Inspector 手动调整，实时生效）")]
        [Tooltip("气泡整体缩放")]
        [SerializeField] private float bubbleScale = 0.08f;
        [Tooltip("气泡固定高度（sizeDelta.y）")]
        [SerializeField] private float bubbleHeight = 20f;
        [Tooltip("字体大小（越大越清晰，配合 bubbleScale 控制视觉大小）")]
        [SerializeField] private int fontSize = 12;
        [Tooltip("文本子物体本地缩放（保持1.0最清晰，调fontSize控制大小）")]
        [SerializeField] private float textScale = 1.0f;
        [Tooltip("文字与气泡边框左右间距")]
        [SerializeField] private int textPaddingHorizontal = 6;

        private BoxCollider2D boxCollider;
        private Vector2 dragPointOffset;
        private HorizontalLayoutGroup layoutGroup;

        protected override void Awake()
        {
            base.Awake();
            
            boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider == null) boxCollider = gameObject.AddComponent<BoxCollider2D>();

            // 物理初始化
            mass = 0.5f;
            isPlatform = true;
            useGravity = true;
        }

        private void Start()
        {
            // 预制体实例化后，应用 Inspector 中的尺寸参数
            ApplyBubbleSettings();
        }

        /// <summary>
        /// 将 Inspector 中的尺寸参数应用到实际组件
        /// </summary>
        public void ApplyBubbleSettings()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

            // 1. 整体缩放
            transform.localScale = Vector3.one * bubbleScale;

            // 2. 固定高度（宽度由 ContentSizeFitter 控制）
            Vector2 size = rectTransform.sizeDelta;
            size.y = bubbleHeight;
            rectTransform.sizeDelta = size;

            // 3. 字体大小 + 文本缩放
            if (contentText != null)
            {
                contentText.fontSize = fontSize;
                contentText.rectTransform.localScale = Vector3.one * textScale;
            }

            // 4. 禁用 HorizontalLayoutGroup（它干扰文本对齐，padding 已在宽度计算中）
            if (layoutGroup == null)
                layoutGroup = GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup != null)
                layoutGroup.enabled = false;

            // 注意：不在这里刷新布局，由 SetText 统一控制刷新顺序
        }

        public void SetText(string text)
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (contentText == null) return;

            // 1. 应用尺寸参数
            ApplyBubbleSettings();

            // 2. 设置文本 + 确保左对齐
            contentText.alignment = TextAnchor.MiddleLeft;
            contentText.text = text;

            // 3. ★ 估算文本宽度：考虑 textScale（用户用大 fontSize+小 textScale 获得清晰文字）
            float effectiveSize = fontSize * textScale; // 实际视觉字体大小
            float textWidth = 0f;
            foreach (char c in text)
            {
                textWidth += (c > 127) ? effectiveSize : effectiveSize * 0.5f;
            }
            textWidth = Mathf.Max(textWidth, effectiveSize);

            float totalWidth = textWidth + textPaddingHorizontal * 2;
            totalWidth = Mathf.Max(totalWidth, 20f);

            Vector2 size = rectTransform.sizeDelta;
            size.x = totalWidth;
            rectTransform.sizeDelta = size;

            // 4. 同步物理碰撞体
            UpdateColliderSize();
        }

        private void UpdateColliderSize()
        {
            if (boxCollider != null && rectTransform != null)
            {
                Vector2 size = rectTransform.rect.size;
                boxCollider.size = size;
                boxCollider.offset = Vector2.zero;
            }
        }

        protected override void Update()
        {
            base.Update();
            
            if (isBeingDragged) HandleDragRotation();

            if (rectTransform != null && rectTransform.hasChanged)
            {
                UpdateColliderSize();
                rectTransform.hasChanged = false;
            }
        }

        private void HandleDragRotation()
        {
            float pendulumEffect = -dragPointOffset.x * dragRotationStrength;
            rotation = Mathf.LerpAngle(rotation, pendulumEffect, Time.deltaTime * 5f);
            angularVelocity = 0;
        }

        public override void StartDrag()
        {
            base.StartDrag();
            Vector2 mousePos = Input.mousePosition;
            if (rectTransform != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, mousePos, null, out Vector2 localPoint))
            {
                dragPointOffset = localPoint;
            }
        }
    }
}
