using UnityEngine;
using UnityEngine.EventSystems;
using OutOfBounds.Core;
using OutOfBounds.UI;

namespace OutOfBounds.DragSystem
{
    /// <summary>
    /// 光标类型枚举
    /// </summary>
    public enum CursorType
    {
        Default,
        Grab,
        Grabbing,
        Pointer,
        Crosshair
    }

/// <summary>
/// 可拖拽的UI组件
/// 处理鼠标/触摸拖拽交互，支持右键切换固定模式
/// </summary>
[RequireComponent(typeof(UIPhysicsElement))]
public class DraggableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IInteractable
{
    #region 静态光标管理

    private static CursorType currentCursor = CursorType.Default;

    /// <summary>
    /// 设置全局光标类型
    /// </summary>
    public static void SetCursor(CursorType cursor)
    {
        if (currentCursor == cursor) return;
        currentCursor = cursor;

        // 设置对应的系统光标
        switch (cursor)
        {
            case CursorType.Default:
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                break;
            case CursorType.Grab:
                Cursor.SetCursor(CreateGrabCursor(), new Vector2(12, 12), CursorMode.Auto);
                break;
            case CursorType.Grabbing:
                Cursor.SetCursor(CreateGrabbingCursor(), new Vector2(12, 12), CursorMode.Auto);
                break;
            case CursorType.Pointer:
                Cursor.SetCursor(CreatePointerCursor(), new Vector2(0, 0), CursorMode.Auto);
                break;
            case CursorType.Crosshair:
                Cursor.SetCursor(CreateCrosshairCursor(), new Vector2(8, 8), CursorMode.Auto);
                break;
        }
    }

    /// <summary>
    /// 设置自定义光标图片
    /// </summary>
    private void SetCustomCursor(Texture2D cursorTexture)
    {
        if (cursorTexture != null)
        {
            Cursor.SetCursor(cursorTexture, customCursorHotspot, CursorMode.Auto);
        }
    }

