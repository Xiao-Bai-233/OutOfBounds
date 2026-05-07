using UnityEngine;
using System.Collections.Generic;
using OutOfBounds.Core;

namespace OutOfBounds.UI
{
    /// <summary>
    /// 关卡选择管理器
    /// 管理 Grid 网格布局的关卡选择面板
    ///
    /// 功能：
    /// - 根据 LevelDefinition 列表动态生成关卡按钮
    /// - 从 SaveSystem 读取解锁/完成状态
    /// - 点击解锁关卡 → 设置 LevelIndex → 加载游戏场景
    /// - 支持"继续游戏"和"新游戏"入口
    /// </summary>
    public class LevelSelectionManager : MonoBehaviour
    {
        [Header("关卡配置")]
        [Tooltip("所有关卡定义（在编辑器中按顺序配置）")]
        [SerializeField] private LevelDefinition[] levelDefinitions;

        [Header("UI 引用")]
        [Tooltip("关卡按钮的预制体")]
        [SerializeField] private GameObject levelButtonPrefab;

        [Tooltip("网格内容的 RectTransform（GridLayoutGroup 挂载在此）")]
        [SerializeField] private RectTransform gridContent;

        [Tooltip("关卡选择面板根对象")]
        [SerializeField] private GameObject selectionPanel;

        [Tooltip("返回主菜单按钮")]
        [SerializeField] private GameObject backButton;

        [Tooltip("页面标题文本")]
        [SerializeField] private UnityEngine.UI.Text panelTitleText;

        [Header("场景配置")]
        [Tooltip("游戏场景名称")]
        [SerializeField] private string gameSceneName = "TestScene";

        [Header("默认值")]
        [Tooltip("面板标题")]
        [SerializeField] private string panelTitle = "选择关卡";

        // 运行时生成的按钮列表
        private List<LevelButton> levelButtons = new List<LevelButton>();

        #region Unity 生命周期

