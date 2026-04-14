using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using OutOfBounds.UI;
using OutOfBounds.Core;

namespace OutOfBounds.Puzzle
{
    /// <summary>
    /// NPC 对话控制器
    /// 实现 IPointerClickHandler 接口，点击时生成物理气泡
    /// </summary>
    public class NPCDialogController : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IInteractable
    {
        [Header("对话设置")]
        [SerializeField] private string[] dialogs = new string[]
        {
            "你好，我是 NPC！",
            "这个世界有些不对劲...",
            "你发现那些可以踩踏的文字了吗？",
            "你可以把对话气泡当作垫脚石！",
            "反复点击我，我会说出更长的话来帮你跳得更高。"
        };

        [Header("气泡预制体配置")]
        [SerializeField] private GameObject dialogBubblePrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float bubbleSpawnOffset = 2f;
        [SerializeField] private Vector2 initialForce = new Vector2(0f, 2f);

        [Header("限制与冷却")]
        [SerializeField] private int maxTotalBubbles = 10; // 总共能生成的对话框上限
        [SerializeField] private float clickCooldown = 0.8f; // 每次点击的冷却时间
        [SerializeField] private bool loopDialogs = true; // 对话内容是否循环

        [Header("NPC 视觉反馈")]
        [SerializeField] private Color highlightColor = Color.yellow;
        private Color originalColor;
        private Image npcImage;

        private int currentDialogIndex = 0;
        private int totalBubblesGenerated = 0;
        private float lastClickTime = -1f;
        private bool canInteract = true;
        private GameObject currentActiveBubble; // 记录当前活跃的气泡

        #region Unity 生命周期

        private void Awake()
        {
            npcImage = GetComponent<Image>();
            if (npcImage != null)
            {
                originalColor = npcImage.color;
            }
        }

        #endregion

        #region IInteractable 实现

        public bool CanInteract => canInteract;

        public void OnInteractStart()
        {
            if (npcImage != null)
            {
                npcImage.color = highlightColor;
            }
        }

        public void OnInteractEnd()
        {
            if (npcImage != null)
            {
                npcImage.color = originalColor;
            }
        }

        #endregion

        #region IPointerClickHandler / Enter / Exit 实现

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (canInteract) OnInteractStart();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnInteractEnd();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!canInteract) return;

            // 1. 检查总上限
            if (totalBubblesGenerated >= maxTotalBubbles)
            {
                Debug.Log("[NPCDialog] 已经说得够多了。");
                canInteract = false;
                return;
            }

            // 2. 检查冷却时间
            if (Time.time - lastClickTime < clickCooldown) return;

            // 3. 检查对话索引
            if (!loopDialogs && currentDialogIndex >= dialogs.Length)
            {
                canInteract = false;
                return;
            }

            // 触发对话生成
            GenerateDialogBubble();

            lastClickTime = Time.time;
            totalBubblesGenerated++;

            // 索引循环逻辑
            if (loopDialogs)
            {
                currentDialogIndex = (currentDialogIndex + 1) % dialogs.Length;
            }
            else
            {
                currentDialogIndex++;
            }
        }

        #endregion

        /// <summary>
        /// 生成物理对话气泡
        /// </summary>
        private void GenerateDialogBubble()
        {
            if (dialogBubblePrefab == null)
            {
                Debug.LogError("[NPCDialog] 对话气泡预制体未分配！");
                return;
            }

            // 1. 如果已有旧气泡，先回收它（确保场景唯一）
            if (currentActiveBubble != null)
            {
                var bubbleElement = currentActiveBubble.GetComponent<DialogBubbleElement>();
                if (bubbleElement != null && UIPhysicsManager.Instance != null)
                {
                    UIPhysicsManager.Instance.RecycleBubbleElement(bubbleElement);
                }
                else
                {
                    Destroy(currentActiveBubble);
                }
            }

            // 2. 增加随机位置偏移，防止完全重叠导致“卡飞”
            Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.1f, 0.1f), 0);
            Vector3 spawnPosition = (spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up * bubbleSpawnOffset) + randomOffset;

            // 3. 使用对象池生成气泡
            DialogBubbleElement bubble;
            if (UIPhysicsManager.Instance != null)
            {
                bubble = UIPhysicsManager.Instance.SpawnBubbleElement(dialogBubblePrefab, spawnPosition, dialogs[currentDialogIndex], transform.parent);
                currentActiveBubble = bubble.gameObject;
            }
            else
            {
                // 回退方案
                currentActiveBubble = Instantiate(dialogBubblePrefab, transform.parent);
                currentActiveBubble.transform.position = spawnPosition;
                bubble = currentActiveBubble.GetComponent<DialogBubbleElement>();
                if (bubble == null) bubble = currentActiveBubble.AddComponent<DialogBubbleElement>();
                bubble.SetText(dialogs[currentDialogIndex]);
            }

            currentActiveBubble.name = $"DialogBubble_{currentDialogIndex}";

            // 4. 增加随机初始旋转，让它掉落时更自然
            currentActiveBubble.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-15f, 15f));

            // 5. 给一个向上且带微小随机侧向的初速度
            Vector2 randomForce = new Vector2(Random.Range(-1f, 1f), initialForce.y);
            bubble.AddForce(randomForce);

            // 视觉反馈效果
            OnInteractStart();
            Invoke(nameof(OnInteractEnd), 0.2f);
        }

        /// <summary>
        /// 动态设置对话内容（供外部调用）
        /// </summary>
        public void SetDialogs(string[] newDialogs)
        {
            if (newDialogs != null && newDialogs.Length > 0)
            {
                dialogs = newDialogs;
                currentDialogIndex = 0;
            }
        }
    }
}
