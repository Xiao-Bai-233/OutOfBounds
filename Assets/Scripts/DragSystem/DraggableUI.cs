using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 可拖拽的UI组件
/// 处理鼠标/触摸拖拽交互
/// </summary>
[RequireComponent(typeof(UIPhysicsElement))]
public class DraggableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("拖拽设置")]
    [SerializeField] private bool canBeDragged = true;
    [SerializeField] private bool highlightOnHover = true;
    [SerializeField] private float dragSpeed = 1f;
    [SerializeField] private float releaseVelocityMultiplier = 1f;

    [Header("视觉效果")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color draggingColor = Color.cyan;
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float draggingScale = 1.1f;
    [SerializeField] private float scaleTransitionSpeed = 10f;

    [Header("音效")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pickUpSound;
    [SerializeField] private AudioClip dropSound;
    [SerializeField] private AudioClip hoverSound;

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

    // 事件
    public System.Action<DraggableUI> OnDragStart;
    public System.Action<DraggableUI> OnDragEnd;
    public System.Action<DraggableUI, Vector2> OnDrag;

    #region Unity 生命周期

    private void Awake()
    {
        physicsElement = GetComponent<UIPhysicsElement>();
        parentCanvas = GetComponentInParent<Canvas>();
        images = GetComponentsInChildren<UnityEngine.UI.Image>();

        // 存储初始颜色
        StoreInitialColors();
    }

    private void Start()
    {
        // 订阅物理元素事件
        physicsElement.OnBecameGrounded += HandleLanded;
    }

    private void Update()
    {
        // 平滑缩放过渡
        UpdateScale();

        // 计算释放速度
        if (isDragging)
        {
            if (Time.time - lastVelocityUpdate > velocityUpdateInterval)
            {
                releaseVelocity = (physicsElement.RectTransform.anchoredPosition - lastPosition) / velocityUpdateInterval;
                lastPosition = physicsElement.RectTransform.anchoredPosition;
                lastVelocityUpdate = Time.time;
            }
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
        if (!canBeDragged) return;

        Vector2 mousePos = GetWorldMousePosition();
        dragOffset = physicsElement.RectTransform.position - (Vector3)mousePos;
        physicsElement.StartDrag();
        isDragging = true;
        releaseVelocity = Vector2.zero;

        OnDragStart?.Invoke(this);
        PlaySound(pickUpSound);
        SetVisualState(VisualState.Dragging);
    }

    /// <summary>
    /// 强制结束拖拽
    /// </summary>
    public void EndDragExternally()
    {
        if (!isDragging) return;

        physicsElement.EndDrag(releaseVelocity * releaseVelocityMultiplier);
        isDragging = false;

        OnDragEnd?.Invoke(this);
        PlaySound(dropSound);
        SetVisualState(VisualState.Normal);
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
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetScale = 1f;

        if (!isDragging && highlightOnHover)
        {
            SetVisualState(VisualState.Normal);
        }
    }

    #endregion

    #region IBeginDragHandler 接口

    public void OnBeginDrag(PointerEventData eventData)
    {
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
        lastPosition = physicsElement.RectTransform.anchoredPosition;

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
    }

    #endregion

    #region IDragHandler 接口

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // 获取鼠标世界坐标
        Vector2 mousePos = GetWorldMousePosition();

        // 计算新位置（考虑偏移和缩放）
        Vector2 newWorldPos = mousePos + dragOffset;
        Vector2 newAnchoredPos = ScreenToAnchoredPosition(newWorldPos);

        // 应用拖拽速度缩放
        newAnchoredPos = Vector2.Lerp(
            physicsElement.RectTransform.anchoredPosition,
            newAnchoredPos,
            dragSpeed * Time.deltaTime * 60f
        );

        physicsElement.Drag(newAnchoredPos);
        OnDrag?.Invoke(this, newAnchoredPos);
    }

    #endregion

    #region IEndDragHandler 接口

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // 结束拖拽，应用释放速度
        physicsElement.EndDrag(releaseVelocity * releaseVelocityMultiplier);
        isDragging = false;

        OnDragEnd?.Invoke(this);
        PlaySound(dropSound);

        if (isHovered)
        {
            SetVisualState(VisualState.Hover);
            targetScale = hoverScale;
        }
        else
        {
            SetVisualState(VisualState.Normal);
            targetScale = 1f;
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
            physicsElement.RectTransform.localScale = Vector3.one * currentScale;
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