        private void Awake()
        {
            // 预加载存档（确保存档被缓存）
            SaveSystem.GetSaveData();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 打开关卡选择面板
        /// </summary>
        public void OpenLevelSelection()
        {
            if (selectionPanel != null)
                selectionPanel.SetActive(true);

            if (panelTitleText != null)
                panelTitleText.text = panelTitle;

            // 重新生成关卡列表（刷新状态）
            RefreshLevelGrid();
        }

        /// <summary>
        /// 关闭关卡选择面板
        /// </summary>
        public void CloseLevelSelection()
        {
            if (selectionPanel != null)
                selectionPanel.SetActive(false);
        }

        /// <summary>
        /// 开始新游戏（从第一关开始）
        /// </summary>
        public void StartNewGame()
        {
            // 如果是新存档，第一关当然解锁
            // 但不清除已有存档（防止误操作）
            // 直接加载第一关
            EnterLevel(0);
        }

        /// <summary>
        /// 继续游戏（从最后完成的下一关开始）
        /// </summary>
        public void ContinueGame()
        {
            var saveData = SaveSystem.GetSaveData();

            // 找最后一个完成的关卡
            int lastCompleted = -1;
            if (saveData.completedLevels.Count > 0)
            {
                saveData.completedLevels.Sort();
                lastCompleted = saveData.completedLevels[^1];
            }

            // 加载下一关
            int targetLevel = lastCompleted + 1;

            // 如果所有关卡都完成了，则重新从第一关玩
            if (targetLevel >= levelDefinitions.Length)
            {
                targetLevel = 0;
            }

            EnterLevel(targetLevel);
        }

        #endregion

        #region 网格刷新

        /// <summary>
        /// 刷新整个关卡网格
        /// </summary>
        private void RefreshLevelGrid()
        {
            // 清空旧的按钮
            ClearGrid();

            if (levelDefinitions == null || levelDefinitions.Length == 0)
            {
                Debug.LogWarning("[LevelSelection] 未配置关卡定义！");
                return;
            }

            var saveData = SaveSystem.GetSaveData();

            // 按 gridRow/gridColumn 排序
            var sortedLevels = new List<LevelDefinition>(levelDefinitions);
            sortedLevels.Sort((a, b) =>
            {
                int rowCompare = a.gridRow.CompareTo(b.gridRow);
                if (rowCompare != 0) return rowCompare;
                return a.gridColumn.CompareTo(b.gridColumn);
            });

            // 逐个生成按钮
            foreach (var levelDef in sortedLevels)
            {
                if (levelDef == null) continue;

                bool unlocked = saveData.IsLevelUnlocked(levelDef.levelIndex);
                bool completed = saveData.IsLevelCompleted(levelDef.levelIndex);

                CreateLevelButton(levelDef, unlocked, completed);
            }

            Debug.Log($"[LevelSelection] 生成了 {levelButtons.Count} 个关卡按钮");
        }

        private void CreateLevelButton(LevelDefinition definition, bool unlocked, bool completed)
        {
            if (levelButtonPrefab == null || gridContent == null)
            {
                Debug.LogError("[LevelSelection] 缺少预制体或网格容器引用！");
                return;
            }

            // 实例化预制体
            GameObject buttonObj = Instantiate(levelButtonPrefab, gridContent);
            buttonObj.name = $"LevelBtn_{definition.levelIndex}_{definition.levelName}";

            // 获取 LevelButton 组件
            LevelButton levelBtn = buttonObj.GetComponent<LevelButton>();
            if (levelBtn == null)
            {
                Debug.LogError($"[LevelSelection] 预制体缺少 LevelButton 组件！");
                Destroy(buttonObj);
                return;
            }

            // 初始化
            levelBtn.Initialize(definition, OnLevelButtonClicked, unlocked, completed);
            levelButtons.Add(levelBtn);
        }

        private void ClearGrid()
        {
            foreach (var btn in levelButtons)
            {
                if (btn != null && btn.gameObject != null)
                    Destroy(btn.gameObject);
            }
            levelButtons.Clear();
        }

        #endregion

        #region 关卡进入

        private void OnLevelButtonClicked(LevelDefinition definition)
        {
            if (definition == null) return;

            Debug.Log($"[LevelSelection] 选择了关卡: {definition.levelName} (索引: {definition.levelIndex})");

            // 触发事件
            Events.OnLevelSelected.Invoke(definition.levelIndex);

            // 进入关卡
            EnterLevel(definition.sceneLevelId);
        }

        /// <summary>
        /// 进入指定关卡
        /// 通过 SceneLoader 淡入淡出过渡加载场景
        /// </summary>
        private void EnterLevel(int sceneLevelId)
        {
            Debug.Log($"[LevelSelection] 加载场景: {gameSceneName}, " +
                      $"关卡ID: {sceneLevelId}");

            // 触发关卡开始事件
            Events.OnLevelStarted.Invoke(sceneLevelId);

            // 通过 SceneLoader 淡入淡出加载
            SceneLoader.Instance.LoadGameLevel(sceneLevelId, gameSceneName);
        }

        #endregion

        #region 编辑器工具

        /// <summary>
        /// 重置所有存档（调试用）
        /// </summary>
        [ContextMenu("重置所有存档")]
        public void DebugResetAllData()
        {
            SaveSystem.ResetAllData();
            Debug.Log("[LevelSelection] 存档已重置");
        }

        /// <summary>
        /// 解锁所有关卡（调试用）
        /// </summary>
        [ContextMenu("解锁所有关卡")]
        public void DebugUnlockAllLevels()
        {
            var data = SaveSystem.GetSaveData();
            data.highestUnlockedLevel = levelDefinitions.Length - 1;
            SaveSystem.Flush();
            Debug.Log($"[LevelSelection] 已解锁所有关卡 (最高: {data.highestUnlockedLevel})");
        }

        #endregion
    }
}
