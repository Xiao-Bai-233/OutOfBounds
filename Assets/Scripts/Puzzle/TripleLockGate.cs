using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using OutOfBounds.UI;
using OutOfBounds.Core;

namespace OutOfBounds.Puzzle
{
    /// <summary>
    /// 三重锁大门 — Stage 6 出口
    /// 需要同时满足三个条件才能打开：
    ///   WEIGHT    — 窗口压住红色按钮
    ///   PERMISSION — 勾选正确复选框
    ///   COMMAND   — 将 CLEAR 文字拖入命令槽
    /// </summary>
    public class TripleLockGate : MonoBehaviour
    {
        [Header("══ 大门 ══")]
        [Tooltip("大门 Animator")]
        [SerializeField] private Animator gateAnimator;

        [Tooltip("大门碰撞体（阻挡玩家）")]
        [SerializeField] private Collider2D gateCollider;

        [Tooltip("门后的通关触发器 — 玩家走进来就通关")]
        [SerializeField] private Collider2D exitTrigger;

        [Tooltip("门上的状态显示文本")]
        [SerializeField] private Text statusText;

        [Header("══ 第一锁：WEIGHT ══")]
        [Tooltip("压力按钮（被窗口压住即解锁）")]
        [SerializeField] private PressureButton weightButton;

        [Tooltip("是否已确认 WEIGHT")]
        [SerializeField] private bool weightConfirmed;

        [Header("══ 第二锁：PERMISSION ══")]
        [Tooltip("复选框权限面板")]
        [SerializeField] private CheckboxPermission permissionPanel;

        [Tooltip("是否已确认 PERMISSION")]
        [SerializeField] private bool permissionConfirmed;

        [Header("══ 第三锁：COMMAND ══")]
        [Tooltip("命令槽 Transform — CLEAR 文字需要拖入此位置")]
        [SerializeField] private Transform commandSlot;

        [Tooltip("命令槽的有效检测半径")]
        [SerializeField] private float commandSlotRadius = 1.5f;

        [Tooltip("需要放入命令槽的文字")]
        [SerializeField] private DraggableWord requiredWord; // CLEAR

        [Tooltip("是否已确认 COMMAND")]
        [SerializeField] private bool commandConfirmed;

        [Header("══ 门后坑 ══")]
        [Tooltip("LEVEL 文字 — 可用于铺路过坑")]
        [SerializeField] private DraggableWord levelWord; // LEVEL

        // 事件
        public System.Action<string> OnLockStateChanged;
        public System.Action OnGateOpened;

        private bool isOpen;

        #region Unity 生命周期

        private void Awake()
        {
            if (gateAnimator == null) gateAnimator = GetComponent<Animator>();
            if (gateCollider == null) gateCollider = GetComponent<Collider2D>();
            if (exitTrigger != null) exitTrigger.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isOpen) return;
            if (!other.CompareTag("Player")) return;

            Debug.Log("[TripleLock] 玩家通过大门！LEVEL CLEAR.");
            // ★ 走新系统：存档 + 回到选关界面（淡入淡出）
            if (OutOfBounds.Physics.GameManager.Instance != null)
            {
                OutOfBounds.Physics.GameManager.Instance.CompleteLevel();
            }
            else
            {
                Debug.LogWarning("[TripleLock] 未找到 GameManager，回退到直接加载主菜单");
                if (SceneLoader.Instance != null)
                    SceneLoader.Instance.LoadMainMenu();
            }
        }

        private void Start()
        {
            // 订阅 WEIGHT 锁 — 监听压力按钮
            if (weightButton != null)
            {
                weightButton.OnPressed.AddListener(OnWeightPressed);
                weightButton.OnReleased.AddListener(OnWeightReleased);
            }

            // 订阅 PERMISSION 锁 — 监听复选框
            if (permissionPanel != null)
            {
                permissionPanel.OnPermissionGranted += OnPermissionGranted;
                permissionPanel.OnPermissionRevoked += OnPermissionRevoked;
                permissionPanel.OnWrongChoice += OnWrongChoice;
            }

            UpdateStatusDisplay();
        }

        private void Update()
        {
            // 持续检测 COMMAND 锁 — 文字是否在命令槽范围内
            if (!commandConfirmed && requiredWord != null && commandSlot != null)
            {
                float distance = Vector3.Distance(requiredWord.transform.position, commandSlot.position);
                if (distance < commandSlotRadius)
                {
                    ConfirmCommand();
                }
            }
        }

