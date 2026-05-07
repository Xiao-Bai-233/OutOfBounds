using UnityEngine;
using System;
using System.Collections.Generic;

namespace OutOfBounds.Puzzle
{
    /// <summary>
    /// 关卡阶段类型 — 对应设计文档的 7 个 Stage
    /// </summary>
    public enum LevelStage
    {
        Stage0_Awakening = 0,     // 阶段零：异常苏醒（基础移动+UI高亮反馈）
        Stage1_UI_Drag = 1,       // 阶段一：UI拖拽入门（[SPACE] 文字拆解）
        Stage2_QuestWindow = 2,   // 阶段二：任务窗口桥（Quest Log 搭桥）
        Stage3_HP_Resource = 3,   // 阶段三：疼痛的代价（HP 心形资源化）
        Stage4_NPC_Dialog = 4,    // 阶段四：喋喋不休的NPC（对话气泡体积驱动）
        Stage5_DropdownMenu = 5,  // 阶段五：折叠菜单梯（下拉菜单展开结构）
        Stage6_ExitLock = 6       // 阶段六：出口三重锁（综合测试）
    }

    /// <summary>
    /// 关卡阶段事件参数
    /// </summary>
    [Serializable]
    public class StageEventArgs
    {
        public LevelStage previousStage;
        public LevelStage currentStage;
    }

    /// <summary>
    /// 关卡流程管理器
    /// 管理三阶段关卡流程和阶段切换
    /// </summary>
    public class LevelStageManager : MonoBehaviour
    {
        public static LevelStageManager Instance { get; private set; }

        [Header("当前阶段")]
        [SerializeField] private LevelStage currentStage = LevelStage.Stage0_Awakening;

        [Header("阶段配置")]
        [Tooltip("阶段零配置：异常苏醒")]
        [SerializeField] private Stage0Config stage0Config;

        [Tooltip("阶段一配置：UI拖拽入门")]
        [SerializeField] private Stage1Config stage1Config;

        [Tooltip("阶段二配置：任务窗口桥")]
        [SerializeField] private Stage2Config stage2Config;

        [Tooltip("阶段三配置：疼痛的代价")]
        [SerializeField] private Stage3Config stage3Config;

        [Tooltip("阶段四配置：喋喋不休的NPC")]
        [SerializeField] private Stage4Config stage4Config;

        [Tooltip("阶段五配置：折叠菜单梯（预留给 Phase 3）")]
        [SerializeField] private Stage5Config stage5Config;

        [Tooltip("阶段六配置：出口三重锁（预留给 Phase 3）")]
        [SerializeField] private Stage6Config stage6Config;

        [Header("完成条件检测")]
        [SerializeField] private float checkInterval = 0.5f;
        [SerializeField] private bool autoCheckCompletion = true;

        [Header("关卡完成集成")]
        [Tooltip("所有阶段完成后自动通知 GameManager.CompleteLevel()")]
        [SerializeField] private bool autoNotifyGameManager = true;

        // 事件
        public event Action<StageEventArgs> OnStageChanged;
        public event Action<LevelStage> OnStageCompleted;
        public event Action<LevelStage> OnStageStarted;

        /// <summary>
        /// 所有关卡阶段都已完成后触发
        /// LevelStageManager → GameManager 的集成点
        /// </summary>
        public event Action OnAllStagesCompleted;

        // 私有
        private StageConfig currentConfig;
        private float lastCheckTime;
        private bool isTransitioning;

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

            // 初始化配置映射
            InitializeConfigs();
        }

        private void Start()
        {
            // 触发初始阶段开始
            StartStage(currentStage);
        }

