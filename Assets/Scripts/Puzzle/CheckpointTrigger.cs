using UnityEngine;
using OutOfBounds.Camera;

namespace OutOfBounds.Puzzle
{
    /// <summary>
    /// 检查点触发器组件
    /// 挂载到场景中的检查点物体上，玩家进入触发器时通知相机控制器重置
    /// 与 CameraController 集成，形成统一的相机控制系统
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CheckpointTrigger : MonoBehaviour
    {
        [Header("检查点配置")]
        [Tooltip("检查点名称（用于调试显示）")]
        public string checkpointName = "Checkpoint";

        [Header("相机位置设置")]
        [Tooltip("触发后相机移动到此位置（留空则使用检查点物体位置）")]
        public bool useCustomCameraPosition = false;
        [Tooltip("自定义相机位置 X")]
        public float customCameraPositionX = 0f;
        [Tooltip("自定义相机位置 Y")]
        public float customCameraPositionY = 0f;
        [Tooltip("自定义相机位置 Z")]
        public float customCameraPositionZ = -10f;

        [Header("触发后行为")]
        [Tooltip("触发后是否解锁初始位置锁定")]
        public bool unlockOnReach = false;

        [Tooltip("触发后是否重置垂直偏移")]
        public bool resetVerticalOffset = false;
        [Tooltip("新的垂直偏移值")]
        public float newVerticalOffset = 1f;

        [Tooltip("触发后是否重置水平边界")]
        public bool resetHorizontalLimit = false;
        [Tooltip("新的水平边界X值")]
        public float newMaxPlayerX = 0f;

        [Tooltip("触发后是否锁定初始位置")]
        public bool lockInitialPositionOnReach = false;

        [Header("触发设置")]
        [Tooltip("是否只能触发一次")]
        public bool triggerOnlyOnce = true;
        [Tooltip("触发冷却时间（秒）")]
        public float cooldownTime = 0.5f;

        [Header("视觉效果")]
        [Tooltip("触发时播放的音效")]
        public AudioClip triggerSound;
        [Tooltip("是否在触发后隐藏物体")]
        public bool hideOnTrigger = false;
        [Tooltip("触发后隐藏延迟（秒）")]
        public float hideDelay = 0.5f;

        [Header("Gizmos")]
        [Tooltip("在Scene视图中绘制触发区域")]
        public bool drawGizmo = true;
        [Tooltip("Gizmo颜色")]
        public Color gizmoColor = new Color(0f, 1f, 0f, 0.3f);

        // 状态
        private bool hasTriggered = false;
        private float lastTriggerTime = -999f;
        private Collider2D col;

        // 事件
        public System.Action<CheckpointTrigger> OnCheckpointTriggered;

        #region Unity 生命周期

        private void Awake()
        {
            col = GetComponent<Collider2D>();

            // 确保Collider是触发器
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log($"[CheckpointTrigger] {checkpointName}: 碰触了 {other.name}, Tag: {other.tag}");

            // 检查是否是玩家（可以通过Tag或Layer判断）
            if (!other.CompareTag("Player"))
            {
                Debug.Log($"[CheckpointTrigger] {checkpointName}: 不是Player标签，是 {other.tag}，跳过");
                return;
            }

            TryTrigger();
        }

        #endregion

        #region 触发逻辑

        public void TryTrigger()
        {
            // 检查是否在冷却中
            if (Time.time - lastTriggerTime < cooldownTime)
            {
                Debug.Log($"[CheckpointTrigger] {checkpointName}: 冷却中，跳过");
                return;
            }

            // 检查是否只能触发一次
            if (triggerOnlyOnce && hasTriggered)
            {
                Debug.Log($"[CheckpointTrigger] {checkpointName}: 已触发过且只能触发一次，跳过");
                return;
            }

            // 执行触发
            ExecuteTrigger();
        }

        private void ExecuteTrigger()
        {
            hasTriggered = true;
            lastTriggerTime = Time.time;

            Debug.Log($"[CheckpointTrigger] 触发检查点: {checkpointName}, 位置: {transform.position}");

            // 播放音效
            if (triggerSound != null)
            {
                AudioSource.PlayClipAtPoint(triggerSound, transform.position);
            }

            // 通知相机控制器（统一的系统）
            if (CameraController.Instance != null)
            {
                CameraController.Instance.OnCheckpointTriggered(this, GetCheckpointIndex());
            }
            else
            {
                Debug.LogWarning($"[CheckpointTrigger] {checkpointName}: 未找到 CameraController!");
            }

            // 触发事件
            OnCheckpointTriggered?.Invoke(this);

            // 隐藏物体（可选）
            if (hideOnTrigger)
            {
                Invoke(nameof(HideObject), hideDelay);
            }
        }

        /// <summary>
        /// 获取检查点索引（基于场景中所有 CheckpointTrigger 的排序）
        /// </summary>
        private int GetCheckpointIndex()
        {
            // 查找场景中所有 CheckpointTrigger
            CheckpointTrigger[] allCheckpoints = FindObjectsOfType<CheckpointTrigger>();
            System.Array.Sort(allCheckpoints, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

            for (int i = 0; i < allCheckpoints.Length; i++)
            {
                if (allCheckpoints[i] == this)
                {
                    return i;
                }
            }
            return 0;
        }

        private void HideObject()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 重置检查点状态（用于重新开始游戏等）
        /// </summary>
        public void ResetCheckpoint()
        {
            hasTriggered = false;
            lastTriggerTime = -999f;

            if (hideOnTrigger)
            {
                gameObject.SetActive(true);
            }
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            if (!drawGizmo) return;

            // 绘制触发区域
            Gizmos.color = hasTriggered
                ? new Color(0f, 0.5f, 0f, 0.3f)  // 已触发：深绿色
                : gizmoColor;  // 未触发：配置的颜色

            if (col != null)
            {
                // 绘制碰撞器范围
                Gizmos.DrawCube(
                    transform.position + (Vector3)col.offset,
                    col.bounds.size
                );
            }
            else
            {
                // 没有Collider时，绘制默认大小的方块
                Gizmos.DrawCube(transform.position, new Vector3(1f, 1f, 0.1f));
            }

            // 绘制触发器图标
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }

        #endregion
    }
}