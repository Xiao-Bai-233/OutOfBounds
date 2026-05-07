using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace OutOfBounds.Puzzle
{
    /// <summary>
    /// 复选框权限面板 — Stage 6 PERMISSION 锁
    /// 三个复选框：
    ///   "I am not a bug"     → 错误（身份不匹配）
    ///   "Enable safe exit"   → 正确（授权通过）
    ///   "Accept deletion"    → 隐藏解（生成确认框可拖下作为资源）
    /// </summary>
    public class CheckboxPermission : MonoBehaviour
    {
        [Header("══ 三个复选框 ══")]
        [Tooltip("错误选项 1：I am not a bug")]
        [SerializeField] private Toggle wrongToggle1;

        [Tooltip("正确选项：Enable safe exit")]
        [SerializeField] private Toggle correctToggle;

        [Tooltip("错误选项 2 / 隐藏解：Accept deletion")]
        [SerializeField] private Toggle wrongToggle2;

        [Header("══ 反馈文本 ══")]
        [Tooltip("权限面板的状态文本")]
        [SerializeField] private Text feedbackText;

        [Tooltip("错误提示文本")]
        [SerializeField] private string wrongIdentityText = "Identity mismatch.";

        [Tooltip("授权通过文本")]
        [SerializeField] private string permissionGrantedText = "PERMISSION confirmed.";

        [Header("══ 隐藏解：Accept deletion 生成资源 ══")]
        [Tooltip("确认框预制体（Are you sure? YES / NO）")]
        [SerializeField] private GameObject confirmDialogPrefab;

        [Tooltip("确认框生成位置")]
        [SerializeField] private Transform confirmSpawnPoint;

        [Tooltip("倒计时持续时间（秒）")]
        [SerializeField] private float deletionCountdown = 5f;

        [Header("══ 锁定 ══")]
        [Tooltip("授权后是否锁定面板（防止反悔）")]
        [SerializeField] private bool lockOnCorrect = true;

        // 状态
        private bool isGranted;
        private bool isLocked;
        private bool deletionTriggered;

        // 事件
        public System.Action OnPermissionGranted;
        public System.Action OnPermissionRevoked;
        public System.Action<string> OnWrongChoice;

        public bool IsGranted => isGranted;

        #region Unity 生命周期

        private void Awake()
        {
            // 订阅 Toggle 事件
            if (wrongToggle1 != null)
                wrongToggle1.onValueChanged.AddListener((val) => { if (val) OnWrongToggleChecked(wrongToggle1, "I am not a bug"); });

            if (correctToggle != null)
                correctToggle.onValueChanged.AddListener((val) => { if (val) OnCorrectToggleChecked(); });

            if (wrongToggle2 != null)
                wrongToggle2.onValueChanged.AddListener((val) => { if (val) OnAcceptDeletionChecked(); });

            // ★ 强制所有 Toggle 初始为关闭（防止编辑器里手误点了勾")
            ResetPanel();
        }

        private void OnDestroy()
        {
            if (wrongToggle1 != null)
                wrongToggle1.onValueChanged.RemoveAllListeners();
            if (correctToggle != null)
                correctToggle.onValueChanged.RemoveAllListeners();
            if (wrongToggle2 != null)
                wrongToggle2.onValueChanged.RemoveAllListeners();
        }

        #endregion

        #region 复选框回调

        /// <summary>
        /// 错误选项 "I am not a bug"
        /// </summary>
        private void OnWrongToggleChecked(Toggle toggle, string choice)
        {
            if (isLocked) return;

            // 显示错误
            if (feedbackText != null)
                feedbackText.text = wrongIdentityText;

            // 弹回（取消勾选）
            toggle.isOn = false;

            OnWrongChoice?.Invoke(choice);
            Debug.Log($"[CheckboxPermission] 错误: {choice} → Identity mismatch.");
        }

        /// <summary>
        /// 正确选项 "Enable safe exit"
        /// </summary>
        private void OnCorrectToggleChecked()
        {
            if (isLocked) return;

            isGranted = true;

            if (feedbackText != null)
                feedbackText.text = permissionGrantedText;

            // 锁定面板
            if (lockOnCorrect)
            {
                isLocked = true;
                SetAllTogglesInteractable(false);
            }

            OnPermissionGranted?.Invoke();
            Debug.Log("[CheckboxPermission] PERMISSION confirmed — Enable safe exit.");
        }

        /// <summary>
        /// 隐藏解 "Accept deletion" — 生成确认框作为资源
        /// </summary>
        private void OnAcceptDeletionChecked()
        {
            if (isLocked || deletionTriggered) return;
            deletionTriggered = true;

            if (feedbackText != null)
            {
                feedbackText.text = $"Deleting in {deletionCountdown}...";
                StartCoroutine(DeletionCountdownRoutine());
            }

            Debug.Log("[CheckboxPermission] Accept deletion triggered — 隐藏解激活");
        }

        private System.Collections.IEnumerator DeletionCountdownRoutine()
        {
            float remaining = deletionCountdown;
            while (remaining > 0)
            {
                remaining -= Time.deltaTime;
                if (feedbackText != null)
                    feedbackText.text = $"Deleting in {Mathf.CeilToInt(remaining)}...";
                yield return null;
            }

            // 倒计时结束 → 生成确认框（Are you sure? YES / NO）
            SpawnConfirmDialog();
        }

        /// <summary>
        /// 生成确认框 — YES/NO 按钮可作为物理资源
        /// </summary>
        private void SpawnConfirmDialog()
        {
            if (confirmDialogPrefab == null) return;

            Vector3 spawnPos = confirmSpawnPoint != null
                ? confirmSpawnPoint.position
                : transform.position + Vector3.up * 2f;

            var dialogObj = Instantiate(confirmDialogPrefab, spawnPos, Quaternion.identity, transform.parent);

            // 确保有物理组件
            var physicsElement = dialogObj.GetComponent<UI.UIPhysicsElement>();
            if (physicsElement == null)
                physicsElement = dialogObj.AddComponent<UI.UIPhysicsElement>();

            var draggable = dialogObj.GetComponent<DragSystem.DraggableUI>();
            if (draggable == null)
                draggable = dialogObj.AddComponent<DragSystem.DraggableUI>();

            // 注册到物理管理器
            if (UI.UIPhysicsManager.Instance != null)
                UI.UIPhysicsManager.Instance.RegisterElement(physicsElement);

            if (feedbackText != null)
                feedbackText.text = "Are you sure?\n（YES / NO 按钮可以拖下来用）";

            Debug.Log("[CheckboxPermission] 确认框已生成 — NO 按钮可作为临时平台");
        }

        #endregion

        #region 辅助

        private void SetAllTogglesInteractable(bool interactable)
        {
            if (wrongToggle1 != null) wrongToggle1.interactable = interactable;
            if (correctToggle != null) correctToggle.interactable = interactable;
            if (wrongToggle2 != null) wrongToggle2.interactable = interactable;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 重置权限面板
        /// </summary>
        public void ResetPanel()
        {
            isGranted = false;
            isLocked = false;
            deletionTriggered = false;

            if (wrongToggle1 != null) wrongToggle1.isOn = false;
            if (correctToggle != null) correctToggle.isOn = false;
            if (wrongToggle2 != null) wrongToggle2.isOn = false;

            SetAllTogglesInteractable(true);

            if (feedbackText != null)
                feedbackText.text = "Exit Permission";

            OnPermissionRevoked?.Invoke();
        }

        #endregion
    }
}
