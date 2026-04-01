using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家角色控制器 - 处理移动和跳跃
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerController : MonoBehaviour
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
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.5f);

    // 组件引用
    private Rigidbody2D rb;
    private Collider2D col;

    // 状态
    private Vector2 moveInput;
    private Vector2 currentVelocity;
    private float lastGroundedTime;      // 上次在地面上的时间
    private float lastJumpPressedTime;   // 上次按下跳跃键的时间
    private bool isJumping;
    private bool isFacingRight = true;

    // 输入系统
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;

    #region Unity 生命周期

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // 设置刚体默认属性
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        SetupInputActions();
    }

    private void Update()
    {
        // 记录地面状态时间
        if (IsGrounded())
        {
            lastGroundedTime = Time.time;
        }

        // 读取输入
        moveInput = moveAction.ReadValue<Vector2>();

        // 跳跃输入缓冲
        if (jumpAction.WasPressedThisFrame())
        {
            lastJumpPressedTime = Time.time;
        }

        // 跳跃中断检测（松开跳跃键）
        if (jumpAction.WasReleasedThisFrame() && rb.velocity.y > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpCutMultiplier);
        }

        // 翻转角色朝向
        HandleFlip();
    }

    private void FixedUpdate()
    {
        // 水平移动
        HandleMovement();

        // 跳跃
        HandleJump();

        // 更新当前速度
        currentVelocity = rb.velocity;
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
        float targetVelocityX = moveInput.x * moveSpeed;
        float velocityDifference = targetVelocityX - rb.velocity.x;
        float accelerationRate = Mathf.Abs(targetVelocityX) > 0.01f ? acceleration : deceleration;

        // 使用 SmoothDamp 平滑过渡
        float newVelocityX = Mathf.MoveTowards(rb.velocity.x, targetVelocityX,
            accelerationRate * Time.fixedDeltaTime);

        rb.velocity = new Vector2(newVelocityX, rb.velocity.y);
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
        isJumping = rb.velocity.y > 0 || (isJumping && !IsGrounded());
    }

    private bool CanJump()
    {
        // 检查土狼时间
        bool hasCoyoteTime = Time.time - lastGroundedTime < coyoteTime;
        bool isGrounded = IsGrounded() || hasCoyoteTime;

        // 检查跳跃缓冲
        bool hasJumpBuffer = Time.time - lastJumpPressedTime < jumpBufferTime;

        return isGrounded && hasJumpBuffer && !isJumping;
    }

    private void ExecuteJump()
    {
        // 设置跳跃速度
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        isJumping = true;

        // 触发跳跃事件（可用于播放音效等）
        OnJump?.Invoke();
    }

    #endregion

    #region 地面检测

    private bool IsGrounded()
    {
        // 使用圆形检测
        Vector2 checkPosition = (Vector2)transform.position + groundCheckOffset;
        float checkRadius = col.bounds.extents.x;

        Collider2D hit = Physics2D.OverlapCircle(checkPosition, checkRadius + groundCheckDistance, groundLayer);
        return hit != null;
    }

    #endregion

    #region 公共方法与事件

    // 事件
    public System.Action OnJump;
    public System.Action OnLand;

    // 公共属性
    public bool IsGroundedState => IsGrounded();
    public bool IsJumpingState => isJumping;
    public Vector2 Velocity => rb.velocity;
    public float MoveSpeed => moveSpeed;

    // 强制设置速度（用于外部影响，如击退等）
    public void SetVelocity(Vector2 velocity)
    {
        rb.velocity = velocity;
    }

    // 添加冲击力
    public void AddForce(Vector2 force, ForceMode2D mode = ForceMode2D.Impulse)
    {
        rb.AddForce(force, mode);
    }

    #endregion

    #region 编辑器可视化

    private void OnDrawGizmosSelected()
    {
        // 绘制地面检测点
        Gizmos.color = IsGrounded() ? Color.green : Color.red;
        Vector2 checkPosition = (Vector2)transform.position + groundCheckOffset;
        float checkRadius = GetComponent<Collider2D>() != null
            ? GetComponent<Collider2D>().bounds.extents.x
            : 0.5f;
        Gizmos.DrawWireSphere(checkPosition, checkRadius + groundCheckDistance);
    }

    #endregion
}
