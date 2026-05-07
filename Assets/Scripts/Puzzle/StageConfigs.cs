using UnityEngine;
using OutOfBounds.UI;
using OutOfBounds.DragSystem;
using OutOfBounds.Physics;

namespace OutOfBounds.Puzzle
{
    // ═══════════════════════════════════════════════════════════════
    // Stage 0: 异常苏醒 — 基础移动教学
    // ═══════════════════════════════════════════════════════════════
    [System.Serializable]
    public class Stage0Config : StageConfig
    {
        [Header("阶段零特定设置")]
        [Tooltip("教学文本物体（Press [W] or [SPACE] to Jump）")]
        public GameObject tutorialText;

        [Tooltip("[SPACE] 单词物体（可高亮但此阶段不可拖拽）")]
        public DraggableWord spaceWordHint;

        [Tooltip("前方墙体（Stage 1 的入口标记）")]
        public GameObject entryWall;

        [Tooltip("玩家 Transform（用于检测移动）")]
        public Transform playerTransform;

        [Tooltip("需要玩家至少移动此距离才算完成")]
        public float minMoveDistance = 3f;

        [Tooltip("需要玩家成功跳跃才算完成")]
        public bool requireJump = true;

        private Vector3 playerStartPos;
        private bool hasJumped;

        public Stage0Config()
        {
            stage = LevelStage.Stage0_Awakening;
            completionDescription = "使用 WASD 移动，按 Space/W 跳跃";
        }

        public override void Initialize()
        {
            base.Initialize();
            hasJumped = false;
            if (playerTransform != null)
                playerStartPos = playerTransform.position;
        }

        public override void SetActive(bool active)
        {
            base.SetActive(active);
            if (active)
            {
                hasJumped = false;
                if (playerTransform != null)
                    playerStartPos = playerTransform.position;
            }
        }

