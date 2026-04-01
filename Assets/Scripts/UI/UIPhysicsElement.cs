using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// UI物理元素基类
/// 让UI元素拥有刚体属性，可以参与物理碰撞
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIPhysicsElement : MonoBehaviour
{
    [Header("物理属性")]
    [SerializeField] protected float mass = 1f;
    [SerializeField] protected float drag = 0.5f;
    [SerializeField] protected float angularDrag = 0.5f;
    [SerializeField] protected bool useGravity = true;
    [SerializeField] protected bool isKinematic;

    [Header("碰撞属性")]
    [SerializeField] protected PhysicsMaterial2D material;
    [SerializeField] protected float bounciness = 0.3f;
    [SerializeField] protected float friction = 0.5f;

    [Header("边界")]
    [SerializeField] protected bool constrainToParent = true;
    [SerializeField] protected RectOffset boundaryPadding;

    // 物理状态
    protected Vector2 velocity;
    protected Vector2 angularVelocity;
    protected bool isBeingDragged;
    protected Vector2 lastPosition;

    // 组件
    protected RectTransform rectTransform;
    protected RectTransform parentRect;
    protected Image[] images;

    // 事件
    public System.Action<UIPhysicsElement, Collision2D> OnCollisionEnter2D;
    public System.Action<UIPhysicsElement, Collision2D> OnCollisionStay2D;
    public System.Action<UIPhysicsElement, Collision2D> OnCollisionExit2D;
    public System.Action<UIPhysicsElement> OnBecameGrounded;
    public System.Action<UIPhysicsElement> OnLeftGround;

    // 状态
    protected bool wasGrounded;

    #region Unity 生命周期

    protected virtual void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRect = transform.parent as RectTransform;
        images = GetComponentsInChildren<Image>();

        // 监听全局物理设置变化
        if (GlobalPhysicsSettings.Instance != null)
        {
            GlobalPhysicsSettings.Instance.OnGravityChanged += OnGlobalGravityChanged;
        }
    }

    protected virtual void Start()
    {
        lastPosition = rectTransform.anchoredPosition;
    }

    protected virtual void Update()
    {
        if (isBeingDragged || isKinematic) return;

        // 应用速度到位置
        ApplyVelocity();

        // 应用边界约束
        ApplyBoundaryConstraints();

        // 更新最后位置
        lastPosition = rectTransform.anchoredPosition;

        // 检查是否着地
        CheckGroundedState();
    }

    protected virtual void FixedUpdate()
    {
        if (isBeingDragged || isKinematic) return;

        // 应用重力
        ApplyGravity();

        // 应用阻力
        ApplyDrag();
    }

    protected virtual void OnDestroy()
    {
        if (GlobalPhysicsSettings.Instance != null)
        {
            GlobalPhysicsSettings.Instance.OnGravityChanged -= OnGlobalGravityChanged;
        }
    }

    #endregion

    #region 物理逻辑

    protected virtual void ApplyGravity()
    {
        if (!useGravity) return;

        float gravity = GlobalPhysicsSettings.Instance != null
            ? GlobalPhysicsSettings.Instance.GetUIPhysicsGravity()
            : Physics2D.gravity.y;

        velocity.y += gravity * Time.fixedDeltaTime;
    }

    protected virtual void ApplyVelocity()
    {
        // 使用平滑插值让移动更自然
        Vector2 targetPosition = rectTransform.anchoredPosition + velocity * Time.deltaTime;
        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition,
            targetPosition,
            Time.deltaTime * 10f
        );
    }

    protected virtual void ApplyDrag()
    {
        velocity *= (1f - drag * Time.fixedDeltaTime);
        angularVelocity *= (1f - angularDrag * Time.fixedDeltaTime);

        // 速度过小时归零
        if (velocity.magnitude < 0.01f) velocity = Vector2.zero;
        if (angularVelocity.magnitude < 0.01f) angularVelocity = Vector2.zero;
    }

    protected virtual void ApplyBoundaryConstraints()
    {
        if (!constrainToParent || parentRect == null) return;

        // 获取父物体边界
        Vector2 parentSize = parentRect.rect.size;
        Vector2 elementSize = rectTransform.rect.size;

        Vector2 minBoundary = new Vector2(
            -parentSize.x / 2 + elementSize.x / 2 + (boundaryPadding?.left ?? 0),
            -parentSize.y / 2 + elementSize.y / 2 + (boundaryPadding?.bottom ?? 0)
        );
        Vector2 maxBoundary = new Vector2(
            parentSize.x / 2 - elementSize.x / 2 - (boundaryPadding?.right ?? 0),
            parentSize.y / 2 - elementSize.y / 2 - (boundaryPadding?.top ?? 0)
        );

        Vector2 currentPos = rectTransform.anchoredPosition;
        Vector2 constrainedPos = Vector2.Max(minBoundary, Vector2.Min(maxBoundary, currentPos));

        // 边界碰撞检测和反弹
        if (currentPos.x < minBoundary.x || currentPos.x > maxBoundary.x)
        {
            velocity.x *= -bounciness;
            constrainedPos.x = Mathf.Clamp(currentPos.x, minBoundary.x, maxBoundary.x);
        }

        if (currentPos.y < minBoundary.y || currentPos.y > maxBoundary.y)
        {
            velocity.y *= -bounciness;
            constrainedPos.y = Mathf.Clamp(currentPos.y, minBoundary.y, maxBoundary.y);

            // 着地检测
            if (currentPos.y <= minBoundary.y && velocity.y <= 0)
            {
                OnLanded();
            }
        }

        rectTransform.anchoredPosition = constrainedPos;
    }

    protected virtual void CheckGroundedState()
    {
        bool isGrounded = IsGrounded();

        if (isGrounded && !wasGrounded)
        {
            OnBecameGrounded?.Invoke(this);
        }
        else if (!isGrounded && wasGrounded)
        {
            OnLeftGround?.Invoke(this);
        }

        wasGrounded = isGrounded;
    }

    protected virtual bool IsGrounded()
    {
        if (parentRect == null) return false;

        Vector2 parentSize = parentRect.rect.size;
        Vector2 elementSize = rectTransform.rect.size;
        float minY = -parentSize.y / 2 + elementSize.y / 2 + (boundaryPadding?.bottom ?? 0);

        return rectTransform.anchoredPosition.y <= minY + 0.5f && velocity.y <= 0;
    }

    protected virtual void OnLanded()
    {
        // 触发碰撞事件
        var collision = new Collision2D();
        collision.gameObject = gameObject;
        OnCollisionEnter2D?.Invoke(this, collision);
    }

    #endregion

    #region 拖拽接口

    /// <summary>
    /// 开始被拖拽
    /// </summary>
    public virtual void StartDrag()
    {
        isBeingDragged = true;
        velocity = Vector2.zero;
        angularVelocity = Vector2.zero;
    }

    /// <summary>
    /// 拖拽中更新位置
    /// </summary>
    public virtual void Drag(Vector2 position)
    {
        if (!isBeingDragged) return;
        rectTransform.anchoredPosition = position;
        lastPosition = position;
    }

    /// <summary>
    /// 结束拖拽，释放元素
    /// </summary>
    public virtual void EndDrag(Vector2 releaseVelocity)
    {
        isBeingDragged = false;
        velocity = releaseVelocity;
    }

    #endregion

    #region 物理影响接口

    /// <summary>
    /// 添加冲击力
    /// </summary>
    public virtual void AddForce(Vector2 force)
    {
        velocity += force / mass;
    }

    /// <summary>
    /// 设置速度
    /// </summary>
    public virtual void SetVelocity(Vector2 newVelocity)
    {
        velocity = newVelocity;
    }

    /// <summary>
    /// 获取当前速度
    /// </summary>
    public Vector2 GetVelocity() => velocity;

    /// <summary>
    /// 设置是否使用重力
    /// </summary>
    public void SetUseGravity(bool value)
    {
        useGravity = value;
    }

    /// <summary>
    /// 设置是否为运动学（不受物理影响）
    /// </summary>
    public void SetKinematic(bool value)
    {
        isKinematic = value;
    }

    #endregion

    #region 回调

    protected virtual void OnGlobalGravityChanged(float newGravity)
    {
        // 当全局重力改变时更新
    }

    #endregion

    #region 公共属性

    public bool IsBeingDragged => isBeingDragged;
    public bool IsGroundedElement => IsGrounded();
    public RectTransform RectTransform => rectTransform;

    #endregion

    #region 编辑器可视化

    protected virtual void OnDrawGizmosSelected()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        Gizmos.color = isBeingDragged ? Color.yellow : (IsGrounded() ? Color.green : Color.blue);
        Gizmos.DrawWireCube(rectTransform.position, rectTransform.rect.size);
    }

    #endregion
}

/// <summary>
/// 简单的碰撞信息类（用于事件）
/// </summary>
public class Collision2D
{
    public GameObject gameObject;
    public Transform transform;
    public Vector2 relativeVelocity;
    public ContactFilter2D contactFilter;
}
