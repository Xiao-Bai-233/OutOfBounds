using UnityEngine;
using UnityEngine.UI;

namespace OutOfBounds.UI
{
    /// <summary>
    /// 任务目标窗口
    /// 按TAB键唤起/隐藏
    /// </summary>
    public class TaskWindow : MonoBehaviour
    {
        [Header("窗口设置")]
        [SerializeField] public Text taskText;

        [Header("任务目标")]
        [SerializeField] private string[] tasks = new string[]
        {
            "收集2颗心",
            "使用心作为垫脚石跨越毒水坑",
            "到达终点"
        };

        #region Unity 生命周期

        private void Awake()
        {
            Debug.Log("TaskWindow Awake: 初始化任务窗口");
            // 初始时隐藏窗口
            gameObject.SetActive(false);
            
            // 如果不需要显示任务文字，这里可以跳过引用检查
            if (taskText != null)
            {
                UpdateTaskText();
            }
        }

        private void OnEnable()
        {
            Debug.Log("TaskWindow OnEnable: 窗口被启用");
            
            // 启用拖拽功能
            var draggable = GetComponent<OutOfBounds.DragSystem.DraggableUI>();
            if (draggable != null)
            {
                draggable.SetDraggable(true);
            }
        }

        private void OnDisable()
        {
            Debug.Log("TaskWindow OnDisable: 窗口被禁用");
            
            // 禁用拖拽功能
            var draggable = GetComponent<OutOfBounds.DragSystem.DraggableUI>();
            if (draggable != null)
            {
                draggable.SetDraggable(false);
            }
            
            // 停止物理运动
            var physicsElement = GetComponent<OutOfBounds.UI.UIPhysicsElement>();
            if (physicsElement != null)
            {
                physicsElement.SetVelocity(Vector2.zero);
            }
        }

        private void Update()
        {
            // 窗口显示/隐藏由 TaskWindowManager 处理
        }

        #endregion

        #region 任务管理

        /// <summary>
        /// 关闭窗口（供按钮调用）
        /// </summary>
        public void CloseWindow()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 更新任务文本
        /// </summary>
        private void UpdateTaskText()
        {
            if (taskText != null && tasks.Length > 0)
            {
                string taskString = "任务目标:\n";
                for (int i = 0; i < tasks.Length; i++)
                {
                    taskString += $"{i + 1}. {tasks[i]}\n";
                }
                taskText.text = taskString;
            }
        }

        #endregion
    }
}