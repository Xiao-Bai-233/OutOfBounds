using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using OutOfBounds.Core;
using OutOfBounds.UI;
using OutOfBounds.Data;
using OutOfBounds.Puzzle;

namespace OutOfBounds.DragSystem
{
    public enum CursorType
    {
        Default,
        Grab,
        Grabbing,
        Pointer,
        Crosshair
    }

[RequireComponent(typeof(UIPhysicsElement))]
public class DraggableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IInteractable
{
    #region 静态光标管理

    private static CursorType currentCursor = CursorType.Default;

    public static void SetCursor(CursorType cursor)
    {
        if (currentCursor == cursor) return;
        currentCursor = cursor;

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

    private void SetCustomCursor(Texture2D cursorTexture)
    {
        if (cursorTexture != null)
        {
            Cursor.SetCursor(cursorTexture, customCursorHotspot, CursorMode.Auto);
        }
        else
        {
            ResetCursor();
        }
    }

    public static void ResetCursor()
    {
        currentCursor = CursorType.Default;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private static Texture2D CreateGrabCursor()
    {
        int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float centerX = size / 2f;
                float centerY = size / 2f;
                float dx = (x - centerX) / (size / 4f);
                float dy = (y - centerY) / (size / 4f);
                float dist = dx * dx + dy * dy;

                if (dist < 1f)
                {
                    pixels[y * size + x] = new Color(1f, 1f, 1f, 1f);
                }
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

    private static Texture2D CreateGrabbingCursor()
    {
        int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float centerX = size / 2f;
                float centerY = size / 2.5f;
                float dx = (x - centerX) / (size / 4f);
                float dy = (y - centerY) / (size / 3f);
                float dist = dx * dx + dy * dy;

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

    private static Texture2D CreatePointerCursor()
    {
        int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];

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
    }

    public void OnInteractEnd()
    {
    }

    #endregion

    [Header("══ 拖拽设置 ══")]
    [Tooltip("是否可以被拖拽")]
    [SerializeField] public bool canBeDragged = true;

    [Tooltip("鼠标悬停时是否高亮")]
    [SerializeField] public bool highlightOnHover = true;

    [Tooltip("拖拽跟随速度（越大越跟手）")]
    [SerializeField] public float dragSpeed = 15f;

    [Tooltip("松手时速度放大倍数")]
    [SerializeField] public float releaseVelocityMultiplier = 5f;

    [Header("══ UI 类型配置 ══")]
    [Tooltip("UI 类型配置文件 — 拖入 .asset 即可自动加载物理参数")]
    [SerializeField] private UITypeProfile typeProfile;

    [Tooltip("上下文限制 — 控制 UI 离开有效区域后的行为（防逃课）")]
    [SerializeField] private UIContextConstraint contextConstraint;

    [Header("══ 视觉效果 ══")]
    [Tooltip("正常状态颜色")]
    [SerializeField] public Color normalColor = Color.white;

    [Tooltip("悬停时颜色")]
    [SerializeField] public Color hoverColor = Color.yellow;

    [Tooltip("拖拽中颜色")]
    [SerializeField] public Color draggingColor = Color.cyan;

    [Tooltip("悬停时放大比例")]
    [SerializeField] public float hoverScale = 1.05f;

    [Tooltip("拖拽时放大比例")]
    [SerializeField] public float draggingScale = 1.1f;

    [Tooltip("缩放过渡速度")]
    [SerializeField] public float scaleTransitionSpeed = 10f;

    [Header("══ 音效 ══")]
    [SerializeField] public AudioSource audioSource;
    [Tooltip("拿起音效")]
    [SerializeField] public AudioClip pickUpSound;
    [Tooltip("放下音效")]
    [SerializeField] public AudioClip dropSound;
    [Tooltip("悬停音效")]
    [SerializeField] public AudioClip hoverSound;

    [Header("══ 光标设置 ══")]
    [Tooltip("悬停时的光标类型")]
    [SerializeField] public CursorType hoverCursor = CursorType.Grab;
    [Tooltip("拖拽时的光标类型")]
    [SerializeField] public CursorType draggingCursor = CursorType.Grabbing;
    [Tooltip("是否自动切换光标形状")]
    [SerializeField] public bool autoSwitchCursor = true;
    [Tooltip("悬停时的自定义光标图片")]
    [SerializeField] public Texture2D customHoverCursor;
    [Tooltip("拖拽时的自定义光标图片")]
    [SerializeField] public Texture2D customDraggingCursor;
    [Tooltip("自定义光标的热点位置")]
    [SerializeField] public Vector2 customCursorHotspot = new Vector2(12, 12);

    [Header("══ R 键重置 ══")]
    [Tooltip("是否允许按 R 键重置位置")]
    [SerializeField] public bool enableRKeyReset = true;
    [Tooltip("R 键重置方式")]
    [SerializeField] public ResetMode resetMode = ResetMode.DefaultPosition;
    [Tooltip("跟随目标（用于 FollowTarget 模式）")]
    [SerializeField] public Transform followTarget;
    [Tooltip("跟随目标的偏移量")]
    [SerializeField] public Vector3 followOffset = new Vector3(0, 1.5f, 0);

    public enum ResetMode
    {
        [Tooltip("回到初始位置")]
        DefaultPosition,
        [Tooltip("跟随目标位置")]
        FollowTarget
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
    private Vector2 lastMousePosition;
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

    // 右键拖拽锁定标记（防止 OnPointerClick 重复触发 ToggleFixed）
    private bool rightClickDropHappened;

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
        contextConstraint = GetComponent<UIContextConstraint>();

        StoreInitialColors();
        ApplyTypeProfile();

        if (physicsElement != null && physicsElement.RectTransform != null)
        {
            initialScale = physicsElement.RectTransform.localScale;
            currentScale = 1f;

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
            initialScale = transform.localScale;
            currentScale = 1f;
            initialWorldPosition = transform.position;
        }
    }

    private void Start()
    {
        if (physicsElement != null)
        {
            physicsElement.OnBecameGrounded += HandleLanded;
        }
    }

    private void Update()
    {
        UpdateScale();

        if (enableRKeyReset && Input.GetKeyDown(KeyCode.R))
        {
            ResetPosition();
        }

        // ★ 拖拽中按住右键 → 立即结束拖拽并固定气泡
        if (isDragging && Input.GetMouseButtonDown(1))
        {
            RightClickDrop();
        }

        if (isDragging && physicsElement != null && physicsElement.RectTransform != null)
        {
            Vector2 currentMousePos = GetWorldMousePosition();
            Vector2 mouseFrameVelocity = (currentMousePos - lastMousePosition) / Time.deltaTime;
            
            float maxSpeed = 15f; 
            if (mouseFrameVelocity.magnitude > maxSpeed)
            {
                mouseFrameVelocity = mouseFrameVelocity.normalized * maxSpeed;
            }
            
            releaseVelocity = Vector2.Lerp(releaseVelocity, mouseFrameVelocity, 0.35f);
            lastMousePosition = currentMousePos;
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

    public void SetDraggable(bool value)
    {
        canBeDragged = value;
    }

    public bool IsBeingDragged => isDragging;

    public void StartDragExternally()
    {
        if (!canBeDragged || physicsElement == null || physicsElement.RectTransform == null) return;

        Vector2 mousePos = GetWorldMousePosition();
        dragOffset = physicsElement.RectTransform.position - (Vector3)mousePos;
        physicsElement.StartDrag();
        isDragging = true;
        releaseVelocity = Vector2.zero;

        OnDragStart?.Invoke(this);
        PlaySound(pickUpSound);
        SetVisualState(VisualState.Dragging);
    }

    public void EndDragExternally()
    {
        if (!isDragging || physicsElement == null) return;

        physicsElement.EndDrag(releaseVelocity * releaseVelocityMultiplier);
        isDragging = false;

        OnDragEnd?.Invoke(this);
        PlaySound(dropSound);
        SetVisualState(VisualState.Normal);
    }

    public void ApplyTypeProfile()
    {
        if (typeProfile == null) return;

        if (physicsElement != null)
            typeProfile.ApplyTo(physicsElement);

        typeProfile.ApplyTo(this);

        Debug.Log($"[DraggableUI] {name} 已应用 UI 类型配置: {typeProfile.uiType}");
    }

    private void CheckContextConstraint()
    {
        if (contextConstraint == null || !contextConstraint.IsEnabled) return;
        contextConstraint.OnDragEnded(this);
    }

    public void ResetPosition()
    {
        if (isDragging)
        {
            isDragging = false;
            releaseVelocity = Vector2.zero;
        }

        if (physicsElement != null && physicsElement.RectTransform != null)
        {
            physicsElement.SetVelocity(Vector2.zero);

            if (physicsElement.IsPhysicsLocked)
            {
                physicsElement.SetPhysicsLocked(false);
                physicsElement.SetColliderEnabled(true);
                Debug.Log($"[DraggableUI] {name} 已解锁物理并恢复碰撞器");
            }

            switch (resetMode)
            {
                case ResetMode.DefaultPosition:
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
                    if (followTarget != null)
                    {
                        Vector3 targetPos = followTarget.transform.position + followOffset;

                        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
                        {
                            physicsElement.RectTransform.position = targetPos;
                        }
                        else
                        {
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

            SetVisualState(VisualState.Normal);
            targetScale = 1f;
            currentScale = 1f;
            physicsElement.RectTransform.localScale = initialScale;

            Debug.Log($"[DraggableUI] R键重置位置完成，模式: {resetMode}");
        }
        else
        {
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

        if (autoSwitchCursor)
        {
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

        if (autoSwitchCursor)
        {
            ResetCursor();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 如果刚用右键结束拖拽并固定，跳过这次点击防止重复触发
        if (rightClickDropHappened)
        {
            rightClickDropHappened = false;
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right && physicsElement != null)
        {
            physicsElement.ToggleFixed();
            PlaySound(hoverSound);
        }
    }

    #endregion

    #region IBeginDragHandler 接口

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (physicsElement == null || physicsElement.RectTransform == null) return;
        if (physicsElement.IsFixed) return;
        if (!canBeDragged) return;

        if (UIPhysicsManager.Instance != null &&
            UIPhysicsManager.Instance.IsOverlappingWithFixed(this))
        {
            Debug.Log($"[DraggableUI] {name} 卡在墙壁/地板中，按 R 键重置后才能拖拽");
            return;
        }

        eventData.selectedObject = gameObject;

        Vector2 mousePos = GetWorldMousePosition();
        Vector2 elementPos = physicsElement.RectTransform.position;
        dragOffset = elementPos - mousePos;

        physicsElement.StartDrag();
        isDragging = true;
        releaseVelocity = Vector2.zero;
        lastMousePosition = GetWorldMousePosition();

        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
        {
            lastPosition = physicsElement.RectTransform.position;
        }
        else
        {
            lastPosition = physicsElement.RectTransform.anchoredPosition;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            physicsElement.RectTransform.SetAsLastSibling();
        }

        OnDragStart?.Invoke(this);
        PlaySound(pickUpSound);
        SetVisualState(VisualState.Dragging);
        targetScale = draggingScale;

        if (autoSwitchCursor)
        {
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

        Vector2 mousePos = GetWorldMousePosition();
        Vector2 newWorldPos = mousePos + dragOffset;

        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
        {
            physicsElement.RectTransform.position = newWorldPos;
            OnDragUpdate?.Invoke(this, newWorldPos);
        }
        else
        {
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

        Vector2 currentMousePos = GetWorldMousePosition();
        Vector2 finalFrameVelocity = (currentMousePos - lastMousePosition) / Mathf.Max(Time.deltaTime, 0.001f);
        
        float maxReleaseSpeed = 20f;
        if (finalFrameVelocity.magnitude > maxReleaseSpeed)
        {
            finalFrameVelocity = finalFrameVelocity.normalized * maxReleaseSpeed;
        }
        
        Vector2 throwVelocity = Vector2.Lerp(releaseVelocity, finalFrameVelocity, 0.6f);
        throwVelocity *= releaseVelocityMultiplier;
        
        physicsElement.EndDrag(throwVelocity);
        isDragging = false;

        OnDragEnd?.Invoke(this);
        PlaySound(dropSound);

        if (isHovered)
        {
            SetVisualState(VisualState.Hover);
            targetScale = hoverScale;
            if (autoSwitchCursor)
            {
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
        else
        {
            SetVisualState(VisualState.Normal);
            targetScale = 1f;
            if (autoSwitchCursor)
            {
                ResetCursor();
            }
        }

        physicsElement.autoLockOnWallCollision = true;
        StartCoroutine(DisableAutoLockAfterFrames(2));
        CheckContextConstraint();
    }

    private System.Collections.IEnumerator DisableAutoLockAfterFrames(int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            yield return null;
        }

        if (physicsElement != null)
        {
            if (!physicsElement.IsPhysicsLocked)
            {
                physicsElement.autoLockOnWallCollision = false;
            }
        }
    }

    #endregion

    #region 右键锁定

    /// <summary>
    /// 拖拽中按下右键 → 结束拖拽并固定气泡在当前位置
    /// </summary>
    private void RightClickDrop()
    {
        if (physicsElement == null) return;

        // 标记防重复（OnPointerClick 也会响应右键）
        rightClickDropHappened = true;

        // 1. 结束当前拖拽
        isDragging = false;
        physicsElement.EndDrag(Vector2.zero);

        // 2. 将气泡固定（停止物理、变灰、禁止再拖）
        if (!physicsElement.IsFixed)
            physicsElement.ToggleFixed();

        // 3. 恢复视觉状态
        SetVisualState(VisualState.Normal);
        targetScale = 1f;
        currentScale = 1f;
        if (physicsElement.RectTransform != null)
            physicsElement.RectTransform.localScale = initialScale;

        OnDragEnd?.Invoke(this);
        PlaySound(dropSound);

        Debug.Log($"[DraggableUI] {name} 右键拖拽锁定");
    }

    #endregion

    #region 辅助方法

    private Vector2 GetWorldMousePosition()
    {
        Vector2 mousePos = Input.mousePosition;

        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            if (parentCanvas.worldCamera == null) return mousePos;

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
