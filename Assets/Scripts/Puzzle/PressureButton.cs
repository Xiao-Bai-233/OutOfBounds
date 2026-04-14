using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using OutOfBounds.Core;
using OutOfBounds.UI;

namespace OutOfBounds.Puzzle
{
    /// <summary>
    /// 压力按钮组件
    /// 检测物理对象（玩家或 UIPhysicsElement）是否压在上面
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PressureButton : MonoBehaviour
    {
        [Header("设置")]
        [SerializeField] private LayerMask detectionLayer;
        [SerializeField] private float requiredMass = 0.1f; // 触发所需的最小总质量
        
        [Header("视觉反馈")]
        [SerializeField] private Transform buttonVisual; // 按钮的可移动部分
        [SerializeField] private Vector3 pressedOffset = new Vector3(0, -0.1f, 0);
        [SerializeField] private float smoothSpeed = 10f;

        [Header("目标关联")]
        [SerializeField] private List<ExitGate> targetGates = new List<ExitGate>();
        [SerializeField] private bool toggleOnRelease = true; // 松开后是否关闭大门

        [Header("事件")]
        public UnityEvent OnPressed;
        public UnityEvent OnReleased;

        private HashSet<Collider2D> objectsOnButton = new HashSet<Collider2D>();
        private List<Collider2D> currentOverlaps = new List<Collider2D>();
        private bool isPressed = false;
        private Vector3 initialVisualPos;
        private Vector3 targetVisualPos;
        private Collider2D triggerCollider;

        private void Awake()
        {
            triggerCollider = GetComponent<Collider2D>();
            if (buttonVisual != null)
            {
                initialVisualPos = buttonVisual.localPosition;
                targetVisualPos = initialVisualPos;
            }
            
            // 确保 Collider 是触发器
            if (triggerCollider != null) triggerCollider.isTrigger = true;
        }

        private void Update()
        {
            // 平滑移动按钮视觉效果
            if (buttonVisual != null)
            {
                buttonVisual.localPosition = Vector3.Lerp(buttonVisual.localPosition, targetVisualPos, Time.deltaTime * smoothSpeed);
            }
        }

        private void FixedUpdate()
        {
            // 每物理帧检测一次重叠，支持没有 Rigidbody2D 的物体
            DetectObjects();
        }

        private void DetectObjects()
        {
            if (triggerCollider == null) return;

            // 使用 OverlapBox 检测所有重叠的物体
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(detectionLayer);
            filter.useLayerMask = true;
            filter.useTriggers = true;

            int count = Physics2D.OverlapCollider(triggerCollider, filter, currentOverlaps);
            
            objectsOnButton.Clear();
            for (int i = 0; i < count; i++)
            {
                objectsOnButton.Add(currentOverlaps[i]);
            }

            CheckButtonState();
        }

        private void CheckButtonState()
        {
            float totalMass = 0f;
            foreach (var col in objectsOnButton)
            {
                if (col == null) continue;
                
                // 尝试获取物理对象质量
                var physicsObj = col.GetComponent<IPhysicsObject>();
                if (physicsObj != null)
                {
                    totalMass += physicsObj.Mass;
                }
                else if (col.CompareTag("Player"))
                {
                    // 如果是玩家，假设一个质量
                    totalMass += 1.0f; 
                }
            }

            bool shouldBePressed = totalMass >= requiredMass;

            if (shouldBePressed && !isPressed)
            {
                Press();
            }
            else if (!shouldBePressed && isPressed)
            {
                Release();
            }
        }

        private void Press()
        {
            isPressed = true;
            targetVisualPos = initialVisualPos + pressedOffset;
            
            // 开启大门
            foreach (var gate in targetGates)
            {
                if (gate != null) gate.Open();
            }
            
            OnPressed?.Invoke();
            Debug.Log($"[PressureButton] {name} 已按下 (质量: {CalculateCurrentMass()})");
        }

        private void Release()
        {
            isPressed = false;
            targetVisualPos = initialVisualPos;
            
            // 关闭大门 (如果开启了释放触发)
            if (toggleOnRelease)
            {
                foreach (var gate in targetGates)
                {
                    if (gate != null) gate.Close();
                }
            }
            
            OnReleased?.Invoke();
            Debug.Log($"[PressureButton] {name} 已松开");
        }

        private float CalculateCurrentMass()
        {
            float total = 0f;
            foreach (var col in objectsOnButton)
            {
                if (col == null) continue;
                var p = col.GetComponent<IPhysicsObject>();
                if (p != null) total += p.Mass;
                else if (col.CompareTag("Player")) total += 1.0f;
            }
            return total;
        }
    }
}