        private void OnDestroy()
        {
            if (weightButton != null)
            {
                weightButton.OnPressed.RemoveListener(OnWeightPressed);
                weightButton.OnReleased.RemoveListener(OnWeightReleased);
            }
            if (permissionPanel != null)
            {
                permissionPanel.OnPermissionGranted -= OnPermissionGranted;
                permissionPanel.OnPermissionRevoked -= OnPermissionRevoked;
                permissionPanel.OnWrongChoice -= OnWrongChoice;
            }
        }

        #endregion

        #region 三重锁回调

        // ── WEIGHT ──
        private void OnWeightPressed()
        {
            weightConfirmed = true;
            UpdateStatusDisplay();
            Debug.Log("[TripleLock] WEIGHT confirmed.");
            CheckAllLocks();
        }

        private void OnWeightReleased()
        {
            weightConfirmed = false;
            UpdateStatusDisplay();
            Debug.Log("[TripleLock] WEIGHT lost — 窗口被移开");
            CheckAllLocks(); // 关门
        }

        // ── PERMISSION ──
        private void OnPermissionGranted()
        {
            permissionConfirmed = true;
            UpdateStatusDisplay();
            Debug.Log("[TripleLock] PERMISSION confirmed.");
            CheckAllLocks();
        }

        private void OnPermissionRevoked()
        {
            permissionConfirmed = false;
            UpdateStatusDisplay();
            Debug.Log("[TripleLock] PERMISSION revoked.");
            CheckAllLocks(); // 关门
        }

        private void OnWrongChoice(string choice)
        {
            Debug.Log($"[TripleLock] 错误选择: {choice}");
        }

        // ── COMMAND ──
        private void ConfirmCommand()
        {
            commandConfirmed = true;
            UpdateStatusDisplay();
            Debug.Log("[TripleLock] COMMAND confirmed — CLEAR 已放入命令槽");

            // ★ 回收 CLEAR 文字（不再是可拖拽的物理物体）
            if (requiredWord != null)
            {
                var pe = requiredWord.GetComponent<UIPhysicsElement>();
                if (pe != null && UIPhysicsManager.Instance != null)
                    UIPhysicsManager.Instance.RecycleHeartElement(pe);
                else
                    Destroy(requiredWord.gameObject);
            }

            CheckAllLocks();
        }

        #endregion

        #region 大门控制

        private void CheckAllLocks()
        {
            if (weightConfirmed && permissionConfirmed && commandConfirmed && !isOpen)
            {
                OpenGate();
            }
            else if (!weightConfirmed || !permissionConfirmed)
            {
                // 任一锁松开 → 关门（COMMAND 一旦放入保持锁定，不需要重试）
                CloseGate();
            }
        }

        private void OpenGate()
        {
            if (isOpen) return;
            isOpen = true;

            if (gateAnimator != null)
            {
                gateAnimator.SetBool("IsOpen", true);
                gateAnimator.SetTrigger("Open");
            }

            if (gateCollider != null)
                gateCollider.enabled = false;

            OnGateOpened?.Invoke();

            StartCoroutine(ShowClearText());
            Debug.Log("[TripleLock] ✨ 大门已开启！LEVEL CLEAR.");
        }

        private void CloseGate()
        {
            if (!isOpen) return;
            isOpen = false;

            if (gateAnimator != null)
            {
                gateAnimator.SetBool("IsOpen", false);
                gateAnimator.SetTrigger("Close");
            }

            if (gateCollider != null)
                gateCollider.enabled = true;

            Debug.Log("[TripleLock] 大门的锁重新激活");
        }

        private IEnumerator ShowClearText()
        {
            if (statusText != null)
            {
                statusText.text = "LEVEL CLEAR.\nExternal pointer interference confirmed.\nCaret-7 classification updated: BUG / SENTIENT.";
                statusText.color = Color.green;
            }
            yield return new WaitForSeconds(5f);
        }

        private void UpdateStatusDisplay()
        {
            string status = "EXIT LOCKED\n";

            status += weightConfirmed    ? "WEIGHT confirmed.\n"    : "Missing: WEIGHT\n";
            status += permissionConfirmed ? "PERMISSION confirmed.\n" : "Missing: PERMISSION\n";
            status += commandConfirmed   ? "COMMAND confirmed.\n"   : "Missing: COMMAND\n";

            if (statusText != null)
                statusText.text = status;

            OnLockStateChanged?.Invoke(status);
        }

        #endregion

        #region 公共属性

        public bool IsOpen => isOpen;
        public bool WeightConfirmed => weightConfirmed;
        public bool PermissionConfirmed => permissionConfirmed;
        public bool CommandConfirmed => commandConfirmed;

        #endregion

        #region 编辑器可视化

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 命令槽范围
            if (commandSlot != null)
            {
                Gizmos.color = commandConfirmed ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(commandSlot.position, commandSlotRadius);
            }
        }
        #endif

        #endregion
    }
}
