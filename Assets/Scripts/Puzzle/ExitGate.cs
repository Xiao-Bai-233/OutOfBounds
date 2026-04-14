using UnityEngine;
using OutOfBounds.Core;

namespace OutOfBounds.Puzzle
{
    /// <summary>
    /// 出口大门组件
    /// 响应按钮或其他机关的信号，播放 Open 动画并管理通过逻辑
    /// </summary>
    public class ExitGate : MonoBehaviour
    {
        [Header("设置")]
        [SerializeField] private bool isOpenOnStart = false;
        [SerializeField] private bool autoClose = false; // 是否在按钮释放后自动关闭

        [Header("组件引用")]
        [SerializeField] private Animator animator;
        [SerializeField] private Collider2D gateCollider; // 阻挡玩家的大门碰撞体

        [Header("动画参数")]
        [SerializeField] private string openTrigger = "Open";
        [SerializeField] private string closeTrigger = "Close";
        [SerializeField] private string isOpenBool = "IsOpen";

        private bool isOpen;

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (gateCollider == null) gateCollider = GetComponent<Collider2D>();
            
            isOpen = isOpenOnStart;
            UpdateGateState();
        }

        /// <summary>
        /// 开启大门
        /// </summary>
        public void Open()
        {
            if (isOpen) return;
            
            isOpen = true;
            if (animator != null)
            {
                animator.SetBool(isOpenBool, true);
                animator.SetTrigger(openTrigger);
            }
            
            // 延迟一点时间或者根据动画帧关闭碰撞体
            // 这里简单处理，立即关闭
            if (gateCollider != null) gateCollider.enabled = false;
            
            Debug.Log($"[ExitGate] {name} 已开启");
        }

        /// <summary>
        /// 关闭大门
        /// </summary>
        public void Close()
        {
            if (!isOpen) return;
            
            isOpen = false;
            if (animator != null)
            {
                animator.SetBool(isOpenBool, false);
                animator.SetTrigger(closeTrigger);
            }
            
            if (gateCollider != null) gateCollider.enabled = true;
            
            Debug.Log($"[ExitGate] {name} 已关闭");
        }

        /// <summary>
        /// 响应按钮状态变化
        /// </summary>
        public void SetStateFromButton(bool isPressed)
        {
            if (isPressed)
            {
                Open();
            }
            else if (autoClose)
            {
                Close();
            }
        }

        private void UpdateGateState()
        {
            if (animator != null)
            {
                animator.SetBool(isOpenBool, isOpen);
            }
            
            if (gateCollider != null)
            {
                gateCollider.enabled = !isOpen;
            }
        }
        
        // 当玩家进入开放的大门时触发关卡完成
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isOpen && other.CompareTag("Player"))
            {
                Debug.Log("[ExitGate] 玩家已成功穿过出口！");
                // 调用全局游戏管理器完成关卡
                if (OutOfBounds.Physics.GameManager.Instance != null)
                {
                    OutOfBounds.Physics.GameManager.Instance.CompleteLevel();
                }
            }
        }
    }
}
