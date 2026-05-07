using UnityEngine;
using System.Collections;
using OutOfBounds.Core;
using OutOfBounds.UI;
using OutOfBounds.DragSystem;

namespace OutOfBounds.Puzzle
{
    /// <summary>
    /// UI 上下文限制组件
    /// 挂载到任意 DraggableUI GameObject 上，定义该 UI 的有效使用范围。
    /// 当 UI 被拖离有效范围时，触发约束违规行为（乱码/透明/折叠/冻结/回收）。
    ///
    /// 三种约束类型:
    ///   AreaTrigger  — 检测 UI 是否离开指定的 RectTransform 矩形区域
    ///   SignalSource — 检测 UI 与信号源的距离是否超过 maxRange
    ///   ParentAttachment — 检测 UI 与父级 Transform 的距离，超过则断开并回收
    /// </summary>
    public class UIContextConstraint : MonoBehaviour
    {
        [Header("══ 约束方式 ══")]
        [Tooltip("用什么方法限制此 UI 的有效范围")]
        [SerializeField] private ContextConstraintType constraintType = ContextConstraintType.AreaTrigger;

        [Header("══ 区域检测 （AreaTrigger 模式） ══")]
        [Tooltip("有效区域的方框 — UI 中心离开这个方框就视为违规")]
        [SerializeField] private RectTransform validArea;

        [Tooltip("方框额外内边距 — 实际有效范围 = 方框 + 边距")]
        [SerializeField] private Vector2 areaPadding = new Vector2(20f, 20f);

        [Header("══ 信号源检测 （SignalSource / ParentAttachment 模式） ══")]
        [Tooltip("信号源物体 — UI 离它太远就会违规")]
        [SerializeField] private Transform signalSource;

        [Tooltip("最大有效距离 — 超过就触发违规")]
        [SerializeField] private float maxRange = 10f;

        [Tooltip("信号衰减起点 — 0=超出立刻违规，1=到最远才违规")]
        [Range(0f, 1f)]
        [SerializeField] private float signalFadeStart = 0.5f;

        [Header("══ 违规后行为 ══")]
        [Tooltip("离开有效范围后 UI 变成什么样子")]
        [SerializeField] private ConstraintViolationBehavior violationBehavior = ConstraintViolationBehavior.BecomeBroken;

        [Tooltip("回到有效区域后多久恢复正常（秒）")]
        [Range(0f, 5f)]
        [SerializeField] private float restoreDelay = 0.3f;

        [Tooltip("只在松手时检测 — 拖拽中不触发（推荐开启）")]
        [SerializeField] private bool checkOnReleaseOnly = true;

        [Tooltip("启用此约束")]
        [SerializeField] private bool enabled = true;

        // 状态
        private bool isViolated;
        private bool isRestoring;
        private float violationTime;
        private float restoreStartTime;

        // 缓存的组件引用
        private DraggableUI draggableUI;
        private UIPhysicsElement physicsElement;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private UnityEngine.UI.Image[] images;
        private Color[] originalColors;
        private Vector3 originalScale;
        private BoxCollider2D boxCollider;

        // 事件
        public System.Action<UIContextConstraint> OnConstraintViolated;
        public System.Action<UIContextConstraint> OnConstraintRestored;

        // 属性
        public bool IsViolated => isViolated;
        public bool IsEnabled { get => enabled; set => enabled = value; }
        public ContextConstraintType ConstraintType => constraintType;
        public ConstraintViolationBehavior ViolationBehavior => violationBehavior;

        /// <summary>
        /// 运行时设置信号源（解决预制体无法绑定场景物体的问题）
        /// 调用后自动切换到 SignalSource 或 ParentAttachment 模式
        /// </summary>
        public void SetupSignalSource(Transform source, float range, ContextConstraintType type = ContextConstraintType.SignalSource, float fadeStart = 0.5f)
        {
            signalSource = source;
            maxRange = range;
            constraintType = type;
            signalFadeStart = fadeStart;
            enabled = true;
        }

        /// <summary>
        /// 运行时设置区域检测（解决预制体问题）
        /// </summary>
        public void SetupAreaTrigger(RectTransform area, float paddingX = 20f, float paddingY = 20f)
        {
            validArea = area;
            areaPadding = new Vector2(paddingX, paddingY);
            constraintType = ContextConstraintType.AreaTrigger;
            enabled = true;
        }

