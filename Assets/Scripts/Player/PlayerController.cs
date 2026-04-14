using UnityEngine;
using UnityEngine.InputSystem;
using OutOfBounds.Core;
using OutOfBounds.Physics;
using OutOfBounds.UI;

namespace OutOfBounds.Player
{
/// <summary>
/// 玩家角色控制器 - 处理移动和跳跃
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerController : MonoBehaviour, IPhysicsObject
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float deceleration = 50f;

    [Header("跳跃设置")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;  // 跳跃中断时的减速倍数
    [SerializeField] private float coyoteTime = 0.1f;        // 土狼时间（落地后还能跳的时间窗口）
    [SerializeField] private float jumpBufferTime = 0.1f;   // 跳跃预输入缓冲时间

    [Header("地面检测")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask uiPhysicsLayer; // 专门用于检测 UI 物理层的掩码
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.5f);
    [SerializeField] private PhysicsMaterial2D frictionMaterial; // 走路时的摩擦力材质
    [SerializeField] private PhysicsMaterial2D noFrictionMaterial; // 斜坡防止下滑的材质

    // 组件引用
    private Rigidbody2D rb;
    private Collider2D col;
    private Animator anim;
    private AudioSource audioSource;

    [Header("音频设置")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip teleportSound;

    [Header("动画参数名")]
    [SerializeField] private string animSpeed = "Speed";
    [SerializeField] private string animIsGrounded = "IsGrounded";
    [SerializeField] private string animVerticalVelocity = "VerticalVelocity";
    [SerializeField] private string animDieTrigger = "Die";
    [SerializeField] private string animHurtTrigger = "Hurt";

    // 状态
    private Vector2 moveInput;
    private Vector2 currentVelocity;
    private float lastGroundedTime;      // 上次在地面上的时间
    private float lastJumpPressedTime;   // 上次按下跳跃键的时间
    private bool isJumping;
    private bool isFacingRight = true;
    private bool wasGroundedState;       // 记录上一帧的地面状态
    private bool isCurrentlyGrounded;    // 当前帧的着地缓存

    // 输入系统
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;

    #region Unity 生命周期

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        anim = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // 设置刚体默认属性
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        SetupInputActions();
    }

    private void Start()
    {
        // 确保HealthBarManager存在并初始化
        if (HealthBarManager.Instance != null)
        {
            // 使用HealthBarManager组件中设置的maxHealth
            HealthBarManager.Instance.InitializeHealthBar(HealthBarManager.Instance.maxHealth);
        }
    }

    private void Update()
    {
        // 1. 更新并缓存着地状态
        isCurrentlyGrounded = CheckGrounded();
        
        // 强制修复：如果着地，必须结束跳跃状态
        if (isCurrentlyGrounded)
        {
            isJumping = false; 
            
            if (!wasGroundedState)
            {
                PlaySound(landSound);
                OnLand?.Invoke();
            }
            lastGroundedTime = Time.time;
        }
        wasGroundedState = isCurrentlyGrounded;

        // 2. 读取输入
        moveInput = moveAction.ReadValue<Vector2>();

        // 3. 处理跳跃逻辑缓存
        if (jumpAction.WasPressedThisFrame())
        {
            lastJumpPressedTime = Time.time;
        }

        if (jumpAction.WasReleasedThisFrame() && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }

        // 4. 视觉与动画
        HandleFlip();
        UpdateAnimations();
    }

    private void UpdateAnimations()
    {
        if (anim == null) return;

        // 1. 水平速度消抖
        float horizontalSpeed = Mathf.Abs(rb.linearVelocity.x);
        anim.SetFloat(animSpeed, horizontalSpeed > 0.1f ? horizontalSpeed : 0f);
        
        // 2. 着地状态同步
        anim.SetBool(animIsGrounded, isCurrentlyGrounded);

        // 3. 垂直速度消抖
        float verticalVel = rb.linearVelocity.y;
        
        // 核心修复：如果已经着地，强行将动画速度设为0，防止斜坡滑动导致动画错误
        if (isCurrentlyGrounded || Mathf.Abs(verticalVel) < 0.2f) 
        {
            verticalVel = 0f; 
        }
        anim.SetFloat(animVerticalVelocity, verticalVel);
    }

    private void FixedUpdate()
    {
        // 水平移动
        HandleMovement();

        // 跳跃
        HandleJump();

        // 更新当前速度
        currentVelocity = rb.linearVelocity;
    }

    #endregion

    #region 输入设置

    private void SetupInputActions()
    {
        // 创建 Input Actions（使用 Unity 新的 Input System）
        var map = new InputActionMap("Player");

        // 移动动作
        moveAction = map.AddAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");

        // 跳跃动作
        jumpAction = map.AddAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Keyboard>/w");
        jumpAction.AddBinding("<Keyboard>/upArrow");

        map.Enable();
    }

    #endregion

    #region 移动逻辑

    private void HandleMovement()
    {
        // 1. 计算目标速度
        float targetVelocityX = moveInput.x * moveSpeed;
        
        // 2. 根据是否有输入决定加速度或减速度
        float accelerationRate = Mathf.Abs(targetVelocityX) > 0.01f ? acceleration : deceleration;

        // 3. 计算新的水平速度
        float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetVelocityX,
            accelerationRate * Time.fixedDeltaTime);

        // 4. 应用速度
        if (isCurrentlyGrounded && Mathf.Abs(moveInput.x) < 0.01f)
        {
            // 彻底稳定 Y 轴，防止微小跳动
            rb.linearVelocity = new Vector2(0, 0);
        }
        else
        {
            rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);
        }
        
