using UnityEngine;
using OutOfBounds.Puzzle;

namespace OutOfBounds.Camera
{
    /// <summary>
    /// Celeste风格的摄像机跟随系统
    /// 特性：
    /// - 死区（Deadzone）：玩家在死区内移动时摄像机不跟随
    /// - 平滑跟随：摄像机以一定速度平滑移动到目标位置
    /// - 可调节参数：通过Inspector调整所有数值
    /// - 检查点系统：支持场景中放置检查点触发器
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        [Header("目标")]
        [Tooltip("跟随的目标物体")]
        public Transform target;

        [Header("初始位置设置")]
        [Tooltip("玩家初始时在屏幕上的Y位置（0=屏幕底部，1=屏幕顶部），设为0.3表示玩家在屏幕下方30%处")]
        [Range(0f, 1f)]
        public float initialPlayerScreenY = 0.3f;
        [Tooltip("游戏开始时是否锁定初始位置（锁定后摄像机不会立即跟随玩家）")]
        public bool lockInitialPosition = true;

        [Header("死区设置（玩家在此范围内移动，摄像机不跟随）")]
        [Tooltip("死区宽度的一半")]
        public float deadzoneWidth = 2f;
        [Tooltip("死区高度的一半")]
        public float deadzoneHeight = 1f;

        [Header("跟随设置")]
        [Tooltip("摄像机跟随速度（值越大跟随越快，0表示即时跟随）")]
        [Range(0f, 20f)]
        public float followSpeed = 8f;
        [Tooltip("是否只在目标移动时跟随")]
        public bool followOnlyWhenMoving = true;

        [Header("垂直偏移")]
        [Tooltip("目标上方固定偏移量")]
        public float verticalOffset = 1f;
        [Tooltip("是否平滑应用垂直偏移")]
        public bool smoothVerticalOffset = true;
        [Tooltip("垂直偏移平滑速度")]
        [Range(1f, 10f)]
        public float verticalSmoothSpeed = 3f;

        [Header("水平边界（玩家位置限制）")]
        [Tooltip("是否限制玩家只能在屏幕左半边")]
        public bool limitPlayerToLeftSide = true;
        [Tooltip("玩家能到达的最右侧X位置（世界坐标），设为0表示屏幕中线")]
        public float maxPlayerX = 0f;
        [Tooltip("是否平滑阻止玩家越界（true=平滑推回，false=立即阻止）")]
        public bool smoothBoundaryPush = true;

        [Header("边界限制")]
        [Tooltip("是否限制摄像机边界")]
        public bool limitBounds = false;
        [Tooltip("世界边界左下角")]
        public Vector2 boundsMin;
        [Tooltip("世界边界右上角")]
        public Vector2 boundsMax;

        [Header("Debug")]
        [Tooltip("在Scene视图中绘制死区")]
        public bool drawDeadzoneGizmo = true;

        [Header("检查点设置")]
        [Tooltip("检查点触发半径")]
        public float checkpointTriggerRadius = 1f;
        [Tooltip("是否在Scene视图中绘制检查点")]
        public bool drawCheckpoints = true;

        // 内部状态
        private Vector3 currentVelocity;
        private float targetVerticalOffset;
        private float currentVerticalOffset;
        private Vector3 lastTargetPosition;
        private bool isInitialPositionLocked;
        private int currentCheckpointIndex = -1;

        // 检查点触发器引用（用于解锁/锁定）
        private CheckpointTrigger lastTriggeredCheckpoint;

        // 检查点触发后的跟随冷却（防止立即被拉回）
        private float checkpointFollowCooldown = 0f;
        [Tooltip("检查点触发后多久恢复跟随（秒）")]
        public float checkpointFollowCooldownTime = 0.5f;

        // 检查点触发事件
        public System.Action<CheckpointTrigger, int> OnCheckpointReached;

        #region Unity 生命周期

