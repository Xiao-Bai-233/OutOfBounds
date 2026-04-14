using UnityEngine;

namespace OutOfBounds.UI
{
    /// <summary>
    /// 任务窗口管理器
    /// 检测Tab键按下并控制任务窗口的显示/隐藏
    /// </summary>
    public class TaskWindowManager : MonoBehaviour
    {
        [Header("窗口设置")]
        [SerializeField] public GameObject taskWindow;

        #region Unity 生命周期

        private void Awake()
        {
            Debug.Log("TaskWindowManager Awake: 初始化任务窗口管理器");
            if (taskWindow != null)
            {
                // 初始时隐藏窗口
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
                // 确保在游戏开始时强制隐藏，并同步状态
                taskWindow.SetActive(false);
            }
        }

        private void Update()
        {
            // 处理窗口显示/隐藏
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (taskWindow != null)
                {
                    bool nextState = !taskWindow.activeSelf;
                    taskWindow.SetActive(nextState);
                    Debug.Log($"TaskWindowManager: Tab键按下，窗口状态切换为: {nextState}");
                }
            }
        }

        #endregion
    }
}