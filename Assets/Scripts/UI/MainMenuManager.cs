using UnityEngine;
using UnityEngine.UI;
using OutOfBounds.Core;

namespace OutOfBounds.UI
{
    /// <summary>
    /// 主菜单管理器
    /// 处理开始游戏、关卡选择、继续游戏、退出游戏逻辑
    ///
    /// 核心设计：按钮绑定全部由代码在 Start() 中自动完成
    /// 不再依赖 UnityEvent 序列化，彻底解决场景重载后绑定丢失的问题
    ///
    /// 按钮命名约定（Hierarchy 中必须按此命名）：
    ///   MainMenuPanel/
    ///   ├── Btn_SelectLevel    — 选关按钮
    ///   ├── Btn_Continue       — 继续游戏按钮
    ///   ├── Btn_NewGame        — 新游戏按钮
    ///   └── Btn_Quit           — 退出按钮
    ///   LevelSelectionPanel/
    ///   └── Btn_Back           — 返回按钮
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("关卡选择管理器（拖拽场景中的实例）")]
        [SerializeField] private LevelSelectionManager levelSelectionManager;

        [Header("UI 面板")]
        [Tooltip("主菜单面板根对象")]
        [SerializeField] private GameObject mainMenuPanel;

        [Tooltip("关卡选择面板（由 LevelSelectionManager 管理）")]
        [SerializeField] private GameObject levelSelectionPanel;

        [Header("按钮命名前缀（与 Hierarchy 一致）")]
        [SerializeField] private string btnPrefix = "Btn_";

        // 内部缓存的按钮
        private Button btnSelectLevel;
        private Button btnContinue;
        private Button btnNewGame;
        private Button btnQuit;
        private Button btnBack;

        #region Unity 生命周期

        private void Start()
        {
            // ★ 自动查找并绑定所有按钮（不依赖 UnityEvent 序列化）
            AutoBindButtons();

            // 确保主菜单显示
            ShowMainMenu();

            // 检查是否有存档，控制"继续游戏"按钮状态
            UpdateContinueButtonState();

            // 初始存档预加载
            SaveSystem.GetSaveData();

            // ★ 检测是否需要自动打开选关面板（从关卡完成返回）
            if (LevelTransitionData.ShouldOpenLevelSelection)
            {
                LevelTransitionData.ShouldOpenLevelSelection = false;
                Invoke(nameof(DelayedOpenSelection), 0.05f);
            }
        }

        /// <summary>
        /// 延迟打开选关面板（给 UI 初始化留一帧时间）
        /// </summary>
        private void DelayedOpenSelection()
        {
            OpenLevelSelection();
        }

        #endregion

        #region 自动按钮绑定

        /// <summary>
        /// 在场景中按名字查找按钮，自动绑定点击事件
        /// 无需在 Inspector 的 OnClick 中手动配置
        /// </summary>
        private void AutoBindButtons()
        {
            // 在 MainMenuPanel 下查找按钮
            if (mainMenuPanel != null)
            {
                btnSelectLevel = FindButton(mainMenuPanel.transform, btnPrefix + "SelectLevel");
                btnContinue    = FindButton(mainMenuPanel.transform, btnPrefix + "Continue");
                btnNewGame     = FindButton(mainMenuPanel.transform, btnPrefix + "NewGame");
                btnQuit        = FindButton(mainMenuPanel.transform, btnPrefix + "Quit");
            }

            // 在 LevelSelectionPanel 下查找返回按钮
            if (levelSelectionPanel != null)
            {
                btnBack = FindButton(levelSelectionPanel.transform, btnPrefix + "Back");
            }

            // 绑定点击事件（先清空旧的，防止重复绑定）
            BindButton(btnSelectLevel, OpenLevelSelection);
            BindButton(btnContinue, ContinueGame);
            BindButton(btnNewGame, NewGame);
            BindButton(btnQuit, QuitGame);
            BindButton(btnBack, BackToMainMenu);

            Debug.Log("[MainMenu] 按钮自动绑定完成");
        }

        /// <summary>
        /// 在父物体下递归查找指定名称的 Button 组件
        /// </summary>
        private static Button FindButton(Transform parent, string name)
        {
            // 先查直接子物体
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    var btn = child.GetComponent<Button>();
                    if (btn != null) return btn;
                }
            }

            // 没有直接找到，打印警告
            Debug.LogWarning($"[MainMenu] 未找到按钮: {parent.name}/{name}");
            return null;
        }

        /// <summary>
        /// 绑定按钮的 onClick 事件，先清空旧监听
        /// </summary>
        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        #endregion

        #region 主菜单按钮事件

        /// <summary>
        /// "选关" 按钮 — 打开关卡选择面板
        /// </summary>
        public void OpenLevelSelection()
        {
            Debug.Log("[MainMenu] 打开关卡选择面板");

            if (levelSelectionManager != null)
            {
                HideMainMenu();
                levelSelectionManager.OpenLevelSelection();
            }
            else
            {
                Debug.LogError("[MainMenu] LevelSelectionManager 未绑定！");
            }
        }

        /// <summary>
        /// "继续游戏" 按钮 — 加载最后进度的下一关
        /// </summary>
        public void ContinueGame()
        {
            Debug.Log("[MainMenu] 继续游戏");

            if (levelSelectionManager != null)
            {
                levelSelectionManager.ContinueGame();
            }
            else
            {
                Debug.LogError("[MainMenu] LevelSelectionManager 未绑定！");
            }
        }

        /// <summary>
        /// "新游戏" 按钮 — 从第一关开始
        /// </summary>
        public void NewGame()
        {
            Debug.Log("[MainMenu] 新游戏 - 从第一关开始");

            if (levelSelectionManager != null)
            {
                levelSelectionManager.StartNewGame();
            }
            else
            {
                Debug.LogError("[MainMenu] LevelSelectionManager 未绑定！");
            }
        }

        /// <summary>
        /// 从关卡选择面板返回主菜单
        /// </summary>
        public void BackToMainMenu()
        {
            Debug.Log("[MainMenu] 返回主菜单");

            if (levelSelectionManager != null)
            {
                levelSelectionManager.CloseLevelSelection();
            }

            ShowMainMenu();
        }

        /// <summary>
        /// 退出游戏
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[MainMenu] 正在退出游戏...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region UI 控制

        private void ShowMainMenu()
        {
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(true);

            if (levelSelectionPanel != null)
                levelSelectionPanel.SetActive(false);
        }

        private void HideMainMenu()
        {
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(false);
        }

        /// <summary>
        /// 根据存档是否存在，更新"继续游戏"按钮状态
        /// </summary>
        private void UpdateContinueButtonState()
        {
            if (btnContinue == null) return;

            bool hasSave = SaveSystem.SaveFileExists();
            if (hasSave)
            {
                var data = SaveSystem.GetSaveData();
                hasSave = data.completedLevels.Count > 0;
            }

            btnContinue.gameObject.SetActive(hasSave);
        }

        #endregion
    }
}
