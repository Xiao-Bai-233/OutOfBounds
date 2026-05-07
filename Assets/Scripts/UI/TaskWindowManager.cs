using UnityEngine;
using OutOfBounds.Puzzle;

namespace OutOfBounds.UI
{
    /// <summary>
    /// 任务窗口管理器
    /// 检测Tab键按下并控制任务窗口的显示/隐藏
    /// Phase 2: 添加信号区绑定 — 窗口只能在信号区内有效
    /// </summary>
    public class TaskWindowManager : MonoBehaviour
    {
        [Header("══ 窗口 ══")]
        [Tooltip("要管理的任务窗口 GameObject")]
        [SerializeField] public GameObject taskWindow;

        [Header("══ 信号区（防逃课） ══")]
        [Tooltip("Quest Log 的有效信号区域 — 玩家必须在这个区域内才能按 Tab 召唤窗口")]
        [SerializeField] private Transform signalRegion;

        [Tooltip("信号区有效范围半径")]
        [SerializeField] private float signalRange = 20f;

        [Tooltip("玩家 Transform — 用于检测是否在信号区内")]
        [SerializeField] private Transform playerTransform;

        #region Unity 生命周期

        private void Awake()
        {
            Debug.Log("TaskWindowManager Awake: 初始化任务窗口管理器");
            if (taskWindow != null)
            {
                taskWindow.SetActive(false);
                Debug.Log("TaskWindowManager Awake: 窗口初始状态: " + taskWindow.activeSelf);
            }
            else
            {
                Debug.LogError("TaskWindowManager Awake: taskWindow is null");
            }
        }

        private void Start()
        {
            if (taskWindow != null)
            {
                taskWindow.SetActive(false);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (taskWindow != null)
                {
                    // Phase 2: 检查玩家是否在信号区内
                    if (!IsPlayerInSignalRegion())
                    {
                        Debug.Log("[TaskWindowManager] 不在信号区内，无法召唤 Quest Log");
                        return;
                    }

                    bool nextState = !taskWindow.activeSelf;
                    taskWindow.SetActive(nextState);
                    Debug.Log($"TaskWindowManager: Tab键按下，窗口状态切换为: {nextState}");
                }
            }
        }

        #endregion

        #region 信号区检测

        /// <summary>
        /// 检查玩家是否在信号区内
        /// </summary>
        private bool IsPlayerInSignalRegion()
        {
            if (signalRegion == null || playerTransform == null) return true;

            float distance = Vector3.Distance(playerTransform.position, signalRegion.position);
            return distance <= signalRange;
        }

        /// <summary>
        /// 获取当前信号强度 (0~1)
        /// </summary>
        public float GetSignalStrength()
        {
            if (signalRegion == null || playerTransform == null) return 1f;

            float distance = Vector3.Distance(playerTransform.position, signalRegion.position);
            return Mathf.Clamp01(1f - (distance / signalRange));
        }

        #endregion
    }
}
