using UnityEngine;
using OutOfBounds.UI;

namespace OutOfBounds.Puzzle
{
    /// <summary>
    /// 阶段一配置：UI拖拽入门
    /// 任务：拖拽 "[SPACE]" 单词，将其垫在墙下，踩踏跳过
    /// </summary>
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

        [Tooltip("单词放置后是否需要玩家踩踏才能完成")]
        public bool requirePlayerStep = true;

        [Tooltip("玩家物体引用（用于检测踩踏）")]
        public Transform playerTransform;

        [Tooltip("完成前的延迟时间（秒）")]
        public float completionDelay = 1f;

        // 私有状态
        private bool wordPlaced = false;
        private float wordPlacedTime;
        private bool playerOnWord = false;

        // 构造函数设置阶段
        public Stage1Config()
        {
            stage = LevelStage.Stage1_UI_Drag;
            completionDescription = "拖拽 [SPACE] 单词到墙下方，踩踏跳过";
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            wordPlaced = false;
            playerOnWord = false;
            wordPlacedTime = 0f;

            // 订阅单词事件
            if (spaceWord != null)
            {
                spaceWord.OnWordDetached += OnWordDetached;
                spaceWord.OnWordDropped += OnWordDropped;
            }
        }

        /// <summary>
        /// 激活阶段
        /// </summary>
        public override void SetActive(bool active)
        {
            base.SetActive(active);

            if (!active)
            {
                // 停用时取消订阅
                if (spaceWord != null)
                {
                    spaceWord.OnWordDetached -= OnWordDetached;
                    spaceWord.OnWordDropped -= OnWordDropped;
                }
            }
            else
            {
                // 激活时重置状态
                ResetStageState();
            }
        }

        /// <summary>
        /// 检查是否完成
        /// </summary>
        public override bool IsCompleted()
        {
            if (!isActive) return false;

            // 情况1：单词已放置在目标区域
            if (wordPlaced && IsWordInTargetArea())
            {
                if (requirePlayerStep)
                {
                    // 情况2：玩家踩在单词上
                    if (IsPlayerOnWord())
                    {
                        return isCompleted;
                    }
                }
                else
                {
                    return isCompleted;
                }
            }

            return false;
        }

        /// <summary>
        /// 重置阶段状态
        /// </summary>
        public void ResetStageState()
        {
            wordPlaced = false;
            playerOnWord = false;
            wordPlacedTime = 0f;

            // 重置单词
            if (spaceWord != null && spaceWord.IsDetached)
            {
                spaceWord.ResetWord();
            }
        }

        /// <summary>
        /// 手动标记完成（用于调试或触发器）
        /// </summary>
        public override void MarkCompleted()
        {
            isCompleted = true;
        }

        #region 私有方法

        private void OnWordDetached(DraggableWord word)
        {
            Debug.Log("[Stage1] [SPACE] 单词已被拖拽脱离！");
            wordPlaced = false;
        }

        private void OnWordDropped(DraggableWord word)
        {
            Debug.Log("[Stage1] [SPACE] 单词已放下！");
            wordPlacedTime = Time.time;

            // 检测是否在目标区域
            if (IsWordInTargetArea())
            {
                wordPlaced = true;
                Debug.Log("[Stage1] [SPACE] 单词已放置到目标区域！");
            }
        }

        private bool IsWordInTargetArea()
        {
            if (spaceWord == null || targetArea == null) return false;

            Vector2 wordPos = spaceWord.Rect.anchoredPosition;
            Vector2 targetPos = targetArea.anchoredPosition;
            Vector2 targetSize = targetArea.sizeDelta;

            // 简单矩形检测
            bool inX = Mathf.Abs(wordPos.x - targetPos.x) < targetSize.x * 0.5f;
            bool inY = Mathf.Abs(wordPos.y - targetPos.y) < targetSize.y * 0.5f;

            return inX && inY;
        }

        private bool IsPlayerOnWord()
        {
            if (playerTransform == null || spaceWord == null) return false;

            // 检测玩家是否在单词上方
            Vector3 wordPos = spaceWord.Rect.position;
            Vector3 playerPos = playerTransform.position;

            float distanceX = Mathf.Abs(playerPos.x - wordPos.x);
            float distanceY = playerPos.y - wordPos.y;

            // 玩家在单词上方且距离较近
            return distanceX < 50f && distanceY > 0 && distanceY < 100f;
        }

        #endregion

        #region Unity 回调

        private void Update()
        {
            if (!isActive || isCompleted) return;

            // 持续检测玩家是否在单词上
            if (wordPlaced && requirePlayerStep)
            {
                playerOnWord = IsPlayerOnWord();
            }
        }

        #endregion
    }

    /// <summary>
    /// 阶段二配置（预留）
    /// </summary>
    [System.Serializable]
    public class Stage2Config : StageConfig
    {
        [Header("阶段二设置")]
        [Tooltip("占位描述")]
        public string description = "待实现...";

        public Stage2Config()
        {
            stage = LevelStage.Stage2_XXX;
            completionDescription = "阶段二完成条件：待定义";
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override bool IsCompleted()
        {
            // 占位 - 待实现
            return false;
        }
    }

    /// <summary>
    /// 阶段三配置（预留）
    /// </summary>
    [System.Serializable]
    public class Stage3Config : StageConfig
    {
        [Header("阶段三设置")]
        [Tooltip("占位描述")]
        public string description = "待实现...";

        public Stage3Config()
        {
            stage = LevelStage.Stage3_XXX;
            completionDescription = "阶段三完成条件：待定义";
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override bool IsCompleted()
        {
            // 占位 - 待实现
            return false;
        }
    }
}
