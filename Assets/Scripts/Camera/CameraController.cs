using UnityEngine;
using OutOfBounds.Puzzle;

namespace OutOfBounds.Camera
{
    /// <summary>
    /// 优化后的摄像机跟随系统
    /// 特性：
    /// - 柔性跟随：基于 SmoothDamp 的全轴平滑移动
    /// - 动态前瞻 (Look-ahead)：根据玩家移动方向自动偏移视角
    /// - 垂直偏移控制：支持根据站立/空中状态调整视角中心
    /// - 检查点集成：支持平滑或瞬间传送至指定位置
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        [Header("目标引用")]
        public Transform target;
        private Rigidbody2D targetRb;

        [Header("跟随设置")]
        [Tooltip("跟随平滑时间，越小越紧贴目标")]
        public float smoothTime = 0.15f;
        [Tooltip("最大跟随速度")]
        public float maxSpeed = 50f;

        [Header("动态前瞻 (Look-ahead)")]
        [Tooltip("前瞻距离，根据速度方向偏移")]
        public float lookAheadDistance = 2f;
        [Tooltip("前瞻平滑速度")]
        public float lookAheadSmoothSpeed = 2f;
        [Tooltip("触发前瞻的最小速度阈值")]
        public float lookAheadThreshold = 0.1f;

        [Header("偏移设置")]
        [Tooltip("基础垂直偏移")]
        public float verticalOffset = 1.5f;
        [Tooltip("当玩家在空中时的额外垂直偏移（例如下落时看下方）")]
        public float airVerticalBias = -1f;

        [Header("边界与限制")]
        public bool limitBounds = false;
        public Vector2 boundsMin;
        public Vector2 boundsMax;
        [Tooltip("水平最右侧限制（用于限制玩家视角）")]
        public float maxPlayerX = 999f;
        public bool limitPlayerToLeftSide = false;

        // 内部状态
        private Vector3 currentVelocity;
        private Vector3 lookAheadOffset;
        private float currentVerticalBias;
        private bool isTeleporting;

        #region Unity 生命周期

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (target != null)
            {
                targetRb = target.GetComponent<Rigidbody2D>();
                // 初始位置对齐
                SnapToTarget();
            }
        }

        private void LateUpdate()
        {
            if (target == null || isTeleporting) return;

            // 1. 计算前瞻偏移
            UpdateLookAhead();

            // 2. 计算垂直偏差（处理跳跃/下落时的视角重心）
            UpdateVerticalBias();

            // 3. 计算目标位置
            Vector3 targetPos = target.position;
            targetPos.z = transform.position.z;
            targetPos += lookAheadOffset;
            targetPos.y += verticalOffset + currentVerticalBias;

            // 4. 水平边界限制
            if (limitPlayerToLeftSide)
            {
                targetPos.x = Mathf.Min(targetPos.x, maxPlayerX);
            }

            // 5. 应用平滑跟随
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPos,
                ref currentVelocity,
                smoothTime,
                maxSpeed
            );

            // 6. 最终边界限制
            if (limitBounds)
            {
                Vector3 pos = transform.position;
                pos.x = Mathf.Clamp(pos.x, boundsMin.x, boundsMax.x);
                pos.y = Mathf.Clamp(pos.y, boundsMin.y, boundsMax.y);
                transform.position = pos;
            }
        }

        #endregion

        #region 内部逻辑

        private void UpdateLookAhead()
        {
            float moveX = 0;
            if (targetRb != null)
            {
                moveX = targetRb.linearVelocity.x;
            }

            if (Mathf.Abs(moveX) > lookAheadThreshold)
            {
                float targetXOffset = Mathf.Sign(moveX) * lookAheadDistance;
                lookAheadOffset.x = Mathf.Lerp(lookAheadOffset.x, targetXOffset, Time.deltaTime * lookAheadSmoothSpeed);
            }
            else
            {
                lookAheadOffset.x = Mathf.Lerp(lookAheadOffset.x, 0, Time.deltaTime * lookAheadSmoothSpeed);
            }
        }

        private void UpdateVerticalBias()
        {
            // 如果玩家在下落，稍微向下偏移视角
            float moveY = targetRb != null ? targetRb.linearVelocity.y : 0;
            float targetBias = (moveY < -2f) ? airVerticalBias : 0;
            currentVerticalBias = Mathf.Lerp(currentVerticalBias, targetBias, Time.deltaTime * 2f);
        }

        #endregion

        #region 公共接口 (适配现有逻辑)

        /// <summary>
        /// 瞬间对齐目标
        /// </summary>
        public void SnapToTarget()
        {
            if (target == null) return;
            Vector3 pos = target.position;
            pos.z = transform.position.z;
            pos.y += verticalOffset;
            transform.position = pos;
            currentVelocity = Vector3.zero;
            lookAheadOffset = Vector3.zero;
        }

        /// <summary>
        /// 被 CheckpointTrigger 调用：重置相机位置
        /// </summary>
        public void OnCheckpointTriggered(CheckpointTrigger trigger, int checkpointIndex)
        {
            if (trigger == null) return;

            Vector3 newPos;
            if (trigger.useCustomCameraPosition)
            {
                newPos = new Vector3(trigger.customCameraPositionX, trigger.customCameraPositionY, transform.position.z);
            }
            else
            {
                newPos = new Vector3(trigger.transform.position.x, trigger.transform.position.y, transform.position.z);
            }

            // 执行瞬间传送或快速平滑移动
            transform.position = newPos;
            currentVelocity = Vector3.zero;
            lookAheadOffset = Vector3.zero;

            // 处理其他限制参数
            if (trigger.resetHorizontalLimit)
            {
                maxPlayerX = trigger.newMaxPlayerX;
            }
            
            if (trigger.resetVerticalOffset)
            {
                verticalOffset = trigger.newVerticalOffset;
            }

            Debug.Log($"[CameraController] 检查点传送至: {newPos}");
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null) targetRb = target.GetComponent<Rigidbody2D>();
        }

        public void SetMaxPlayerX(float maxX) => maxPlayerX = maxX;

        #endregion
    }
}
