using UnityEngine;

/// <summary>
/// 游戏主管理器 - 管理游戏状态和全局配置
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

    // 事件
    public System.Action<bool> OnPauseChanged;
    public System.Action OnGameOver;
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

        // 确保全局物理设置存在
        EnsurePhysicsSettings();
    }

    private void Start()
    {
        // 初始化
        ResumeGame();
    }

    private void Update()
    {
        // 处理暂停输入（ESC键）
        HandlePauseInput();
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
        OnGameOver?.Invoke();
    }

    public void RestartLevel()
    {
        isGameOver = false;
        Time.timeScale = 1f;
        // 触发场景重新加载
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    public void CompleteLevel()
    {
        OnLevelComplete?.Invoke();

        if (currentLevel < totalLevels - 1)
        {
            currentLevel++;
            LoadLevel(currentLevel);
        }
        else
        {
            // 通关！
            Debug.Log("游戏通关！");
        }
    }

    public void LoadLevel(int levelIndex)
    {
        currentLevel = Mathf.Clamp(levelIndex, 0, totalLevels - 1);
        OnLevelChanged?.Invoke(currentLevel);

        // 重新加载场景
        UnityEngine.SceneManagement.SceneManager.LoadScene(currentLevel);
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

    #region 公共属性

    public bool IsPaused => isPaused;
    public bool IsGameOver => isGameOver;
    public int CurrentLevel => currentLevel;
    public int TotalLevels => totalLevels;

    #endregion
}
