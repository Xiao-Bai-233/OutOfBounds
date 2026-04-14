using UnityEngine;
using UnityEngine.UI;
using OutOfBounds.UI;
using OutOfBounds.DragSystem;

namespace OutOfBounds.UI
{
    /// <summary>
    /// 物理对话气泡 - 优化版
    /// 解决世界空间下尺寸过大问题，并支持最大宽度限制
    /// </summary>
    public class DialogBubbleElement : UIPhysicsElement
    {
        [Header("对话气泡设置")]
        [SerializeField] private Text contentText;
        [SerializeField] private float maxWidth = 5f; // 世界空间下的最大宽度（米）
        [SerializeField] private float dragRotationStrength = 8f; 

        private BoxCollider2D boxCollider;
        private Vector2 dragPointOffset; 

        protected override void Awake()
        {
            base.Awake();
            
            boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider == null) boxCollider = gameObject.AddComponent<BoxCollider2D>();

            // 物理初始化
            mass = 0.5f;
            isPlatform = true;
            useGravity = true;
            isFloating = false;
        }

        public void SetText(string text)
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

            if (contentText != null)
            {
                // 1. 设置文本
                contentText.text = text;
                
                // 2. 处理宽度限制逻辑
                // 在世界空间下，我们需要根据文字量动态调整 LayoutElement
                LayoutElement layoutElement = contentText.GetComponent<LayoutElement>();
                if (layoutElement == null) layoutElement = contentText.gameObject.AddComponent<LayoutElement>();
                
                // 先不限制宽度，获取理想宽度
                layoutElement.preferredWidth = -1; 
                Canvas.ForceUpdateCanvases();
                
                // 如果理想宽度超过了最大限制，则开启自动换行并固定宽度
                float preferredWidth = LayoutUtility.GetPreferredWidth(contentText.rectTransform);
                if (preferredWidth > maxWidth)
                {
                    layoutElement.preferredWidth = maxWidth;
                    contentText.horizontalOverflow = HorizontalWrapMode.Wrap;
                }
                else
                {
                    layoutElement.preferredWidth = -1;
                    contentText.horizontalOverflow = HorizontalWrapMode.Overflow;
                }

                // 3. 强制刷新整体布局
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                
                // 4. 同步物理碰撞体
                UpdateColliderSize();
            }
        }

        private void UpdateColliderSize()
        {
            if (boxCollider != null && rectTransform != null)
            {
                // 强制同步物理盒子的尺寸
                Vector2 size = rectTransform.rect.size;
                boxCollider.size = size;
                boxCollider.offset = Vector2.zero;
                
                Debug.Log($"[DialogBubble] 尺寸同步: {size}, 最大限制: {maxWidth}");
            }
        }

        protected override void Update()
        {
            base.Update();
            
            if (isBeingDragged) HandleDragRotation();

            // 持续监测 UI 变化
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