    /// <summary>
    /// 恢复默认光标
    /// </summary>
    public static void ResetCursor()
    {
        currentCursor = CursorType.Default;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    /// <summary>
    /// 创建抓取光标纹理（简单箭头+手的组合）
    /// </summary>
    private static Texture2D CreateGrabCursor()
    {
        int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];

        // 简单的手形光标
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 手掌（白色椭圆）
                float centerX = size / 2f;
                float centerY = size / 2f;
                float dx = (x - centerX) / (size / 4f);
                float dy = (y - centerY) / (size / 4f);
                float dist = dx * dx + dy * dy;

                // 手掌
                if (dist < 1f)
                {
                    pixels[y * size + x] = new Color(1f, 1f, 1f, 1f);
                }
                // 食指
                else if (y < size / 2 && x > size / 3 && x < size / 2 && y > size / 4)
                {
                    pixels[y * size + x] = new Color(1f, 1f, 1f, 1f);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    /// <summary>
    /// 创建抓取中光标纹理
    /// </summary>
    private static Texture2D CreateGrabbingCursor()
    {
        int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];

        // 闭合的拳头形状
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float centerX = size / 2f;
                float centerY = size / 2.5f;
                float dx = (x - centerX) / (size / 4f);
                float dy = (y - centerY) / (size / 3f);
                float dist = dx * dx + dy * dy;

                // 拳头（实心椭圆）
                if (dist < 1f)
                {
                    pixels[y * size + x] = new Color(1f, 1f, 1f, 1f);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    /// <summary>
    /// 创建指针光标纹理
    /// </summary>
    private static Texture2D CreatePointerCursor()
    {
        int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];

        // 三角形指针
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (y < size / 2 && x > y && x < size - 1 - y)
                {
                    pixels[y * size + x] = new Color(1f, 1f, 1f, 1f);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    /// <summary>
    /// 创建十字光标纹理
    /// </summary>
    private static Texture2D CreateCrosshairCursor()
    {
        int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];

        int thickness = 2;
        int center = size / 2;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isHorizontal = Mathf.Abs(y - center) < thickness;
                bool isVertical = Mathf.Abs(x - center) < thickness;

                if (isHorizontal || isVertical)
                {
                    pixels[y * size + x] = new Color(1f, 1f, 1f, 1f);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    #endregion

    #region IInteractable Implementation

    public bool CanInteract => canBeDragged;

    public void OnInteractStart()
    {
        // 交互开始时的处理
    }

    public void OnInteractEnd()
    {
        // 交互结束时的处理
    }

    #endregion
    [Header("拖拽设置")]
    [SerializeField] public bool canBeDragged = true;
    [SerializeField] public bool highlightOnHover = true;
    [SerializeField] public float dragSpeed = 15f;
    [SerializeField] public float releaseVelocityMultiplier = 5f;

    [Header("视觉效果")]
    [SerializeField] public Color normalColor = Color.white;
    [SerializeField] public Color hoverColor = Color.yellow;
    [SerializeField] public Color draggingColor = Color.cyan;
    [SerializeField] public float hoverScale = 1.05f;
    [SerializeField] public float draggingScale = 1.1f;
    [SerializeField] public float scaleTransitionSpeed = 10f;

    [Header("音效")]
    [SerializeField] public AudioSource audioSource;
    [SerializeField] public AudioClip pickUpSound;
    [SerializeField] public AudioClip dropSound;
    [SerializeField] public AudioClip hoverSound;

    [Header("光标设置")]
    [Tooltip("悬停时的光标类型")]
    [SerializeField] public CursorType hoverCursor = CursorType.Grab;
    [Tooltip("拖拽时的光标类型")]
    [SerializeField] public CursorType draggingCursor = CursorType.Grabbing;
    [Tooltip("是否自动切换光标")]
    [SerializeField] public bool autoSwitchCursor = true;

    [Header("自定义光标图片")]
    [Tooltip("悬停时的自定义光标图片（优先使用）")]
    [SerializeField] public Texture2D customHoverCursor;
    [Tooltip("拖拽时的自定义光标图片（优先使用）")]
    [SerializeField] public Texture2D customDraggingCursor;
    [Tooltip("自定义光标的热点（点击位置）")]
    [SerializeField] public Vector2 customCursorHotspot = new Vector2(12, 12);

    [Header("R键重置设置")]
    [Tooltip("按R键是否重置位置")]
    [SerializeField] public bool enableRKeyReset = true;
    [Tooltip("R键重置方式")]
    [SerializeField] public ResetMode resetMode = ResetMode.DefaultPosition;
    [Tooltip("跟随目标（如玩家），用于 FollowTarget 模式")]
    [SerializeField] public Transform followTarget;
    [Tooltip("相对于跟随目标的偏移（FollowTarget 模式下使用，比如头顶上方2格）")]
    [SerializeField] public Vector3 followOffset = new Vector3(0, 1.5f, 0);

    /// <summary>
    /// 重置模式枚举
    /// </summary>
    public enum ResetMode
    {
        DefaultPosition,  // 回到初始位置
        FollowTarget      // 跟随目标的位置+偏移
    }

    // 组件引用
    private UIPhysicsElement physicsElement;
    private Canvas parentCanvas;
    private UnityEngine.UI.Image[] images;

    // 状态
    private bool isHovered;
    private bool isDragging;
    private Vector2 dragOffset;
    private Vector2 lastPosition;
    private Vector2 releaseVelocity;
    private float velocityUpdateInterval = 0.02f;
    private float lastVelocityUpdate;

    // 目标缩放
    private float targetScale = 1f;
    private float currentScale = 1f;
    private Vector3 initialScale = Vector3.one;

    // R键重置相关
    private Vector3 initialWorldPosition;
    private Vector2? initialAnchoredPosition;

    // 事件
    public System.Action<DraggableUI> OnDragStart;
    public System.Action<DraggableUI> OnDragEnd;
    public System.Action<DraggableUI, Vector2> OnDragUpdate;

    #region Unity 生命周期

    private void Awake()
    {
        physicsElement = GetComponent<UIPhysicsElement>();
        parentCanvas = GetComponentInParent<Canvas>();
        images = GetComponentsInChildren<UnityEngine.UI.Image>();

        // 存储初始颜色和缩放
        StoreInitialColors();
        
        // 检查physicsElement是否为null
        if (physicsElement != null && physicsElement.RectTransform != null)
        {
            initialScale = physicsElement.RectTransform.localScale;
            currentScale = 1f;

            // 记录初始位置
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
            {
                initialWorldPosition = physicsElement.RectTransform.position;
            }
            else
            {
                initialAnchoredPosition = physicsElement.RectTransform.anchoredPosition;
                initialWorldPosition = physicsElement.RectTransform.position;
            }
        }
        else
        {
            // 如果没有UIPhysicsElement组件，使用默认值
            initialScale = transform.localScale;
            currentScale = 1f;
            initialWorldPosition = transform.position;
        }
    }

    private void Start()
    {
        // 订阅物理元素事件
        if (physicsElement != null)
        {
            physicsElement.OnBecameGrounded += HandleLanded;
        }
    }

    private void Update()
    {
        // 平滑缩放过渡
        UpdateScale();

        // R键重置位置
        if (enableRKeyReset && Input.GetKeyDown(KeyCode.R))
        {
            ResetPosition();
        }

        // 计算释放速度
        if (isDragging && physicsElement != null && physicsElement.RectTransform != null)
        {
            // 每帧都记录位置，用于计算速度
            Vector2 currentPos;
            
            // World Space 模式下使用 position
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
            {
                currentPos = physicsElement.RectTransform.position;
            }
            else
            {
                currentPos = physicsElement.RectTransform.anchoredPosition;
            }
            
            // 计算瞬时速度
            Vector2 frameVelocity = (currentPos - lastPosition) / Time.deltaTime;
            
            // 限制最大速度，防止飞出去
            float maxSpeed = 10f; // 最大速度限制
            if (frameVelocity.magnitude > maxSpeed)
            {
                frameVelocity = frameVelocity.normalized * maxSpeed;
            }
            
            // 平滑速度变化
            releaseVelocity = Vector2.Lerp(releaseVelocity, frameVelocity, 0.2f);
            
            lastPosition = currentPos;
        }
    }

    private void OnDestroy()
    {
        if (physicsElement != null)
        {
            physicsElement.OnBecameGrounded -= HandleLanded;
        }
    }

    #endregion

    #region 公共接口

    /// <summary>
    /// 设置是否可拖拽
    /// </summary>
    public void SetDraggable(bool value)
    {
        canBeDragged = value;
    }

    /// <summary>
    /// 强制开始拖拽
    /// </summary>
    public void StartDragExternally()
    {
        if (!canBeDragged || physicsElement == null || physicsElement.RectTransform == null) return;

        Vector2 mousePos = GetWorldMousePosition();
        dragOffset = physicsElement.RectTransform.position - (Vector3)mousePos;
        physicsElement.StartDrag();
        isDragging = true;
        releaseVelocity = Vector2.zero;

        // 开始拖拽时取消漂浮，开始受重力影响
        physicsElement.isFloating = false;

        OnDragStart?.Invoke(this);
        PlaySound(pickUpSound);
        SetVisualState(VisualState.Dragging);
    }

/// <summary>
        /// 强制结束拖拽
        /// </summary>
        public void EndDragExternally()
        {
            if (!isDragging || physicsElement == null) return;

            physicsElement.EndDrag(releaseVelocity * releaseVelocityMultiplier);
            isDragging = false;

            OnDragEnd?.Invoke(this);
            PlaySound(dropSound);
            SetVisualState(VisualState.Normal);
        }

        /// <summary>
        /// R键重置位置（公共方法，也可外部调用）
        /// </summary>
        public void ResetPosition()
        {
            // 先结束拖拽状态
            if (isDragging)
            {
                isDragging = false;
                releaseVelocity = Vector2.zero;
            }

            // 停止物理运动（归零速度）
            if (physicsElement != null && physicsElement.RectTransform != null)
            {
                physicsElement.SetVelocity(Vector2.zero);

                switch (resetMode)
                {
                    case ResetMode.DefaultPosition:
                        // 回到初始位置
                        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
                        {
                            physicsElement.RectTransform.position = initialWorldPosition;
                        }
                        else if (initialAnchoredPosition.HasValue)
                        {
                            physicsElement.RectTransform.anchoredPosition = initialAnchoredPosition.Value;
                        }
                        break;

                    case ResetMode.FollowTarget:
                        // 跟随目标的位置+偏移
                        if (followTarget != null)
                        {
                            Vector3 targetPos = followTarget.transform.position + followOffset;

                            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
                            {
                                physicsElement.RectTransform.position = targetPos;
                            }
                            else
                            {
                                // Screen Space 模式：转换世界坐标到锚点坐标
                                Vector2 anchoredPos = WorldToAnchored(targetPos);
                                physicsElement.RectTransform.anchoredPosition = anchoredPos;
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[DraggableUI] FollowTarget 模式但未设置跟随目标，回退到默认位置");
                            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
                            {
                                physicsElement.RectTransform.position = initialWorldPosition;
                            }
                            else if (initialAnchoredPosition.HasValue)
                            {
                                physicsElement.RectTransform.anchoredPosition = initialAnchoredPosition.Value;
                            }
                        }
                        break;
                }

                // 重置视觉状态
                SetVisualState(VisualState.Normal);
                targetScale = 1f;
                currentScale = 1f;
                physicsElement.RectTransform.localScale = initialScale;

                Debug.Log($"[DraggableUI] R键重置位置完成，模式: {resetMode}");
            }
            else
            {
                // 如果没有physicsElement，使用transform
                switch (resetMode)
                {
                    case ResetMode.DefaultPosition:
                        transform.position = initialWorldPosition;
                        break;
                    case ResetMode.FollowTarget:
                        if (followTarget != null)
                        {
                            transform.position = followTarget.transform.position + followOffset;
                        }
                        else
                        {
                            transform.position = initialWorldPosition;
                        }
                        break;
                }
                // 重置视觉状态
                SetVisualState(VisualState.Normal);
                targetScale = 1f;
                currentScale = 1f;
                transform.localScale = initialScale;
            }
        }

    #endregion

    #region IPointer 接口

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!canBeDragged) return;

        isHovered = true;
        targetScale = hoverScale;

        if (highlightOnHover)
        {
            SetVisualState(VisualState.Hover);
            PlaySound(hoverSound);
        }

        // 切换光标
        if (autoSwitchCursor)
        {
            // 优先使用自定义图片
            if (customHoverCursor != null)
            {
                SetCustomCursor(customHoverCursor);
            }
            else
            {
                SetCursor(hoverCursor);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetScale = 1f;

        if (!isDragging && highlightOnHover)
        {
            SetVisualState(VisualState.Normal);
        }

        // 恢复默认光标
        if (autoSwitchCursor)
        {
            ResetCursor();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 右键点击切换固定模式
        if (eventData.button == PointerEventData.InputButton.Right && physicsElement != null)
        {
            physicsElement.ToggleFixed();
            
            // 播放音效
            PlaySound(hoverSound);
        }
    }

    #endregion

    #region IBeginDragHandler 接口

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 检查physicsElement是否存在
        if (physicsElement == null || physicsElement.RectTransform == null) return;
        
        // 固定模式下不能拖拽
        if (physicsElement.IsFixed) return;
        
        if (!canBeDragged) return;

        // 检查是否在可拖拽UI层
        eventData.selectedObject = gameObject;

        // 计算鼠标与元素中心的偏移
        Vector2 mousePos = GetWorldMousePosition();
        Vector2 elementPos = physicsElement.RectTransform.position;
        dragOffset = elementPos - mousePos;

        // 开始拖拽
        physicsElement.StartDrag();
        isDragging = true;
        releaseVelocity = Vector2.zero;
        
        // 开始拖拽时取消漂浮，开始受重力影响
        physicsElement.isFloating = false;

        // World Space 模式下使用 position 而不是 anchoredPosition
        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
        {
            lastPosition = physicsElement.RectTransform.position;
        }
        else
        {
            lastPosition = physicsElement.RectTransform.anchoredPosition;
        }

        // 更新层序，确保在最上层
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            physicsElement.RectTransform.SetAsLastSibling();
        }

        OnDragStart?.Invoke(this);
        PlaySound(pickUpSound);
        SetVisualState(VisualState.Dragging);
        targetScale = draggingScale;

        // 切换到抓取中光标
        if (autoSwitchCursor)
        {
            // 优先使用自定义图片
            if (customDraggingCursor != null)
            {
                SetCustomCursor(customDraggingCursor);
            }
            else
            {
                SetCursor(draggingCursor);
            }
        }
    }

    #endregion

    #region IDragHandler 接口

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || physicsElement == null || physicsElement.RectTransform == null) return;

        // 获取鼠标世界坐标
        Vector2 mousePos = GetWorldMousePosition();

        // 计算新位置（考虑偏移）
        Vector2 newWorldPos = mousePos + dragOffset;

        // World Space 模式下直接设置世界坐标
        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
        {
            physicsElement.RectTransform.position = newWorldPos;
            OnDragUpdate?.Invoke(this, newWorldPos);
        }
        else
        {
            // Screen Space 模式下转换为 anchoredPosition
            Vector2 newAnchoredPos = ScreenToAnchoredPosition(newWorldPos);
            physicsElement.Drag(newAnchoredPos);
            OnDragUpdate?.Invoke(this, newAnchoredPos);
        }
    }

    #endregion

    #region IEndDragHandler 接口

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging || physicsElement == null) return;

        // 结束拖拽，应用释放速度
        // World Space 模式下速度单位不同，需要调整
        float multiplier = releaseVelocityMultiplier;
        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
        {
            multiplier = 0.5f; // World Space 模式下减小速度倍数
        }
        
        Vector2 finalVelocity = releaseVelocity * multiplier;
        
        // 再次限制速度
        float maxReleaseSpeed = 8f;
        if (finalVelocity.magnitude > maxReleaseSpeed)
        {
            finalVelocity = finalVelocity.normalized * maxReleaseSpeed;
        }
        
        physicsElement.EndDrag(finalVelocity);
        isDragging = false;

        OnDragEnd?.Invoke(this);
        PlaySound(dropSound);

        if (isHovered)
        {
            SetVisualState(VisualState.Hover);
            targetScale = hoverScale;
            // 恢复悬停光标
            if (autoSwitchCursor)
            {
                SetCursor(hoverCursor);
            }
        }
        else
        {
            SetVisualState(VisualState.Normal);
            targetScale = 1f;
            // 恢复默认光标
            if (autoSwitchCursor)
            {
                ResetCursor();
            }
        }
    }

    #endregion

    #region 辅助方法

    private Vector2 GetWorldMousePosition()
    {
        Vector2 mousePos = Input.mousePosition;

        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                parentCanvas.transform as RectTransform,
                mousePos,
                parentCanvas.worldCamera,
                out Vector3 worldPos
            );
            return worldPos;
        }

