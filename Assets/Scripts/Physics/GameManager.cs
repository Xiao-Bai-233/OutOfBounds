using UnityEngine;
using OutOfBounds.Core;

namespace OutOfBounds.Physics
{
/// <summary>
/// 游戏主管理器 - 管理游戏状态和全局配置
///
/// 重构说明：
/// - 新增从 LevelTransitionData 读取关卡索引（由 LevelSelectionManager 注入）
/// - 关卡完成后自动调用 SaveSystem.CompleteLevel() 持久化解锁进度
/// - 支持"同一场景，多关卡配置"的架构
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("游戏状态")]
    [SerializeField] private bool isPaused;
    [SerializeField] private bool isGameOver;

    [Header("关卡配置")]
    [SerializeField] private int currentLevel = 0;
    [SerializeField] private int totalLevels = 2; // 教程关 + 第一关

    [Header("关卡名称映射（编辑器中配置）")]
    [Tooltip("关卡显示名称，索引对应 currentLevel")]
    [SerializeField] private string[] levelDisplayNames = new string[]
    {
        "教程关 - 异常苏醒",
        "第一关 - UI丛林"
    };

    // 事件
    public System.Action<bool> OnPauseChanged;
    public System.Action OnGameOverEvent;
    public System.Action OnLevelComplete;
    public System.Action<int> OnLevelChanged;

    #region Unity 生命周期

    private void Awake()
    {
        // 单例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // ★ 从 LevelTransitionData 读取关卡索引
        int incomingLevelId = LevelTransitionData.NextLevelId;
        if (incomingLevelId >= 0)
        {
            currentLevel = Mathf.Clamp(incomingLevelId, 0, Mathf.Max(0, totalLevels - 1));
            Debug.Log($"[GameManager] 从 LevelTransitionData 接收关卡索引: {currentLevel}");

            // 重置过渡数据
            LevelTransitionData.Reset();
        }

        // 触发关卡改变事件
        OnLevelChanged?.Invoke(currentLevel);

        // 确保全局物理设置存在
        EnsurePhysicsSettings();
    }

    private void Start()
    {
        // 初始化
        ResumeGame();

        // 显示当前关卡信息
        string displayName = GetLevelDisplayName();
        Debug.Log($"[GameManager] 当前关卡: {displayName} (索引: {currentLevel})");
    }

    private void Update()
    {
        // 处理暂停输入（ESC键）
        HandlePauseInput();

        // 处理全局重置（R键）— 仅在游戏场景中有效
        HandleResetInput();
    }

    /// <summary>
    /// 全局 R 键重置：气泡复位 + 玩家回检查点 + 血回满
    /// </summary>
    private void HandleResetInput()
    {
        if (!Input.GetKeyDown(KeyCode.R)) return;
        if (isPaused || isGameOver) return;

        Debug.Log("[GameManager] R键重置：复位气泡 + 传送玩家 + 恢复血量");

        // 1. 复位所有可拖拽 UI
        var draggables = FindObjectsByType<OutOfBounds.DragSystem.DraggableUI>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var drag in draggables)
        {
            drag.ResetPosition();
        }

        // 2. 传送玩家到检查点
        var player = FindFirstObjectByType<OutOfBounds.Player.PlayerController>();
        if (player != null)
        {
            player.RespawnAtCheckpoint();
        }
        else
        {
            Debug.LogWarning("[GameManager] 未找到玩家，无法传送");
        }

        // 3. 恢复血量
        if (OutOfBounds.UI.HealthBarManager.Instance != null)
        {
            OutOfBounds.UI.HealthBarManager.Instance.ResetHealth();
        }
    }

    #endregion

    #region 暂停系统

    private void HandlePauseInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isGameOver) return;

        isPaused = !isPaused;

        if (isPaused)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        OnPauseChanged?.Invoke(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        OnPauseChanged?.Invoke(false);
    }

    #endregion

    #region 游戏状态

    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        Time.timeScale = 0f;
        OnGameOverEvent?.Invoke();
    }

    public void RestartLevel()
    {
        isGameOver = false;
        Time.timeScale = 1f;

        // 通过 SceneLoader 淡入淡出重新加载当前关卡
        SceneLoader.Instance.ReloadCurrentScene(currentLevel);
    }

    /// <summary>
    /// 完成当前关卡
    /// 自动保存进度到 SaveSystem，解锁下一关
    /// 然后加载下一关（如果还有）或返回主菜单
    /// </summary>
    public void CompleteLevel()
    {
        OnLevelComplete?.Invoke();

        // ★ 通过 SaveSystem 持久化关卡进度
        string displayName = GetLevelDisplayName();
        Debug.Log($"[GameManager] 关卡完成！保存进度: {displayName} (索引: {currentLevel})");
        SaveSystem.CompleteLevel(currentLevel);

        // 触发关卡完成全局事件
        Events.OnLevelCompleted.Invoke(currentLevel);

        // 标记"回到选关"——无论还有没有下一关，都让玩家自己选
        LevelTransitionData.MarkReturnToSelection();

        // 检查是否所有关卡都已通关
        if (currentLevel >= totalLevels - 1)
        {
            Debug.Log("[GameManager] 🎉 恭喜通关全部关卡！");
            Events.OnGameOver.Invoke();
        }

        // 回到选择关卡界面（带淡入淡出）
        ReturnToSelectionScreen();
    }

    /// <summary>
    /// 回到选关界面
    /// </summary>
    public void ReturnToSelectionScreen()
    {
        Debug.Log("[GameManager] 回到选择关卡界面");

        // 清理全局事件
        Events.ClearAll();

        // 直接调用 SceneLoader.LoadScene，不经过 LoadMainMenu（避免重置标记）
        // LoadMainMenu 内部会 Reset() 清掉 ShouldOpenLevelSelection
        SceneLoader.Instance.LoadScene(0);
    }

    /// <summary>
    /// 返回主菜单场景
    /// </summary>
    public void ReturnToMainMenu()
    {
        Debug.Log("[GameManager] 返回主菜单");
        LevelTransitionData.Reset();

        // 清理全局事件
        Events.ClearAll();

        // 通过 SceneLoader 淡入淡出返回主菜单
        SceneLoader.Instance.LoadMainMenu();
    }

    public void LoadLevel(int levelIndex)
    {
        currentLevel = Mathf.Clamp(levelIndex, 0, totalLevels - 1);
        OnLevelChanged?.Invoke(currentLevel);

        // 通过 SceneLoader 淡入淡出加载下一关
        SceneLoader.Instance.LoadGameLevel(currentLevel);
    }

    #endregion

    #region 初始化

    private void EnsurePhysicsSettings()
    {
        if (FindObjectOfType<GlobalPhysicsSettings>() == null)
        {
            var go = new GameObject("GlobalPhysicsSettings");
            go.AddComponent<GlobalPhysicsSettings>();
            DontDestroyOnLoad(go);
        }
    }

    #endregion

    #region 公共属性与方法

    public bool IsPaused => isPaused;
    public bool IsGameOver => isGameOver;
    public int CurrentLevel => currentLevel;
    public int TotalLevels => totalLevels;

    /// <summary>
    /// 获取当前关卡的中文显示名称
    /// </summary>
    public string GetLevelDisplayName()
    {
        if (levelDisplayNames != null &&
            currentLevel >= 0 &&
            currentLevel < levelDisplayNames.Length)
        {
            return levelDisplayNames[currentLevel];
        }
        return $"关卡 {currentLevel + 1}";
    }

    #endregion
}
}