        private void Awake()
        {
            // 单例模式
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // 如果锁定初始位置，摄像机保持当前Transform的位置
            // 玩家需要在场景中放置在屏幕下方相应位置
            isInitialPositionLocked = lockInitialPosition;

            if (target != null)
            {
                lastTargetPosition = target.position;
                currentVerticalOffset = verticalOffset;
                targetVerticalOffset = verticalOffset;

                if (!lockInitialPosition)
                {
                    // 非锁定模式：立即跟随到玩家位置
                    Vector3 startPos = transform.position;
                    startPos.x = target.position.x;
                    startPos.y = target.position.y + verticalOffset;
                    transform.position = startPos;
                }
                else
                {
                    // 锁定模式：计算玩家到当前摄像机位置的初始偏移
                    // 玩家应该在屏幕下方，所以需要记录这个偏移
                    lastTargetPosition = target.position;
                }
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // 检查点冷却中：不跟随，让相机停在原地
            if (checkpointFollowCooldown > 0)
            {
                checkpointFollowCooldown -= Time.deltaTime;
                return;
            }

            // 检测目标是否在死区外（考虑垂直偏移）
            Vector3 cameraFocusPoint = transform.position - new Vector3(0, currentVerticalOffset, 0);
            Vector2 delta = (Vector2)(target.position - cameraFocusPoint);

            bool shouldMoveX = Mathf.Abs(delta.x) > deadzoneWidth;
            bool shouldMoveY = Mathf.Abs(delta.y) > deadzoneHeight;

            // 如果开启了"只在移动时跟随"，检测目标是否有移动
            bool targetIsMoving = (target.position - lastTargetPosition).sqrMagnitude > 0.0001f;
            lastTargetPosition = target.position;

            if (followOnlyWhenMoving && !targetIsMoving)
            {
                shouldMoveX = false;
                shouldMoveY = false;
            }

            // 计算目标位置
            float targetX = transform.position.x;
            float targetY = transform.position.y;

            if (shouldMoveX)
            {
                // 目标在死区右侧，摄像机向右移动
                if (delta.x > deadzoneWidth)
                {
                    targetX = target.position.x - deadzoneWidth;
                }
                // 目标在死区左侧，摄像机向左移动
                else if (delta.x < -deadzoneWidth)
                {
                    targetX = target.position.x + deadzoneWidth;
                }
            }

            if (shouldMoveY)
            {
                // 目标在死区上方
                if (delta.y > deadzoneHeight)
                {
                    targetY = target.position.y - deadzoneHeight + currentVerticalOffset;
                }
                // 目标在死区下方（只在非锁定模式时跟随向下）
                else if (delta.y < -deadzoneHeight && !isInitialPositionLocked)
                {
                    targetY = target.position.y + deadzoneHeight + currentVerticalOffset;
                }
            }

            // 锁定模式：摄像机不能低于初始位置
            if (isInitialPositionLocked)
            {
                float initialY = transform.position.y; // 记录初始Y位置
                targetY = Mathf.Max(targetY, initialY);
            }

            // 水平边界：限制玩家只能在屏幕左半边
            if (limitPlayerToLeftSide)
            {
                // 摄像机目标X不能超过 maxPlayerX
                // 这样玩家视角就永远被限制在左侧
                targetX = Mathf.Min(targetX, maxPlayerX);
            }

            // 平滑移动到目标位置
            Vector3 targetPosition = new Vector3(targetX, targetY, transform.position.z);

            if (followSpeed > 0)
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    targetPosition,
                    ref currentVelocity,
                    1f / followSpeed
                );
            }
            else
            {
                transform.position = targetPosition;
            }

            // 更新垂直偏移
            if (smoothVerticalOffset)
            {
                currentVerticalOffset = Mathf.Lerp(currentVerticalOffset, targetVerticalOffset, Time.deltaTime * verticalSmoothSpeed);
            }
            else
            {
                currentVerticalOffset = targetVerticalOffset;
            }

