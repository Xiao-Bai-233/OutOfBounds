using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using OutOfBounds.Core;
using OutOfBounds.Physics;
using OutOfBounds.Utils;

namespace OutOfBounds.UI
{
/// <summary>
/// UI物理元素基类
/// 让UI元素拥有刚体属性，可以参与物理碰撞
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIPhysicsElement : MonoBehaviour, IPhysicsObject
{
    [Header("物理属性")]
    [SerializeField] public float mass = 1f;
    [SerializeField] public float drag = 1.5f; // 进一步增加阻力
    [SerializeField] public float angularDrag = 1f;
    [SerializeField] public bool useGravity = true;
    [SerializeField] public bool isKinematic;
    [SerializeField] public float gravityScale = 0.3f; // 降低重力
    [SerializeField] public bool isFloating = true; // 是否漂浮（不受重力影响）
    
    [Header("固定模式")]
    [SerializeField] public bool isFixed = false; // 是否固定（作为地面/墙壁）
    [SerializeField] public Color fixedColor = Color.gray; // 固定时的颜色
    [SerializeField] public Color normalColor = Color.white; // 正常颜色
    protected Color originalColor;

    [Header("碰撞属性")]
    [SerializeField] public PhysicsMaterial2D material;
    [SerializeField] public float bounciness = 0.2f; // 进一步降低弹性
    [SerializeField] public float friction = 0.3f;
    [SerializeField] public LayerMask collisionLayers = ~0; // 默认和所有层碰撞
    [SerializeField] public float collisionCheckDistance = 0.05f; // 进一步减小检测距离
    [SerializeField] public bool isPlatform = true; // 是否作为平台（允许玩家站上去）

    [Header("边界")]
    [SerializeField] protected bool constrainToParent = true;
    [SerializeField] protected RectOffset boundaryPadding;

    // 物理状态
    protected Vector2 velocity;
    protected float angularVelocity; // 改为标量，2D旋转只有Z轴
    protected float rotation; // 当前旋转角度
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

        // 保存原始颜色
        if (images.Length > 0)
        {
            originalColor = images[0].color;
        }
    }

    protected virtual void OnEnable()
    {
        // 监听全局物理设置变化
        if (GlobalPhysicsSettings.Instance != null)
        {
            GlobalPhysicsSettings.Instance.OnGravityChanged += OnGlobalGravityChanged;
        }
    }

    protected virtual void OnDisable()
    {
        if (GlobalPhysicsSettings.Instance != null)
        {
            GlobalPhysicsSettings.Instance.OnGravityChanged -= OnGlobalGravityChanged;
        }
    }

    protected virtual void Start()
    {
        lastPosition = rectTransform.anchoredPosition;
    }

    /// <summary>
    /// 重置物理状态，用于对象池重用
    /// </summary>
    public virtual void ResetPhysics()
    {
        velocity = Vector2.zero;
        angularVelocity = 0f;
        rotation = 0f;
        isBeingDragged = false;
        wasGrounded = false;
        isFixed = false;
        
        if (rectTransform != null)
        {
            rectTransform.rotation = Quaternion.identity;
        }

        if (images != null && images.Length > 0)
        {
            images[0].color = originalColor;
        }
    }

    protected virtual void Update()
    {
        // 固定模式下不更新物理
        if (isFixed) return;
        
        if (isBeingDragged || isKinematic) return;

        // 应用旋转
        ApplyRotation();

        // 子步进碰撞检测，防止高速穿模
        int subSteps = 4;
        Vector2 totalVelocity = velocity * Time.deltaTime;
        Vector2 stepVelocity = totalVelocity / subSteps;
        
        for (int i = 0; i < subSteps; i++)
        {
            // 先检测碰撞（基于当前位置）
            CheckSceneCollisionsBoxCast();
            
            // 再移动一小步
            ApplyVelocitySubStep(stepVelocity);
            
            // 应用边界约束
            ApplyBoundaryConstraints();
        }

        // 更新最后位置
        lastPosition = rectTransform.position;

        // 检查是否着地
        CheckGroundedState();
    }