        private void Update()
        {
            // 自动检测完成条件
            if (autoCheckCompletion && !isTransitioning)
            {
                if (Time.time - lastCheckTime > checkInterval)
                {
                    lastCheckTime = Time.time;
                    CheckStageCompletion();
                }
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 开始指定阶段
        /// </summary>
        public void StartStage(LevelStage stage)
        {
            if (isTransitioning) return;

            LevelStage previousStage = currentStage;
            currentStage = stage;

            // 激活该阶段的配置
            ActivateStageConfig(stage);

            // 触发事件
            var args = new StageEventArgs { previousStage = previousStage, currentStage = stage };
            OnStageStarted?.Invoke(stage);
            OnStageChanged?.Invoke(args);

            Debug.Log($"[LevelStageManager] 开始阶段: {stage}");
        }

        /// <summary>
        /// 完成当前阶段，进入下一阶段
        /// </summary>
        public void CompleteCurrentStage()
        {
            if (isTransitioning) return;

            isTransitioning = true;
            OnStageCompleted?.Invoke(currentStage);

            // 获取下一个阶段
            LevelStage nextStage = GetNextStage();
            if (nextStage != currentStage) // 还有下一阶段
            {
                Debug.Log($"[LevelStageManager] 阶段 {currentStage} 完成！进入 {nextStage}");
                StartStage(nextStage);
            }
            else
            {
                // ★ 所有阶段已完成 → 触发关卡完成集成
                Debug.Log($"[LevelStageManager] 🎉 所有阶段已完成！关卡通关！");
                OnAllStagesCompleted?.Invoke();

                if (autoNotifyGameManager)
                {
                    var gm = UnityEngine.Object.FindObjectOfType<OutOfBounds.Physics.GameManager>();
                    if (gm != null)
                    {
                        gm.CompleteLevel();
                    }
                    else
                    {
                        Debug.LogWarning("[LevelStageManager] 未找到 GameManager，无法自动完成关卡");
                    }
                }
            }

            isTransitioning = false;
        }

        /// <summary>
        /// 手动触发阶段完成检测
        /// </summary>
        public void TriggerCompletionCheck()
        {
            CheckStageCompletion();
        }

        /// <summary>
        /// 获取当前阶段
        /// </summary>
        public LevelStage GetCurrentStage() => currentStage;

        /// <summary>
        /// 检查是否在指定阶段
        /// </summary>
        public bool IsInStage(LevelStage stage) => currentStage == stage;

        #endregion

        #region 私有方法

        private void InitializeConfigs()
        {
            // 初始化各阶段配置
            if (stage0Config != null) stage0Config.Initialize();
            if (stage1Config != null) stage1Config.Initialize();
            if (stage2Config != null) stage2Config.Initialize();
            if (stage3Config != null) stage3Config.Initialize();
            if (stage4Config != null) stage4Config.Initialize();
            if (stage5Config != null) stage5Config.Initialize();
            if (stage6Config != null) stage6Config.Initialize();
        }

        private void ActivateStageConfig(LevelStage stage)
        {
            // 停用所有阶段配置
            if (stage0Config != null) stage0Config.SetActive(false);
            if (stage1Config != null) stage1Config.SetActive(false);
            if (stage2Config != null) stage2Config.SetActive(false);
            if (stage3Config != null) stage3Config.SetActive(false);
            if (stage4Config != null) stage4Config.SetActive(false);
            if (stage5Config != null) stage5Config.SetActive(false);
            if (stage6Config != null) stage6Config.SetActive(false);

            // 激活当前阶段配置
            switch (stage)
            {
                case LevelStage.Stage0_Awakening:
                    stage0Config?.SetActive(true);
                    currentConfig = stage0Config;
                    break;
                case LevelStage.Stage1_UI_Drag:
                    stage1Config?.SetActive(true);
                    currentConfig = stage1Config;
                    break;
                case LevelStage.Stage2_QuestWindow:
                    stage2Config?.SetActive(true);
                    currentConfig = stage2Config;
                    break;
                case LevelStage.Stage3_HP_Resource:
                    stage3Config?.SetActive(true);
                    currentConfig = stage3Config;
                    break;
                case LevelStage.Stage4_NPC_Dialog:
                    stage4Config?.SetActive(true);
                    currentConfig = stage4Config;
                    break;
                case LevelStage.Stage5_DropdownMenu:
                    stage5Config?.SetActive(true);
                    currentConfig = stage5Config;
                    break;
                case LevelStage.Stage6_ExitLock:
                    stage6Config?.SetActive(true);
                    currentConfig = stage6Config;
                    break;
            }
        }

        private void CheckStageCompletion()
        {
            if (currentConfig != null && currentConfig.IsCompleted())
            {
                CompleteCurrentStage();
            }
        }

        private LevelStage GetNextStage()
        {
            int currentIndex = (int)currentStage;
            int nextIndex = currentIndex + 1;

            // 如果有配置，使用配置的下一个阶段
            if (currentConfig != null && currentConfig.nextStage != currentStage)
            {
                return currentConfig.nextStage;
            }

            // 否则按顺序（不再硬编码上限为 Stage3_XXX）
            if (nextIndex <= (int)LevelStage.Stage6_ExitLock)
            {
                return (LevelStage)nextIndex;
            }

            return currentStage; // 没有更多阶段
        }

        #endregion

        #region 编辑器支持

        /// <summary>
        /// 获取指定阶段的配置
        /// </summary>
        public StageConfig GetConfig(LevelStage stage)
        {
            switch (stage)
            {
                case LevelStage.Stage0_Awakening: return stage0Config;
                case LevelStage.Stage1_UI_Drag:   return stage1Config;
                case LevelStage.Stage2_QuestWindow: return stage2Config;
                case LevelStage.Stage3_HP_Resource: return stage3Config;
                case LevelStage.Stage4_NPC_Dialog: return stage4Config;
                case LevelStage.Stage5_DropdownMenu: return stage5Config;
                case LevelStage.Stage6_ExitLock: return stage6Config;
                default: return null;
            }
        }

        /// <summary>
        /// 获取阶段一配置
        /// </summary>
        public Stage1Config GetStage1Config() => stage1Config;

        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                InitializeConfigs();
            }
        }
        #endif

