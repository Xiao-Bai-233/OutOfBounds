using UnityEngine;
using OutOfBounds.UI;
using OutOfBounds.DragSystem;

namespace OutOfBounds.Puzzle
{
    /// <summary>
    /// 可拖拽单词组件（精简版）
    /// 仅负责：单词身份标识、脱离/重置行为逻辑、事件通知、BrokenGlyph 状态
    /// 物理由 UIPhysicsElement 接管，拖拽由 DraggableUI 接管
    /// 上下文限制由 UIContextConstraint 接管
    /// </summary>
    public class DraggableWord : MonoBehaviour
    {
        [Header("单词设置")]
        [Tooltip("单词内容（用于身份识别）")]
        [SerializeField] private string wordText = "[SPACE]";

        [Header("视觉反馈")]
        [Tooltip("脱离状态颜色（如果物体有 Image 组件）")]
        [SerializeField] private Color detachedTint = new Color(1f, 0.8f, 0.2f, 1f);

        [Tooltip("乱码化颜色（BrokenGlyph 状态）")]
        [SerializeField] private Color brokenTint = new Color(1f, 0.2f, 0.2f, 0.5f);

        [Tooltip("乱码化时的抖动强度")]
        [SerializeField] private float brokenShakeIntensity = 2f;

        [Header("上下文限制（BrokenGlyph）")]
        [Tooltip("上下文限制组件 — 检测是否离开语义区域")]
        [SerializeField] private UIContextConstraint contextConstraint;

        // 状态
        private bool isDetached = false;
        private bool isBroken = false;
        private UnityEngine.UI.Image cachedImage;
        private Color originalColor;
        private Vector3 originalLocalPos;
        private BoxCollider2D boxCollider;

        // 事件 — 供 StageConfig 订阅
        public event System.Action<DraggableWord> OnWordDetached;
        public event System.Action<DraggableWord> OnWordDropped;
        public event System.Action<DraggableWord> OnWordBroken;
        public event System.Action<DraggableWord> OnWordRestored;

        // 属性
        public bool IsDetached => isDetached;
        public bool IsBroken => isBroken;
        public string WordText => wordText;
        public RectTransform Rect => GetComponent<RectTransform>();

        #region Unity 生命周期

        private void Awake()
        {
            cachedImage = GetComponent<UnityEngine.UI.Image>();
            boxCollider = GetComponent<BoxCollider2D>();
            contextConstraint = GetComponent<UIContextConstraint>();

            if (cachedImage != null)
                originalColor = cachedImage.color;

            originalLocalPos = transform.localPosition;
        }

        private void Start()
        {
            var draggable = GetComponent<DraggableUI>();
            if (draggable != null)
            {
                draggable.OnDragStart += HandleDragStart;
                draggable.OnDragEnd += HandleDragEnd;
            }

            // 订阅上下文限制事件
            if (contextConstraint != null)
            {
                contextConstraint.OnConstraintViolated += OnContextViolated;
                contextConstraint.OnConstraintRestored += OnContextRestored;
            }
        }

        private void OnDestroy()
        {
            var draggable = GetComponent<DraggableUI>();
            if (draggable != null)
            {
                draggable.OnDragStart -= HandleDragStart;
                draggable.OnDragEnd -= HandleDragEnd;
            }

            if (contextConstraint != null)
            {
                contextConstraint.OnConstraintViolated -= OnContextViolated;
                contextConstraint.OnConstraintRestored -= OnContextRestored;
            }
        }

        #endregion

        #region 事件回调

        private void HandleDragStart(DraggableUI draggable)
        {
            // 乱码状态下不能拖拽
            if (isBroken) return;
            DetachWord();
        }

        private void HandleDragEnd(DraggableUI draggable)
        {
            if (isDetached && !isBroken)
                OnWordDropped?.Invoke(this);
        }

        private void OnContextViolated(UIContextConstraint constraint)
        {
            BecomeBrokenGlyph();
        }

        private void OnContextRestored(UIContextConstraint constraint)
        {
            RestoreGlyph();
        }

        #endregion

        #region BrokenGlyph 机制

        /// <summary>
        /// 变成乱码 — 脱离语义区域后的惩罚
        /// 视觉损坏 + 禁用碰撞 + 抖动
        /// </summary>
        public void BecomeBrokenGlyph()
        {
            if (isBroken) return;
            isBroken = true;

            // 视觉反馈
            if (cachedImage != null)
                cachedImage.color = brokenTint;

            // 禁用碰撞（变成 Broken Glyph — 无碰撞的废料）
            if (boxCollider != null)
                boxCollider.enabled = false;

            // 禁用物理
            var physicsElement = GetComponent<UIPhysicsElement>();
            if (physicsElement != null)
            {
                physicsElement.SetColliderEnabled(false);
                physicsElement.SetPhysicsLocked(true);
                physicsElement.SetVelocity(Vector2.zero);
                physicsElement.isPlatform = false;
            }

            // 禁用拖拽
            var draggable = GetComponent<DraggableUI>();
            if (draggable != null)
                draggable.SetDraggable(false);

            OnWordBroken?.Invoke(this);
            Debug.Log($"[DraggableWord] 单词 '{wordText}' 变成 Broken Glyph — Token lost semantic context.");
        }

        /// <summary>
        /// 恢复乱码 — 回到语义区域后恢复
        /// </summary>
        public void RestoreGlyph()
        {
            if (!isBroken) return;
            isBroken = false;

            // 恢复视觉
            if (cachedImage != null)
                cachedImage.color = isDetached ? detachedTint : originalColor;

            // 恢复碰撞
            if (boxCollider != null)
                boxCollider.enabled = true;

            // 恢复物理
            var physicsElement = GetComponent<UIPhysicsElement>();
            if (physicsElement != null)
            {
                physicsElement.SetColliderEnabled(true);
                physicsElement.SetPhysicsLocked(false);
                physicsElement.isPlatform = true;
            }

            // 恢复拖拽
            var draggable = GetComponent<DraggableUI>();
            if (draggable != null)
                draggable.SetDraggable(true);

            OnWordRestored?.Invoke(this);
            Debug.Log($"[DraggableWord] 单词 '{wordText}' 已恢复 — Token restored.");
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 触发单词脱离
        /// </summary>
        public void DetachWord()
        {
            if (isDetached || isBroken) return;
            isDetached = true;

            if (cachedImage != null)
                cachedImage.color = detachedTint;

            if (transform.parent != null)
            {
                Canvas parentCanvas = GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                    transform.SetParent(parentCanvas.transform, true);
            }

            OnWordDetached?.Invoke(this);
            Debug.Log($"[DraggableWord] 单词 '{wordText}' 已脱离");
        }

        /// <summary>
        /// 重置单词回到初始状态
        /// </summary>
        public void ResetWord()
        {
            isDetached = false;
            isBroken = false;

            if (cachedImage != null)
                cachedImage.color = originalColor;

            if (boxCollider != null)
                boxCollider.enabled = true;

            var physicsElement = GetComponent<UIPhysicsElement>();
            if (physicsElement != null)
            {
                physicsElement.SetColliderEnabled(true);
                physicsElement.SetPhysicsLocked(false);
                physicsElement.SetVelocity(Vector2.zero);
                physicsElement.isPlatform = true;
            }

            var draggable = GetComponent<DraggableUI>();
            if (draggable != null)
                draggable.SetDraggable(true);

            OnWordDropped?.Invoke(this);
            Debug.Log($"[DraggableWord] 单词 '{wordText}' 已重置");
        }

        #endregion
    }
}