    /// <summary>
    /// 子步进移动
    /// </summary>
    protected virtual void ApplyVelocitySubStep(Vector2 stepVelocity)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            rectTransform.position += (Vector3)stepVelocity;
        }
        else
        {
            rectTransform.anchoredPosition += stepVelocity;
        }
    }

    protected virtual void FixedUpdate()
    {
        if (isBeingDragged || isKinematic) return;

        // 应用重力
        ApplyGravity();

        // 应用阻力
        ApplyDrag();
        
        // 稳定性处理：速度和角速度很小时完全停止
        // 增加阈值，防止微小颤动
        if (velocity.magnitude < 0.15f)
        {
            velocity = Vector2.zero;
        }
        
        if (Mathf.Abs(angularVelocity) < 2f)
        {
            angularVelocity = 0f;
        }
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
        if (useGravity && !isFloating)
        {
            // 如果已经在地面上且速度很小，停止累积重力速度
            if (IsGrounded() && velocity.y <= 0.01f)
            {
                // 维持一个微小的向下速度以保持着地状态，但不累积
                velocity.y = Mathf.Max(velocity.y, -0.1f);
                return;
            }

            // 应用重力
            float gravity = GlobalPhysicsSettings.Instance != null 
                ? GlobalPhysicsSettings.Instance.GetUIPhysicsGravity() 
                : -15f;
            velocity.y += gravity * gravityScale * Time.fixedDeltaTime;
        }
    }

    protected virtual void ApplyRotation()
    {
        // 应用旋转
        rotation += angularVelocity * Time.deltaTime;
        rectTransform.rotation = Quaternion.Euler(0, 0, rotation);
    }

    protected virtual void ApplyVelocity()
    {
        // World Space 模式下直接修改 position
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            // 直接设置位置，不使用 Lerp 避免累积误差
            Vector2 newPosition = (Vector2)rectTransform.position + velocity * Time.deltaTime;
            rectTransform.position = newPosition;
        }
        else
        {
            // Screen Space 模式下使用 anchoredPosition
            Vector2 targetPosition = rectTransform.anchoredPosition + velocity * Time.deltaTime;
            rectTransform.anchoredPosition = Vector2.Lerp(
                rectTransform.anchoredPosition,
                targetPosition,
                Time.deltaTime * 10f
            );
        }
    }

    protected virtual void ApplyDrag()
    {
        velocity *= (1f - drag * Time.fixedDeltaTime);
        angularVelocity *= (1f - angularDrag * Time.fixedDeltaTime);

        // 速度过小时归零
        if (velocity.magnitude < 0.01f) velocity = Vector2.zero;
        if (Mathf.Abs(angularVelocity) < 0.1f) angularVelocity = 0;
    }

    protected virtual void ApplyBoundaryConstraints()
    {
        if (!constrainToParent || parentRect == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        Vector2 minBoundary, maxBoundary;
        Vector2 currentPos;

        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            // World Space 模式下使用 Canvas 的世界坐标边界
            Vector3[] worldCorners = new Vector3[4];
            parentRect.GetWorldCorners(worldCorners);
            
            float minX = Mathf.Min(worldCorners[0].x, worldCorners[1].x, worldCorners[2].x, worldCorners[3].x);
            float maxX = Mathf.Max(worldCorners[0].x, worldCorners[1].x, worldCorners[2].x, worldCorners[3].x);
            float minY = Mathf.Min(worldCorners[0].y, worldCorners[1].y, worldCorners[2].y, worldCorners[3].y);
            float maxY = Mathf.Max(worldCorners[0].y, worldCorners[1].y, worldCorners[2].y, worldCorners[3].y);

            Vector2 elementSize = rectTransform.rect.size * rectTransform.lossyScale;
            
            minBoundary = new Vector2(minX + elementSize.x / 2, minY + elementSize.y / 2);
            maxBoundary = new Vector2(maxX - elementSize.x / 2, maxY - elementSize.y / 2);
            currentPos = rectTransform.position;

            // 边界碰撞检测和反弹
            bool hitBoundary = false;
            if (currentPos.x < minBoundary.x || currentPos.x > maxBoundary.x)
            {
                velocity.x *= -bounciness;
                currentPos.x = Mathf.Clamp(currentPos.x, minBoundary.x, maxBoundary.x);
                hitBoundary = true;
            }

            if (currentPos.y < minBoundary.y || currentPos.y > maxBoundary.y)
            {
                velocity.y *= -bounciness;
                currentPos.y = Mathf.Clamp(currentPos.y, minBoundary.y, maxBoundary.y);
                hitBoundary = true;
            }

            if (hitBoundary)
            {
                rectTransform.position = currentPos;
            }
        }
        else
        {
            // Screen Space 模式下使用 anchoredPosition
            Vector2 parentSize = parentRect.rect.size;
            Vector2 elementSize = rectTransform.rect.size;

            minBoundary = new Vector2(
                -parentSize.x / 2 + elementSize.x / 2 + (boundaryPadding?.left ?? 0),
                -parentSize.y / 2 + elementSize.y / 2 + (boundaryPadding?.bottom ?? 0)
            );
            maxBoundary = new Vector2(
                parentSize.x / 2 - elementSize.x / 2 - (boundaryPadding?.right ?? 0),
                parentSize.y / 2 - elementSize.y / 2 - (boundaryPadding?.top ?? 0)
            );

            currentPos = rectTransform.anchoredPosition;
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

                if (currentPos.y <= minBoundary.y && velocity.y <= 0)
                {
                    OnLanded();
                }
            }

            rectTransform.anchoredPosition = constrainedPos;
        }
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
        // World Space 模式下使用射线检测地面
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            Vector2 worldPos = GetWorldPosition();
            Vector2 size = GetColliderSize();
            float checkDistance = size.y * 0.6f;
            
            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.down, checkDistance, collisionLayers);
            return hit.collider != null && velocity.y <= 0;
        }

        // Screen Space 模式下检查 Canvas 底部边界
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

    /// <summary>
    /// 使用 BoxCast 检测与场景中 2D 碰撞器的碰撞（更准确，不会穿模）
    /// </summary>
    protected virtual void CheckSceneCollisionsBoxCast()
    {
        // 获取 Collider 在世界空间中的信息
        Vector2 worldPos = GetWorldPosition();
        Vector2 size = GetColliderSize();

        // 限制检测距离，避免误检测
        float velocityMag = velocity.magnitude;
        float stepDistance = velocityMag * Time.deltaTime / 3f; // 对应子步进
        float checkDistance = Mathf.Min(size.magnitude * 0.1f, stepDistance * 1.1f);
        checkDistance = Mathf.Max(checkDistance, 0.01f); // 最小检测距离

        // 即使物体静止，也检测碰撞（用于与玩家等场景物体的碰撞）
        Vector2 moveDirection = velocity.normalized;
        if (velocityMag < 0.01f)
        {
            moveDirection = Vector2.down; // 静止时向下检测，用于检测地面
            checkDistance = 0.1f; // 静止时使用固定检测距离
        }

        // 执行 BoxCast 检测 - 使用 Collider 的实际大小
        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            worldPos,           // 起始位置（Collider 中心）
            size * 0.95f,       // 盒子大小（接近 Collider 实际大小）
            rotation,           // 旋转角度
            moveDirection,      // 运动方向
            checkDistance,      // 检测距离
            collisionLayers     // 碰撞层
        );

        foreach (var hit in hits)
        {
            // 忽略自己
            if (hit.collider == null || hit.collider.gameObject == gameObject) continue;
            
            // 忽略触发器
            if (hit.collider.isTrigger) continue;

            // 计算相对速度在碰撞法线方向的分量
            float velocityAlongNormal = Vector2.Dot(velocity, hit.normal);

            // 只处理朝向碰撞面的情况（速度方向与法线相反，即 dot < 0）
            // 或者物体静止时（用于检测与玩家的碰撞）
            if (velocityAlongNormal < 0 || velocityMag < 0.01f)
            {
                // 物理反弹
                HandleCollisionPhysics(hit);

                // 触发碰撞事件
                OnCollisionEnter2D?.Invoke(this, null);
                break; // 一帧只处理一次主要碰撞
            }
        }
    }

    /// <summary>
    /// 处理碰撞物理（反弹 + 旋转）
    /// </summary>
    protected virtual void HandleCollisionPhysics(RaycastHit2D hit)
    {
        // 1. 位置修正 (Depenetration) - 防止物体嵌入导致抽搐
        // 只有在嵌入较深时才强制推开，否则微调
        float penetration = 0.05f - hit.distance;
        if (penetration > 0)
        {
            // 使用更平滑的推开力度
            Vector2 pushBack = hit.normal * (penetration * 0.5f);
            
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            {
                rectTransform.position += (Vector3)pushBack;
            }
            else
            {
                rectTransform.anchoredPosition += ScreenToAnchoredOffset(pushBack);
            }
        }

        // 2. 速度反弹
        float velocityAlongNormal = Vector2.Dot(velocity, hit.normal);
        if (velocityAlongNormal < 0)
        {
            // 稳定性处理：如果碰撞速度极小，不再反弹，直接抹平分量
            if (Mathf.Abs(velocityAlongNormal) < 0.2f)
            {
                velocity -= hit.normal * velocityAlongNormal;
            }
            else
            {
                Vector2 reflection = Vector2.Reflect(velocity, hit.normal);
                velocity = reflection * bounciness;
                
                // 应用摩擦力
                velocity *= (1f - friction * 0.2f);
            }
        }

        // 3. 添加旋转
        Vector2 tangent = new Vector2(-hit.normal.y, hit.normal.x);
        float tangentVelocity = Vector2.Dot(velocity, tangent);
        angularVelocity += tangentVelocity * 2f / mass; 
        angularVelocity = Mathf.Clamp(angularVelocity, -360f, 360f);
    }

    /// <summary>
    /// 获取 UI 元素在世界空间中的位置（Collider 中心）
    /// </summary>
    protected virtual Vector2 GetWorldPosition()
    {
        // 如果有 BoxCollider2D，使用它的中心
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            return collider.bounds.center;
        }
        
        // 否则使用 transform.position
        return transform.position;
    }
    
    /// <summary>
    /// 获取 Collider 的大小
    /// </summary>
    protected virtual Vector2 GetColliderSize()
    {
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            return collider.bounds.size;
        }
        
        // 没有 Collider 时使用 RectTransform 大小
        return rectTransform.rect.size * rectTransform.lossyScale;
    }

    /// <summary>
    /// 将世界空间偏移转换为 anchoredPosition 偏移
    /// </summary>
    protected virtual Vector2 ScreenToAnchoredOffset(Vector2 worldOffset)
    {
        // Canvas 的缩放因子（Screen Space - Overlay 模式下需要）
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Screen Space Overlay 模式下，世界偏移需要除以 Canvas 缩放
            return worldOffset / canvas.scaleFactor;
        }
        
        // 其他模式下直接使用
        return worldOffset;
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
        angularVelocity = 0f;
    }

    /// <summary>
    /// 拖拽中更新位置
    /// </summary>
    public virtual void Drag(Vector2 position)
    {
        if (!isBeingDragged) return;
        
        // World Space 模式下直接设置 position
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            rectTransform.position = position;
            lastPosition = position;
        }
        else
        {
            rectTransform.anchoredPosition = position;
            lastPosition = position;
        }
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

    #region 固定模式

    /// <summary>
    /// 切换固定/可移动模式
    /// </summary>
    public virtual void ToggleFixed()
    {
        isFixed = !isFixed;
        
        if (isFixed)
        {
            // 固定：停止所有运动
            velocity = Vector2.zero;
            angularVelocity = 0f;
            
            // 改变颜色表示固定状态
            SetColor(fixedColor);
            
            Debug.Log($"[{name}] 已固定为地面/墙壁");
        }
        else
        {
            // 恢复可移动
            SetColor(originalColor);
            
            Debug.Log($"[{name}] 已恢复可移动");
        }
    }
    
    /// <summary>
    /// 设置是否固定
    /// </summary>
    public virtual void SetFixed(bool isFixedValue)
    {
        if (isFixed != isFixedValue)
        {
            ToggleFixed();
        }
    }
    
    /// <summary>
    /// 获取是否固定
    /// </summary>
    public bool IsFixed => isFixed;
    
    /// <summary>
    /// 设置颜色
    /// </summary>
    protected virtual void SetColor(Color color)
    {
        foreach (var img in images)
        {
            if (img != null)
            {
                img.color = color;
            }
        }
    }

    #endregion

    #region 公共属性

    public bool IsBeingDragged => isBeingDragged;
    public bool IsGroundedElement => IsGrounded();
    public RectTransform RectTransform => rectTransform;
    public bool IsPlatform => isPlatform;

    #endregion

    #region 编辑器可视化

    protected virtual void OnDrawGizmosSelected()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        Gizmos.color = isBeingDragged ? Color.yellow : (IsGrounded() ? Color.green : Color.blue);

        // 绘制旋转的边界框
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(rectTransform.position, Quaternion.Euler(0, 0, rotation), Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, rectTransform.rect.size);
        Gizmos.matrix = oldMatrix;
    }

    #endregion

    #region IPhysicsObject Implementation

    Vector2 IPhysicsObject.Velocity => velocity;
    float IPhysicsObject.Mass => mass;
    bool IPhysicsObject.IsKinematic { get => isKinematic; set => isKinematic = value; }

    void IPhysicsObject.ApplyForce(Vector2 force)
    {
        AddForce(force);
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
}