        return mousePos;
    }

    private Vector2 ScreenToAnchoredPosition(Vector2 screenPos)
    {
        if (physicsElement == null || physicsElement.RectTransform == null) return screenPos;
        
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            physicsElement.RectTransform.parent as RectTransform,
            screenPos,
            parentCanvas?.worldCamera,
            out localPoint
        );
        return localPoint;
    }

    private void UpdateScale()
    {
        if (Mathf.Abs(currentScale - targetScale) > 0.001f)
        {
            currentScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * scaleTransitionSpeed);
            if (physicsElement != null && physicsElement.RectTransform != null)
            {
                physicsElement.RectTransform.localScale = initialScale * currentScale;
            }
            else
            {
                transform.localScale = initialScale * currentScale;
            }
        }
    }

    private void StoreInitialColors()
    {
        normalColor = images.Length > 0 && images[0].color != Color.clear
            ? images[0].color
            : Color.white;
    }

    private void SetVisualState(VisualState state)
    {
        Color targetColor = state switch
        {
            VisualState.Hover => hoverColor,
            VisualState.Dragging => draggingColor,
            _ => normalColor
        };

        foreach (var img in images)
        {
            if (img != null)
            {
                img.color = targetColor;
            }
        }
    }

    private void HandleLanded(UIPhysicsElement element)
    {
        // UI元素落地时的额外处理
        // 可以在这里添加落地音效、粒子效果等
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        audioSource.PlayOneShot(clip);
    }

    private Vector2 WorldToAnchored(Vector3 worldPos)
    {
        if (physicsElement == null || physicsElement.RectTransform == null) return worldPos;
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            physicsElement.RectTransform.parent as RectTransform,
            RectTransformUtility.WorldToScreenPoint(parentCanvas.worldCamera, worldPos),
            parentCanvas?.worldCamera,
            out Vector2 localPoint
        );
        return localPoint;
    }

    /// <summary>
    /// 设置跟随目标（运行时动态切换）
    /// </summary>
    public void SetFollowTarget(Transform target, Vector3? offset = null)
    {
        followTarget = target;
        if (offset.HasValue)
        {
            followOffset = offset.Value;
        }
    }

    #endregion

    #region 枚举

    private enum VisualState
    {
        Normal,
        Hover,
        Dragging
    }

    #endregion

    #region 编辑器支持

    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;

        // 编辑器预览
        images = GetComponentsInChildren<UnityEngine.UI.Image>();
        if (images.Length > 0)
        {
            normalColor = images[0].color;
        }
    }
    #endif

    #endregion
}
}