            // 应用边界限制
            if (limitBounds)
            {
                Vector3 limitedPos = transform.position;
                limitedPos.x = Mathf.Clamp(limitedPos.x, boundsMin.x, boundsMax.x);
                limitedPos.y = Mathf.Clamp(limitedPos.y, boundsMin.y, boundsMax.y);
                transform.position = limitedPos;
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 立即将摄像机跳转到的目标位置
        /// </summary>
        public void SnapToTarget()
        {
            if (target == null) return;

            Vector3 pos = transform.position;
            pos.x = target.position.x;
            pos.y = target.position.y + verticalOffset;
            transform.position = pos;
            currentVelocity = Vector3.zero;
        }

        /// <summary>
        /// 设置目标物体
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                lastTargetPosition = target.position;
            }
        }

        /// <summary>
        /// 临时设置死区大小
        /// </summary>
        public void SetDeadzone(float width, float height)
        {
            deadzoneWidth = width;
            deadzoneHeight = height;
        }

        /// <summary>
        /// 临时设置垂直偏移
        /// </summary>
        public void SetVerticalOffset(float offset)
        {
            targetVerticalOffset = offset;
        }

        /// <summary>
        /// 重置垂直偏移到默认值
        /// </summary>
        public void ResetVerticalOffset()
        {
            targetVerticalOffset = verticalOffset;
        }

        /// <summary>
        /// 解锁初始位置，允许摄像机自由跟随（包括向下）
        /// </summary>
        public void UnlockInitialPosition()
        {
            isInitialPositionLocked = false;
        }

        /// <summary>
        /// 锁定摄像机在当前位置（禁止向下跟随）
        /// </summary>
        public void LockToCurrentPosition()
        {
            isInitialPositionLocked = true;
        }

        /// <summary>
        /// 直接设置摄像机位置（用于检查点重置）
        /// </summary>
        public void SetCameraPosition(Vector3 position)
        {
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            currentVelocity = Vector3.zero;
        }

        /// <summary>
        /// 重置到第一个检查点
        /// </summary>
        public void ResetToFirstCheckpoint()
        {
            currentCheckpointIndex = -1;
            if (target != null)
            {
                SnapToTarget();
            }
        }

        /// <summary>
        /// 获取当前检查点索引
        /// </summary>
        public int GetCurrentCheckpointIndex()
        {
            return currentCheckpointIndex;
        }

        /// <summary>
        /// 设置水平边界
        /// </summary>
        public void SetMaxPlayerX(float maxX)
        {
            maxPlayerX = maxX;
        }

        #endregion

        #region 检查点触发器集成

        /// <summary>
        /// 被 CheckpointTrigger 调用：重置相机到指定位置
        /// </summary>
        public void OnCheckpointTriggered(CheckpointTrigger trigger, int checkpointIndex)
        {
            if (trigger == null) return;

            lastTriggeredCheckpoint = trigger;
            currentCheckpointIndex = checkpointIndex;

            // 重置相机位置
            Vector3 newPos;
            if (trigger.useCustomCameraPosition)
            {
                newPos = new Vector3(
                    trigger.customCameraPositionX,
                    trigger.customCameraPositionY,
                    trigger.customCameraPositionZ
                );
            }
            else
            {
                newPos = new Vector3(
                    trigger.transform.position.x,
                    trigger.transform.position.y,
                    transform.position.z
                );
            }
            transform.position = newPos;
            currentVelocity = Vector3.zero;

            // 关键：更新最后位置，让相机认为已经跟随到最新位置
            // 否则下一帧会立即把相机拉回玩家位置
            if (target != null)
            {
                lastTargetPosition = target.position;
            }

            // 处理解锁/锁定
            if (trigger.unlockOnReach)
            {
                isInitialPositionLocked = false;
            }

            if (trigger.lockInitialPositionOnReach)
            {
                isInitialPositionLocked = true;
            }

            // 处理垂直偏移重置
            if (trigger.resetVerticalOffset)
            {
                currentVerticalOffset = trigger.newVerticalOffset;
                targetVerticalOffset = trigger.newVerticalOffset;
            }

            // 处理水平边界重置
            if (trigger.resetHorizontalLimit)
            {
                maxPlayerX = trigger.newMaxPlayerX;
            }

            // 触发事件
            OnCheckpointReached?.Invoke(trigger, checkpointIndex);

            Debug.Log($"[CameraController] 检查点触发: {trigger.checkpointName}, 位置: {newPos}");

            // 开始冷却，相机暂时不跟随
            checkpointFollowCooldown = checkpointFollowCooldownTime;
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (!drawDeadzoneGizmo || target == null) return;

            // 绘制死区
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);

            Vector3 center = transform.position;
            center.y += currentVerticalOffset;

            Vector3 size = new Vector3(deadzoneWidth * 2, deadzoneHeight * 2, 0.1f);
            Gizmos.DrawWireCube(center, size);
            Gizmos.DrawCube(center, size);

            // 绘制目标位置
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(target.position, 0.2f);
        }

        #endregion
    }
}