        public override bool IsCompleted()
        {
            if (!isActive) return false;

            // 检测移动距离
            bool movedEnough = playerTransform != null &&
                Vector3.Distance(playerTransform.position, playerStartPos) > minMoveDistance;

            // 检测跳跃（通过 Input 检测）
            if (requireJump && !hasJumped)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W))
                    hasJumped = true;
            }

            return movedEnough && (!requireJump || hasJumped);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Stage 1: 跳不过去的墙 — 文本拆解 + 物理平台
    // ═══════════════════════════════════════════════════════════════
    [System.Serializable]
    public class Stage1Config : StageConfig
    {
        [Header("阶段一特定设置")]
        [Tooltip("单词 [SPACE]")]
        public DraggableWord spaceWord;

        [Tooltip("高墙物体（玩家需要跳过的障碍）")]
        public RectTransform wallObject;

        [Tooltip("地面检测区域")]
        public RectTransform groundArea;

        [Tooltip("单词需要放置的目标区域（墙下方）")]
        public RectTransform targetArea;

        [Tooltip("单词的上下文限制（控制 BrokenGlyph 范围）")]
        public UIContextConstraint contextConstraint;

        [Tooltip("单词放置后是否需要玩家踩踏才能完成")]
        public bool requirePlayerStep = true;

        [Tooltip("玩家物体引用（用于检测踩踏）")]
        public Transform playerTransform;

        [Tooltip("完成前的延迟时间（秒）")]
        public float completionDelay = 1f;

        private bool wordPlaced = false;
        private float wordPlacedTime;
        private bool playerOnWord = false;

        public Stage1Config()
        {
            stage = LevelStage.Stage1_UI_Drag;
            completionDescription = "拖拽 [SPACE] 单词到墙下方，踩踏跳过";
        }

        public override void Initialize()
        {
            base.Initialize();
            wordPlaced = false;
            playerOnWord = false;
            wordPlacedTime = 0f;

            if (spaceWord != null)
            {
                spaceWord.OnWordDetached += OnWordDetached;
                spaceWord.OnWordDropped += OnWordDropped;
            }

            // ★ 启用上下文限制：检测 BrokenGlyph
            if (contextConstraint != null)
            {
                contextConstraint.OnConstraintViolated += OnWordBroken;
                contextConstraint.OnConstraintRestored += OnWordRestored;
            }
        }

        public override void SetActive(bool active)
        {
            base.SetActive(active);

            if (!active)
            {
                if (spaceWord != null)
                {
                    spaceWord.OnWordDetached -= OnWordDetached;
                    spaceWord.OnWordDropped -= OnWordDropped;
                }
                if (contextConstraint != null)
                {
                    contextConstraint.OnConstraintViolated -= OnWordBroken;
                    contextConstraint.OnConstraintRestored -= OnWordRestored;
                }
            }
            else
            {
                ResetStageState();
            }
        }

        public override bool IsCompleted()
        {
            if (!isActive) return false;

            if (wordPlaced && IsWordInTargetArea())
            {
                if (requirePlayerStep)
                {
                    if (IsPlayerOnWord())
                        return isCompleted;
                }
                else
                {
                    return isCompleted;
                }
            }

            return false;
        }

        public void ResetStageState()
        {
            wordPlaced = false;
            playerOnWord = false;
            wordPlacedTime = 0f;

            if (spaceWord != null && spaceWord.IsDetached)
                spaceWord.ResetWord();
        }

        public override void MarkCompleted()
        {
            isCompleted = true;
        }

        #region 事件回调

        private void OnWordDetached(DraggableWord word)
        {
            Debug.Log("[Stage1] [SPACE] 单词已被拖拽脱离！");
            wordPlaced = false;
        }

        private void OnWordDropped(DraggableWord word)
        {
            Debug.Log("[Stage1] [SPACE] 单词已放下！");
            wordPlacedTime = Time.time;

            if (IsWordInTargetArea())
            {
                wordPlaced = true;
                Debug.Log("[Stage1] [SPACE] 单词已放置到目标区域！");
            }
        }

        private void OnWordBroken(UIContextConstraint constraint)
        {
            Debug.Log("[Stage1] [SPACE] 单词变成 Broken Glyph — 离开语义区域");
            wordPlaced = false;
        }

        private void OnWordRestored(UIContextConstraint constraint)
        {
            Debug.Log("[Stage1] [SPACE] 单词恢复 — 回到语义区域");
        }

        #endregion

        #region 检测方法

        private bool IsWordInTargetArea()
        {
            if (spaceWord == null || targetArea == null) return false;

            Vector2 wordPos = spaceWord.transform.position;
            Vector2 targetPos = targetArea.transform.position;
            Vector2 targetSize = targetArea.sizeDelta;

            bool inX = Mathf.Abs(wordPos.x - targetPos.x) < targetSize.x * 0.5f;
            bool inY = Mathf.Abs(wordPos.y - targetPos.y) < targetSize.y * 0.5f;

            return inX && inY;
        }

        private bool IsPlayerOnWord()
        {
            if (playerTransform == null || spaceWord == null) return false;

            Vector3 wordPos = spaceWord.Rect.position;
            Vector3 playerPos = playerTransform.position;

            float distanceX = Mathf.Abs(playerPos.x - wordPos.x);
            float distanceY = playerPos.y - wordPos.y;

            return distanceX < 50f && distanceY > 0 && distanceY < 100f;
        }

        #endregion

        #region Unity 回调

        private void Update()
        {
            if (!isActive || isCompleted) return;

            if (wordPlaced && requirePlayerStep)
                playerOnWord = IsPlayerOnWord();
        }

        #endregion
    }

    // ═══════════════════════════════════════════════════════════════
    // Stage 2: 任务窗口桥 — Quest Log 搭桥 + 区域信号绑定
    // ═══════════════════════════════════════════════════════════════
    [System.Serializable]
    public class Stage2Config : StageConfig
    {
        [Header("阶段二特定设置")]
        [Tooltip("任务窗口 (Quest Log)")]
        public TaskWindow questWindow;

        [Tooltip("窗口的 UI 物理元素")]
        public UIPhysicsElement windowPhysicsElement;

        [Tooltip("坑中两个损坏数据柱之间的桥面检测区域")]
        public RectTransform bridgeArea;

        [Tooltip("窗口的上下文限制（信号区绑定）")]
        public UIContextConstraint contextConstraint;

        [Tooltip("信号区域 Transform — Quest Log 的有效信号区")]
        public Transform signalRegion;

        [Tooltip("玩家物体引用")]
        public Transform playerTransform;

        [Tooltip("是否需要玩家踩踏窗口过坑")]
        public bool requirePlayerCross = true;

        private bool windowOnBridge;
        private bool playerCrossed;

        public Stage2Config()
        {
            stage = LevelStage.Stage2_QuestWindow;
            completionDescription = "按 TAB 召唤 Quest Log，拖到坑中间搭桥跳过";
        }

        public override void Initialize()
        {
            base.Initialize();
            windowOnBridge = false;
            playerCrossed = false;

            if (questWindow != null)
            {
                var draggable = questWindow.GetComponent<DraggableUI>();
                if (draggable != null)
                    draggable.OnDragEnd += OnWindowDropped;
            }

            if (contextConstraint != null)
            {
                contextConstraint.OnConstraintViolated += OnWindowOutOfRange;
                contextConstraint.OnConstraintRestored += OnWindowReturned;
            }
        }

        public override void SetActive(bool active)
        {
            base.SetActive(active);

            if (!active)
            {
                if (questWindow != null)
                {
                    var draggable = questWindow.GetComponent<DraggableUI>();
                    if (draggable != null)
                        draggable.OnDragEnd -= OnWindowDropped;
                }
                if (contextConstraint != null)
                {
                    contextConstraint.OnConstraintViolated -= OnWindowOutOfRange;
                    contextConstraint.OnConstraintRestored -= OnWindowReturned;
                }
            }
            else
            {
                windowOnBridge = false;
                playerCrossed = false;
            }
        }

        public override bool IsCompleted()
        {
            if (!isActive) return false;

            // 窗口放在桥面区域上
            if (!windowOnBridge && IsWindowOnBridge())
            {
                windowOnBridge = true;
                Debug.Log("[Stage2] Quest Log 已稳定放置在桥面上！");
            }

            if (windowOnBridge && requirePlayerCross && !playerCrossed)
            {
                if (playerTransform != null && bridgeArea != null)
                {
                    Vector3 playerPos = playerTransform.position;
                    Vector3 bridgePos = bridgeArea.position;
                    if (playerPos.x > bridgePos.x + bridgeArea.sizeDelta.x * 0.5f)
                    {
                        playerCrossed = true;
                        Debug.Log("[Stage2] 玩家已通过窗口桥！");
                    }
                }
            }

            return windowOnBridge && (!requirePlayerCross || playerCrossed);
        }

        private bool IsWindowOnBridge()
        {
            if (questWindow == null || bridgeArea == null) return false;

            var physicsElement = questWindow.GetComponent<UIPhysicsElement>();
            if (physicsElement == null) return false;

            // 窗口必须已落地（速度很小）
            if (physicsElement.GetVelocity().magnitude > 0.5f) return false;

            Vector2 windowPos = questWindow.transform.position;
            Vector2 bridgePos = bridgeArea.position;
            Vector2 bridgeSize = bridgeArea.sizeDelta;

            bool inX = Mathf.Abs(windowPos.x - bridgePos.x) < bridgeSize.x * 0.5f;
            bool inY = Mathf.Abs(windowPos.y - bridgePos.y) < bridgeSize.y * 0.5f;

            return inX && inY;
        }

        private void OnWindowDropped(DraggableUI draggable)
        {
            Debug.Log("[Stage2] Quest Log 已放下");
        }

        private void OnWindowOutOfRange(UIContextConstraint constraint)
        {
            Debug.Log("[Stage2] Quest Log 离开信号区 — 即将透明化并吸回");
        }

        private void OnWindowReturned(UIContextConstraint constraint)
        {
            Debug.Log("[Stage2] Quest Log 回到信号区");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Stage 3: 疼痛的代价 — HP 心形资源化 + 毒水腐蚀
    // ═══════════════════════════════════════════════════════════════
    [System.Serializable]
    public class Stage3Config : StageConfig
    {
        [Header("阶段三特定设置")]
        [Tooltip("血条管理器")]
        public HealthBarManager healthBarManager;

        [Tooltip("地刺陷阱")]
        public SpikeTrap spikeTrap;

        [Tooltip("毒水坑")]
        public PoisonPit poisonPit;

        [Tooltip("毒水坑的上下文约束区域（心形在此区域内有效）")]
        public Transform damageArea;

        [Tooltip("玩家物体引用")]
        public Transform playerTransform;

        [Tooltip("需要消耗的心形数量")]
        public int heartsRequired = 2;

        [Tooltip("玩家是否已通过毒水坑")]
        public bool playerCrossedPoison;

        private int initialHP;
        private int heartsConsumed;

        public Stage3Config()
        {
            stage = LevelStage.Stage3_HP_Resource;
            completionDescription = "踩地刺掉落心形，将心形拖入毒水坑作为踏点通过";
        }

        public override void Initialize()
        {
            base.Initialize();
            heartsConsumed = 0;
            playerCrossedPoison = false;

            if (healthBarManager != null)
                initialHP = healthBarManager.CurrentHealth;
        }

        public override bool IsCompleted()
        {
            if (!isActive) return false;

            // 计算消耗的心形
            if (healthBarManager != null)
            {
                int currentHP = healthBarManager.CurrentHealth;
                heartsConsumed = Mathf.Max(0, initialHP - currentHP);
            }

            // 检测玩家是否通过毒水坑
            if (!playerCrossedPoison && poisonPit != null && playerTransform != null)
            {
                if (playerTransform.position.x > poisonPit.transform.position.x + 5f)
                    playerCrossedPoison = true;
            }

            return heartsConsumed >= heartsRequired && playerCrossedPoison;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Stage 4: 喋喋不休的NPC — 文本长度驱动体积 + 扫描遮挡
    // ═══════════════════════════════════════════════════════════════
    [System.Serializable]
    public class Stage4Config : StageConfig
    {
        [Header("阶段四特定设置")]
        [Tooltip("NPC 对话控制器")]
        public NPCDialogController npcDialog;

        [Tooltip("NPC 的上下文约束（对话框依附限制）")]
        public UIContextConstraint npcConstraint;

        [Tooltip("扫描光线物体（玩家靠近被弹回）")]
        public GameObject scanLight;

        [Tooltip("高台顶部检测区域")]
        public Transform highPlatformTop;

        [Tooltip("玩家物体引用")]
        public Transform playerTransform;

        [Tooltip("需要的对话框数量")]
        public int dialogsRequired = 3;

        private int dialogsGenerated;
        private bool playerOnPlatform;

        public Stage4Config()
        {
            stage = LevelStage.Stage4_NPC_Dialog;
            completionDescription = "反复点击 NPC 生成对话框，用最大的对话框爬高台";
        }

        public override void Initialize()
        {
            base.Initialize();
            dialogsGenerated = 0;
            playerOnPlatform = false;
        }

        public override bool IsCompleted()
        {
            if (!isActive) return false;

            // 检测玩家是否到达高台
            if (highPlatformTop != null && playerTransform != null)
            {
                if (playerTransform.position.y >= highPlatformTop.position.y - 1f)
                    playerOnPlatform = true;
            }

            return playerOnPlatform;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Stage 5: 折叠菜单梯 — 下拉菜单展开结构（预留给 Phase 3）
    // ═══════════════════════════════════════════════════════════════
    [System.Serializable]
    public class Stage5Config : StageConfig
    {
        [Header("阶段五特定设置（待 Phase 3 实现）")]
        [Tooltip("菜单栏物体 (FILE)")]
        public GameObject menuBar;

        [Tooltip("菜单栏的上下文限制")]
        public UIContextConstraint menuConstraint;

        [Tooltip("玩家物体引用")]
        public Transform playerTransform;

        public Stage5Config()
        {
            stage = LevelStage.Stage5_DropdownMenu;
            completionDescription = "点击 FILE 展开菜单，拖到墙边形成阶梯跳上墙（待实现）";
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override bool IsCompleted()
        {
            if (!isActive) return false;
            // 待 Phase 3 实现完整逻辑
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Stage 6: 出口三重锁 — 综合测试（预留给 Phase 3）
    // ═══════════════════════════════════════════════════════════════
    [System.Serializable]
    public class Stage6Config : StageConfig
    {
        [Header("阶段六特定设置")]
        [Tooltip("出口大门")]
        public ExitGate exitGate;

        [Tooltip("红色压力按钮（WEIGHT 锁）")]
        public PressureButton weightButton;

        [Tooltip("授权面板 — 复选框组（PERMISSION 锁）")]
        public GameObject permissionPanel;

        [Tooltip("正确复选框 (Enable safe exit)")]
        public GameObject correctCheckbox;

        [Tooltip("错误复选框 (I am not a bug)")]
        public GameObject wrongCheckbox1;

        [Tooltip("错误复选框 (Accept deletion)")]
        public GameObject wrongCheckbox2;

        [Tooltip("空中悬浮文字 LEVEL CLEAR")]
        public DraggableWord levelClearWord;

        [Tooltip("EXIT 门的命令槽（COMMAND 锁）")]
        public Transform commandSlot;

        [Header("三重锁状态")]
        [Tooltip("WEIGHT 是否已确认")]
        public bool weightConfirmed;

        [Tooltip("PERMISSION 是否已确认")]
        public bool permissionConfirmed;

        [Tooltip("COMMAND 是否已确认")]
        public bool commandConfirmed;

        public Stage6Config()
        {
            stage = LevelStage.Stage6_ExitLock;
            completionDescription = "用窗口压按钮(WEIGHT) + 勾选正确复选框(PERMISSION) + 拖 CLEAR 到命令槽(COMMAND)";
        }

        public override void Initialize()
        {
            base.Initialize();
            weightConfirmed = false;
            permissionConfirmed = false;
            commandConfirmed = false;
        }

        public override bool IsCompleted()
        {
            if (!isActive) return false;

            // WEIGHT: 压力按钮被按下
            if (!weightConfirmed && weightButton != null)
            {
                // 通过 PressureButton 的 OnPressed 事件追踪
            }

            // PERMISSION: 正确复选框被勾选
            // COMMAND: CLEAR 被拖入命令槽
            // 待 Phase 3 实现完整逻辑

            return weightConfirmed && permissionConfirmed && commandConfirmed;
        }
    }

}