        #endregion
    }

    /// <summary>
    /// 阶段配置基类
    /// 每个阶段可以创建子类来定义特定逻辑
    /// </summary>
    [System.Serializable]
    public abstract class StageConfig
    {
        [Header("阶段设置")]
        [Tooltip("所属阶段")]
        public LevelStage stage;

        [Tooltip("下一个阶段")]
        public LevelStage nextStage = LevelStage.Stage1_UI_Drag;

        [Header("UI元素")]
        [Tooltip("该阶段需要显示的UI物体")]
        public GameObject[] showOnStart;

        [Tooltip("该阶段需要隐藏的UI物体")]
        public GameObject[] hideOnStart;

        [Header("完成条件")]
        [Tooltip("是否需要手动完成（否则自动检测）")]
        public bool manualCompletion = false;

        [Tooltip("完成条件描述")]
        [TextArea]
        public string completionDescription = "";

        // 内部状态
        [HideInInspector] public bool isActive;
        [HideInInspector] public bool isCompleted;

        /// <summary>
        /// 初始化配置
        /// </summary>
        public virtual void Initialize()
        {
            isCompleted = false;
            isActive = false;
        }

        /// <summary>
        /// 激活/停用该阶段配置
        /// </summary>
        public virtual void SetActive(bool active)
        {
            isActive = active;

            // 处理UI显示
            if (showOnStart != null)
            {
                foreach (var obj in showOnStart)
                {
                    if (obj != null)
                        obj.SetActive(active);
                }
            }

            if (hideOnStart != null)
            {
                foreach (var obj in hideOnStart)
                {
                    if (obj != null)
                        obj.SetActive(!active);
                }
            }
        }

        /// <summary>
        /// 检查阶段是否完成
        /// </summary>
        public abstract bool IsCompleted();

        /// <summary>
        /// 手动标记完成
        /// </summary>
        public virtual void MarkCompleted()
        {
            if (!manualCompletion)
            {
                Debug.LogWarning($"[StageConfig] 阶段 {stage} 不支持手动完成");
                return;
            }
            isCompleted = true;
        }
    }
}