        #region Unity 生命周期

        private void Awake()
        {
            CacheComponents();
            StoreOriginalState();
        }

        private void Start()
        {
            // 订阅 DraggableUI 的松手事件
            draggableUI = GetComponent<DraggableUI>();
            if (draggableUI != null)
            {
                draggableUI.OnDragEnd += OnDragEnded;
            }
        }

        private void Update()
        {
            if (!enabled) return;

            // 如果已经违规，检查是否可以恢复
            if (isViolated)
            {
                if (!IsOutOfBounds() && Time.time - restoreStartTime > restoreDelay)
                {
                    RestoreConstraint();
                }
                return;
            }

            // 如果只在松手后检测，更新阶段不做检查
            if (checkOnReleaseOnly) return;

            // 拖拽中持续检测
            if (IsOutOfBounds())
            {
                TriggerViolation();
            }
        }

        private void OnDestroy()
        {
            if (draggableUI != null)
            {
                draggableUI.OnDragEnd -= OnDragEnded;
            }
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 强制触发违规（外部调用，如 StageConfig 检测到违规条件）
        /// </summary>
        public void ForceViolation()
        {
            if (!enabled || isViolated) return;
            TriggerViolation();
        }

        /// <summary>
        /// 强制恢复（外部调用，如玩家回到有效区域）
        /// </summary>
        public void ForceRestore()
        {
            if (!isViolated) return;
            RestoreConstraint();
        }

        /// <summary>
        /// 松手时由 DraggableUI 回调
        /// </summary>
        public void OnDragEnded(DraggableUI draggable)
        {
            if (!enabled || !checkOnReleaseOnly) return;

            // 松手后检测是否超出有效范围
            if (IsOutOfBounds())
            {
                TriggerViolation();
            }
        }

        #endregion

        #region 检测逻辑

        /// <summary>
        /// 检测当前是否超出有效范围
        /// </summary>
        private bool IsOutOfBounds()
        {
            if (rectTransform == null) return false;

            switch (constraintType)
            {
                case ContextConstraintType.None:
                    return false;

                case ContextConstraintType.AreaTrigger:
                    return CheckAreaTrigger();

                case ContextConstraintType.SignalSource:
                    return CheckSignalSource();

                case ContextConstraintType.ParentAttachment:
                    return CheckParentAttachment();

                default:
                    return false;
            }
        }

        private bool CheckAreaTrigger()
        {
            if (validArea == null) return false;

            // 获取 UI 元素的世界位置
            Vector2 uiPos = rectTransform.position;

            // 获取有效区域的世界矩形
            Vector3[] corners = new Vector3[4];
            validArea.GetWorldCorners(corners);

            float minX = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x) - areaPadding.x;
            float maxX = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x) + areaPadding.x;
            float minY = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y) - areaPadding.y;
            float maxY = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y) + areaPadding.y;

            return uiPos.x < minX || uiPos.x > maxX || uiPos.y < minY || uiPos.y > maxY;
        }

        private bool CheckSignalSource()
        {
            if (signalSource == null) return false;

            float distance = Vector2.Distance(rectTransform.position, signalSource.position);
            return distance > maxRange;
        }

        private bool CheckParentAttachment()
        {
            if (signalSource == null) return false;

            float distance = Vector2.Distance(rectTransform.position, signalSource.position);

            // 信号衰减：在 signalFadeStart * maxRange 处开始生效
            if (signalFadeStart > 0)
            {
                float fadeRange = maxRange * (1f - signalFadeStart);
                float effectiveRange = maxRange - fadeRange;
                return distance > effectiveRange;
            }

            return distance > maxRange;
        }

        /// <summary>
        /// 获取当前信号强度 (0~1, 1=最强)
        /// </summary>
        public float GetSignalStrength()
        {
            if (signalSource == null || rectTransform == null) return 1f;

            float distance = Vector2.Distance(rectTransform.position, signalSource.position);
            if (distance >= maxRange) return 0f;

            if (signalFadeStart > 0)
            {
                float fadeStart = maxRange * signalFadeStart;
                if (distance <= fadeStart) return 1f;
                return 1f - Mathf.InverseLerp(fadeStart, maxRange, distance);
            }

            return 1f - (distance / maxRange);
        }

        #endregion

        #region 违规 / 恢复

        private void TriggerViolation()
        {
            if (isViolated) return;
            isViolated = true;
            violationTime = Time.time;

            Debug.Log($"[UIContextConstraint] {gameObject.name} 脱离上下文，触发行为: {violationBehavior}");

            switch (violationBehavior)
            {
                case ConstraintViolationBehavior.BecomeBroken:
                    StartCoroutine(BecomeBrokenRoutine());
                    break;
                case ConstraintViolationBehavior.BecomeTransparent:
                    StartCoroutine(BecomeTransparentRoutine());
                    break;
                case ConstraintViolationBehavior.Collapse:
                    StartCoroutine(CollapseRoutine());
                    break;
                case ConstraintViolationBehavior.Freeze:
                    FreezeElement();
                    break;
                case ConstraintViolationBehavior.Recycle:
                    StartCoroutine(RecycleRoutine());
                    break;
            }

            OnConstraintViolated?.Invoke(this);
        }

        private void RestoreConstraint()
        {
            if (!isViolated) return;
            isViolated = false;
            isRestoring = false;

            Debug.Log($"[UIContextConstraint] {gameObject.name} 约束恢复");

            // 恢复视觉
            RestoreVisuals();

            // 恢复物理
            if (physicsElement != null)
            {
                physicsElement.SetColliderEnabled(true);
                physicsElement.SetPhysicsLocked(false);
                physicsElement.SetKinematic(false);
            }

            OnConstraintRestored?.Invoke(this);
        }

        #endregion

        #region 违规行为协程

        private IEnumerator BecomeBrokenRoutine()
        {
            // 视觉效果：颜色变红 + 抖动
            if (images != null)
            {
                Color brokenColor = new Color(1f, 0.2f, 0.2f, 0.6f);
                foreach (var img in images)
                {
                    if (img != null) img.color = brokenColor;
                }
            }

            // 禁用碰撞（变成 Broken Glyph）
            if (boxCollider != null) boxCollider.enabled = false;
            if (physicsElement != null)
            {
                physicsElement.SetPhysicsLocked(true);
                physicsElement.SetColliderEnabled(false);
            }

            // 抖动效果
            float shakeDuration = 0.5f;
            float shakeIntensity = 2f;
            float elapsed = 0f;
            Vector3 shakeOrigin = rectTransform.localPosition;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float shake = Mathf.Sin(elapsed * 50f) * shakeIntensity * (1f - elapsed / shakeDuration);
                rectTransform.localPosition = shakeOrigin + new Vector3(shake, shake * 0.5f, 0);
                yield return null;
            }

            rectTransform.localPosition = shakeOrigin;

            // 持续检查是否可以恢复
            while (isViolated)
            {
                if (!IsOutOfBounds())
                {
                    restoreStartTime = Time.time;
                    if (restoreDelay <= 0)
                    {
                        RestoreConstraint();
                        yield break;
                    }
                }
                yield return new WaitForSeconds(0.5f);
            }
        }

        private IEnumerator BecomeTransparentRoutine()
        {
            // 渐隐效果
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            float duration = 0.5f;
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0.3f, elapsed / duration);
                yield return null;
            }

            // 禁用碰撞
            if (boxCollider != null) boxCollider.enabled = false;
            if (physicsElement != null)
            {
                physicsElement.SetColliderEnabled(false);
            }

            // 缓慢吸回有效区域中心
            if (validArea != null)
            {
                Vector3 targetPos = validArea.position;
                float returnDuration = 1f;
                elapsed = 0f;
                Vector3 startPos = rectTransform.position;

                while (elapsed < returnDuration && isViolated)
                {
                    elapsed += Time.deltaTime;
                    rectTransform.position = Vector3.Lerp(startPos, targetPos, elapsed / returnDuration);
                    yield return null;
                }
            }

            // 检查恢复
            while (isViolated)
            {
                if (!IsOutOfBounds())
                {
                    restoreStartTime = Time.time;
                    if (restoreDelay <= 0)
                    {
                        RestoreConstraint();
                        yield break;
                    }
                }
                yield return new WaitForSeconds(0.5f);
            }
        }

        private IEnumerator CollapseRoutine()
        {
            // 折叠效果：缩小到 0
            float duration = 0.3f;
            float elapsed = 0f;
            Vector3 startScale = rectTransform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                rectTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
                yield return null;
            }

            rectTransform.localScale = Vector3.zero;

            // 禁用碰撞
            if (boxCollider != null) boxCollider.enabled = false;
            if (physicsElement != null)
            {
                physicsElement.SetColliderEnabled(false);
                physicsElement.SetVelocity(Vector2.zero);
            }

            // 检查恢复
            while (isViolated)
            {
                if (!IsOutOfBounds())
                {
                    restoreStartTime = Time.time;
                    if (restoreDelay <= 0)
                    {
                        RestoreConstraint();
                        yield break;
                    }
                }
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void FreezeElement()
        {
            // 立即冻结：停止所有运动
            if (physicsElement != null)
            {
                physicsElement.SetVelocity(Vector2.zero);
                physicsElement.SetPhysicsLocked(true);
            }
        }

        private IEnumerator RecycleRoutine()
        {
            if (gameObject == null) yield break;

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            float duration = 0.4f;
            float elapsed = 0f;

            while (elapsed < duration && canvasGroup != null)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }

            // 回收：通知 UIPhysicsManager
            if (physicsElement != null && UIPhysicsManager.Instance != null)
            {
                UIPhysicsManager.Instance.RecycleHeartElement(physicsElement);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        #endregion

        #region 辅助方法

        private void CacheComponents()
        {
            draggableUI = GetComponent<DraggableUI>();
            physicsElement = GetComponent<UIPhysicsElement>();
            rectTransform = GetComponent<RectTransform>();
            boxCollider = GetComponent<BoxCollider2D>();
            images = GetComponentsInChildren<UnityEngine.UI.Image>();
        }

        private void StoreOriginalState()
        {
            if (images != null)
            {
                originalColors = new Color[images.Length];
                for (int i = 0; i < images.Length; i++)
                {
                    if (images[i] != null)
                        originalColors[i] = images[i].color;
                }
            }

            if (rectTransform != null)
            {
                originalScale = rectTransform.localScale;
            }
        }

        private void RestoreVisuals()
        {
            // 恢复颜色
            if (images != null && originalColors != null)
            {
                for (int i = 0; i < Mathf.Min(images.Length, originalColors.Length); i++)
                {
                    if (images[i] != null)
                        images[i].color = originalColors[i];
                }
            }

            // 恢复缩放
            if (rectTransform != null)
            {
                rectTransform.localScale = originalScale;
            }

            // 恢复透明度
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            // 恢复碰撞
            if (boxCollider != null) boxCollider.enabled = true;
        }

        #endregion

        #region 编辑器可视化

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!enabled) return;

            switch (constraintType)
            {
                case ContextConstraintType.AreaTrigger:
                    if (validArea != null)
                    {
                        Gizmos.color = isViolated ? Color.red : Color.green;
                        Vector3[] corners = new Vector3[4];
                        validArea.GetWorldCorners(corners);

                        // 绘制有效区域（含内边距）
                        Vector3 center = (corners[0] + corners[2]) / 2f;
                        Vector3 size = corners[2] - corners[0];
                        size.x += areaPadding.x * 2;
                        size.y += areaPadding.y * 2;
                        Gizmos.DrawWireCube(center, size);
                    }
                    break;

                case ContextConstraintType.SignalSource:
                case ContextConstraintType.ParentAttachment:
                    if (signalSource != null)
                    {
                        Gizmos.color = isViolated ? Color.red : Color.cyan;
                        Gizmos.DrawWireSphere(signalSource.position, maxRange);

                        if (signalFadeStart > 0)
                        {
                            Gizmos.color = Color.yellow;
                            Gizmos.DrawWireSphere(signalSource.position, maxRange * signalFadeStart);
                        }

                        if (rectTransform != null)
                        {
                            Gizmos.color = isViolated ? Color.red : Color.white;
                            Gizmos.DrawLine(signalSource.position, rectTransform.position);
                        }
                    }
                    break;
            }
        }
        #endif

        #endregion
    }
}