        // 5. 动态调整材质防止斜坡滑落
        if (col != null)
        {
            bool shouldUseFriction = isCurrentlyGrounded && Mathf.Abs(moveInput.x) < 0.01f;
            col.sharedMaterial = shouldUseFriction ? frictionMaterial : noFrictionMaterial;
        }
    }

    private void HandleFlip()
    {
        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            bool shouldFaceRight = moveInput.x > 0;
            if (shouldFaceRight != isFacingRight)
            {
                isFacingRight = shouldFaceRight;
                transform.localScale = new Vector3(
                    isFacingRight ? Mathf.Abs(transform.localScale.x) : -Mathf.Abs(transform.localScale.x),
                    transform.localScale.y,
                    transform.localScale.z
                );
            }
        }
    }

    #endregion

    #region 跳跃逻辑

    private void HandleJump()
    {
        // 检查是否可以跳跃
        bool canJump = CanJump();

        // 执行跳跃
        if (canJump)
        {
            ExecuteJump();
        }

        // 更新跳跃状态
        isJumping = rb.linearVelocity.y > 0 || (isJumping && !isCurrentlyGrounded);
    }

    private bool CanJump()
    {
        // 检查土狼时间
        bool hasCoyoteTime = Time.time - lastGroundedTime < coyoteTime;
        bool isGrounded = isCurrentlyGrounded || hasCoyoteTime;

        // 检查跳跃缓冲
        bool hasJumpBuffer = Time.time - lastJumpPressedTime < jumpBufferTime;

        return isGrounded && hasJumpBuffer && !isJumping;
    }

    private void ExecuteJump()
    {
        // 设置跳跃速度
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        isJumping = true;
        isCurrentlyGrounded = false; // 跳跃瞬间强制离地

        // 播放跳跃音效
        PlaySound(jumpSound);

        // 触发跳跃事件（可用于播放音效等）
        OnJump?.Invoke();
    }

    #endregion

    #region 地面检测

    private bool CheckGrounded()
    {
        if (col == null) return false;

        // 1. 标准地面检测 (BoxCast)
        Bounds bounds = col.bounds;
        Vector2 boxSize = new Vector2(bounds.size.x * 0.8f, 0.1f);
        Vector2 boxOrigin = new Vector2(bounds.center.x, bounds.min.y + 0.05f);
        float rayLength = groundCheckDistance + 0.1f;

        RaycastHit2D groundHit = Physics2D.BoxCast(boxOrigin, boxSize, 0f, Vector2.down, rayLength, groundLayer);
        if (groundHit.collider != null) return true;

        // 2. 物理 UI 平台检测 (优化：改用 OverlapCircle 增加斜坡容错性)
        Vector2 circlePos = new Vector2(bounds.center.x, bounds.min.y);
        float circleRadius = bounds.extents.x * 0.9f;
        
        // 增加检测范围适配陡峭斜坡
        Collider2D[] hits = Physics2D.OverlapCircleAll(circlePos, circleRadius + groundCheckDistance + 0.1f, uiPhysicsLayer);
        foreach (Collider2D hit in hits)
        {
            if (hit == col || hit.isTrigger) continue;

            UIPhysicsElement uiElement = hit.GetComponent<UIPhysicsElement>();
            if (uiElement != null && uiElement.IsPlatform)
            {
                // 只要位置在物体中心上方，且没有剧烈的上升速度，就视为稳固踩踏
                if (rb.linearVelocity.y < 0.5f && transform.position.y > hit.bounds.center.y - 0.2f)
                {
                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region 公共方法与事件

    // 事件
    public System.Action OnJump;
    public System.Action OnLand;
    public System.Action OnTakeDamage;
    public System.Action OnDeath;

    // 公共属性
    public bool IsGroundedState => isCurrentlyGrounded;
    public bool IsJumpingState => isJumping;
    public Vector2 Velocity => rb.linearVelocity;
    public float MoveSpeed => moveSpeed;

    // 强制设置速度（用于外部影响，如击退等）
    public void SetVelocity(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
    }

    // 添加冲击力
    public void AddForce(Vector2 force, ForceMode2D mode = ForceMode2D.Impulse)
    {
        rb.AddForce(force, mode);
    }

    // 受伤处理
    public void TakeDamage(int damage)
    {
        if (HealthBarManager.Instance != null)
        {
            HealthBarManager.Instance.TakeDamage(damage);
            
            // 播放受伤动画和音效
            if (anim != null) anim.SetTrigger(animHurtTrigger);
            PlaySound(hurtSound);

            OnTakeDamage?.Invoke();
        }
    }

    // 死亡处理
    public void Die()
    {
        if (anim != null) anim.SetTrigger(animDieTrigger);
        
        OnDeath?.Invoke();
        // 可以在这里添加死亡动画、游戏结束逻辑等
        Debug.Log("玩家死亡");
    }

    // 治疗
    public void Heal(int amount)
    {
        if (HealthBarManager.Instance != null)
        {
            HealthBarManager.Instance.Heal(amount);
        }
    }

    /// <summary>
    /// 传送到最后一次存档的检查点
    /// </summary>
    public void RespawnAtCheckpoint()
    {
        if (OutOfBounds.Puzzle.CheckpointManager.Instance != null && OutOfBounds.Puzzle.CheckpointManager.Instance.HasCheckpointSet)
        {
            Vector3 respawnPos = OutOfBounds.Puzzle.CheckpointManager.Instance.GetLastCheckpointPosition();
            
            // 停止当前运动
            rb.linearVelocity = Vector2.zero;
            
            // 执行传送
            transform.position = respawnPos;
            
            // 播放传送音效
            PlaySound(teleportSound);

            // 通知相机重置（如果需要）
            if (OutOfBounds.Camera.CameraController.Instance != null)
            {
                OutOfBounds.Camera.CameraController.Instance.SnapToTarget();
            }

            Debug.Log($"[PlayerController] 玩家已传送回检查点: {respawnPos}");
        }
        else
        {
            Debug.LogWarning("[PlayerController] 未找到存档点，无法传送！");
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    #endregion

    #region 编辑器可视化

    private void OnDrawGizmosSelected()
    {
        if (col == null) col = GetComponent<Collider2D>();
        if (col == null) return;

        // 绘制标准地面检测框 (BoxCast 区域)
        Gizmos.color = isCurrentlyGrounded ? Color.green : Color.red;
        Bounds bounds = col.bounds;
        Vector2 boxSize = new Vector2(bounds.size.x * 0.8f, 0.05f);
        Vector2 boxOrigin = new Vector2(bounds.center.x, bounds.min.y - groundCheckDistance);
        Gizmos.DrawWireCube(boxOrigin, boxSize);

        // 绘制平台检测半径 (OverlapCircle 区域)
        Vector2 circlePos = (Vector2)transform.position + groundCheckOffset;
        Gizmos.DrawWireSphere(circlePos, bounds.extents.x * 0.9f + groundCheckDistance);
    }

    #endregion

    #region IPhysicsObject Implementation

    Vector2 IPhysicsObject.Velocity => rb != null ? rb.linearVelocity : Vector2.zero;
    float IPhysicsObject.Mass => rb != null ? rb.mass : 1f;
    bool IPhysicsObject.IsKinematic { get => rb != null && rb.isKinematic; set { if (rb != null) rb.isKinematic = value; } }

    void IPhysicsObject.ApplyForce(Vector2 force)
    {
        AddForce(force);
    }

    #endregion
}
}
