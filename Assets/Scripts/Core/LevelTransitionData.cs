namespace OutOfBounds.Core
{
    /// <summary>
    /// 场景切换时的关卡传递数据
    /// 在 SceneManager.LoadScene 前设置，GameManager.Awake 时读取
    ///
    /// 使用静态变量而非 PlayerPrefs 的原因：
    /// - 避免磁盘 I/O 开销
    /// - 避免 PlayerPrefs 残留脏数据
    /// - 生命周期仅在内存中，切换场景后仍然存活
    /// </summary>
    public static class LevelTransitionData
    {
        /// <summary>
        /// 当前要加载的关卡 ID
        /// LevelSelectionManager 在 LoadScene 前设置此值
        /// GameManager 在 Awake/Start 时读取
        /// </summary>
        public static int NextLevelId { get; set; } = 0;

        /// <summary>
        /// 上一个关卡 ID（用于"重新开始"时保留上下文）
        /// </summary>
        public static int PreviousLevelId { get; set; } = -1;

        /// <summary>
        /// 通关后是否自动打开选关面板
        /// GameManager.CompleteLevel() 设为 true
        /// MainMenuManager.Start() 读取后清掉
        /// </summary>
        public static bool ShouldOpenLevelSelection { get; set; } = false;

        /// <summary>
        /// 重置过渡数据（场景加载完成后调用）
        /// </summary>
        public static void Reset()
        {
            PreviousLevelId = NextLevelId;
            NextLevelId = 0;
            ShouldOpenLevelSelection = false;
        }

        /// <summary>
        /// 标记为"通关后回到选关"
        /// </summary>
        public static void MarkReturnToSelection()
        {
            ShouldOpenLevelSelection = true;
        }
    }
}
