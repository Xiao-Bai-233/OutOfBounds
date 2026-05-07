using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using OutOfBounds.Core;
using OutOfBounds.UI;
using OutOfBounds.DragSystem;

namespace OutOfBounds.Puzzle
{
    /// <summary>
    /// 下拉菜单控制器 — Stage 5 折叠菜单梯
    /// 点击 FILE 展开菜单项（New/Open/Save/Export/Exit），每个项有碰撞体。
    /// 菜单项垂直排列形成阶梯，玩家可踩踏爬上高墙。
    /// 父级菜单栏可拖拽到墙边，子项保持相对位置。
    /// 离开有效区域后自动折叠。
    /// </summary>
    public class DropdownMenuController : MonoBehaviour, IPointerClickHandler
    {
        [Header("══ 菜单项 ══")]
        [Tooltip("展开后的子菜单项（按从上到下顺序排列）")]
        [SerializeField] private RectTransform[] menuItems;

        [Tooltip("菜单项之间的间距（世界单位）")]
        [SerializeField] private float itemSpacing = 1.5f;

        [Tooltip("展开/收起动画时长（秒）")]
        [SerializeField] private float animationDuration = 0.3f;

        [Header("══ 菜单栏 ══")]
        [Tooltip("菜单栏的 RectTransform（点击展开/收起）")]
        [SerializeField] private RectTransform menuBar;

        [Tooltip("展开时菜单栏的视觉变化颜色")]
        [SerializeField] private Color expandedColor = new Color(1f, 0.9f, 0.5f, 1f);

    [Header("══ 子项跟随 ══")]
    [Tooltip("子项跟随父级的平滑速度")]
    [SerializeField] private float followSpeed = 12f;

    [Header("══ 阶梯排列 ══")]
    [Tooltip("子项阶梯错位幅度，每次展开循环切换：右偏→居中→左偏→居中")]
    [SerializeField] private float staggerAmount = 1.5f;

    [Header("══ 上下文限制 ══")]
        [Tooltip("UIContextConstraint — 离开区域自动折叠")]
        [SerializeField] private UIContextConstraint contextConstraint;

        // 状态
        private bool isExpanded;
        private bool isAnimating;
        private int expandCycle = 1; // 1st展开=右, 2nd=中, 3rd=左, 4th=中...
        private Vector3[] collapsedPositions;  // 收起时各子项的位置（叠在父级上）
        private Vector3[] expandedPositions;   // 展开时各子项的位置（垂直排列）
        private Color originalMenuBarColor;
        private Image menuBarImage;
        private DraggableUI parentDraggable;
        private UIPhysicsElement parentPhysics;

        // 事件
        public System.Action OnExpanded;
        public System.Action OnCollapsed;

        public bool IsExpanded => isExpanded;

        #region Unity 生命周期

        private void Awake()
        {
            menuBarImage = menuBar != null ? menuBar.GetComponent<Image>() : null;
            if (menuBarImage != null)
                originalMenuBarColor = menuBarImage.color;

            parentDraggable = GetComponent<DraggableUI>();
            parentPhysics = GetComponent<UIPhysicsElement>();
            contextConstraint = GetComponent<UIContextConstraint>();

            // 存储收起位置
            collapsedPositions = new Vector3[menuItems.Length];
            expandedPositions = new Vector3[menuItems.Length];

            if (menuBar != null)
            {
                Vector3 basePos = menuBar.position;
                for (int i = 0; i < menuItems.Length; i++)
                {
                    if (menuItems[i] != null)
                    {
                        collapsedPositions[i] = basePos;
                        // 展开位置：从父级下方垂直排列
                        expandedPositions[i] = basePos + Vector3.down * itemSpacing * (i + 1);
                        // 初始设为收起
                        menuItems[i].position = collapsedPositions[i];
                        menuItems[i].gameObject.SetActive(false);
                    }
                }
            }

            // 订阅上下文限制
            if (contextConstraint != null)
            {
                contextConstraint.OnConstraintViolated += OnAreaViolated;
                contextConstraint.OnConstraintRestored += OnAreaRestored;
            }
        }

        private void Update()
        {
            if (!isExpanded || menuBar == null || isAnimating) return;

            for (int i = 0; i < menuItems.Length; i++)
            {
                if (menuItems[i] == null || !menuItems[i].gameObject.activeSelf) continue;

                float staggerX = GetStaggerX(i, menuItems.Length);
                Vector3 targetPos = menuBar.position + Vector3.down * itemSpacing * (i + 1) + Vector3.right * staggerX;
                menuItems[i].position = Vector3.Lerp(menuItems[i].position, targetPos, Time.deltaTime * followSpeed);
            }
        }

        private void OnDestroy()
        {
            if (contextConstraint != null)
            {
                contextConstraint.OnConstraintViolated -= OnAreaViolated;
                contextConstraint.OnConstraintRestored -= OnAreaRestored;
            }
        }

        #endregion

        #region IPointerClickHandler

        public void OnPointerClick(PointerEventData eventData)
        {
            // ★ 防止点击子项时触发父级的展开/收起
            // pointerPress 指向实际被点击的 GameObject
            if (eventData.pointerPress != gameObject && eventData.pointerPress != menuBar?.gameObject)
                return;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                ToggleMenu();
            }
        }

        #endregion

        #region 展开/收起

        public void ToggleMenu()
        {
            if (isAnimating) return;

            if (isExpanded)
                Collapse();
            else
                Expand();
        }

        public void Expand()
        {
            if (isExpanded || isAnimating) return;
            expandCycle = (expandCycle + 1) % 4; // 1中→2右→3中→0左→1中...
            isExpanded = true;
            StopAllCoroutines();
            StartCoroutine(AnimateExpand());
        }

        /// <summary>
        /// 根据展开次数计算水平错位
        /// </summary>
        private float GetStaggerX(int itemIndex, int totalCount)
        {
            if (totalCount <= 1) return 0;
            int cycle = expandCycle % 4;
            if (cycle == 1 || cycle == 3) return 0; // 居中
            float t = (itemIndex + 1) / (float)totalCount; // New 也动一点，Exit 动最多
            float sign = (cycle == 2) ? 1f : -1f; // 2=右, 0=左
            return t * staggerAmount * sign;
        }

        /// <summary>
        /// 收起菜单 — 子项缩回父级位置
        /// </summary>
        public void Collapse()
        {
            if (!isExpanded || isAnimating) return;
            isExpanded = false;
            StopAllCoroutines();
            StartCoroutine(AnimateCollapse());
        }

        private System.Collections.IEnumerator AnimateExpand()
        {
            isAnimating = true;

            // 展开时锁定菜单栏物理，防止下落干扰坐标
            if (parentPhysics != null) parentPhysics.SetKinematic(true);
            if (menuBarImage != null) menuBarImage.color = expandedColor;

            // ★ 每个子项从菜单栏背后（向上偏移）弹出到最终位置，不会叠在一起
            for (int i = 0; i < menuItems.Length; i++)
            {
                if (menuItems[i] == null) continue;
                var go = menuItems[i].gameObject;
                go.SetActive(true);

                // 起始位置：藏在菜单栏背后（向上 0.3 格）
                Vector3 startPos = menuBar.position + Vector3.up * 0.3f;
                // 目标位置：菜单栏下方展开 + 阶梯错位
                float staggerX = GetStaggerX(i, menuItems.Length);
                Vector3 targetPos = menuBar.position + Vector3.down * itemSpacing * (i + 1) + Vector3.right * staggerX;
                go.transform.position = startPos;

                var pe = go.GetComponent<UI.UIPhysicsElement>();
                if (pe == null) pe = go.AddComponent<UI.UIPhysicsElement>();
                pe.useGravity = false;
                pe.isPlatform = true;   // ★ 玩家可以踩
                pe.isKinematic = true;  // ★ 永久锁定，物理引擎不碰
                pe.SetVelocity(Vector2.zero);
                // ★ 不注册到 UIPhysicsManager — 彻底隔离，不会被 CheckCollisions 推飞

                var draggable = go.GetComponent<DragSystem.DraggableUI>();
                if (draggable != null) Destroy(draggable);
                if (go.GetComponent<BoxCollider2D>() == null)
                    go.AddComponent<BoxCollider2D>();

                // 存储动画目标
                expandedPositions[i] = targetPos;
                collapsedPositions[i] = startPos;
            }

            // 级联下拉动画
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                t = 1f - Mathf.Pow(1f - t, 3f); // 缓出

                for (int i = 0; i < menuItems.Length; i++)
                {
                    if (menuItems[i] == null) continue;
                    float delay = i * 0.06f;
                    float itemT = Mathf.Clamp01((t - delay) / Mathf.Max(1f - delay, 0.01f));
                    menuItems[i].position = Vector3.Lerp(collapsedPositions[i], expandedPositions[i], itemT);
                }
                yield return null;
            }

            // 确保最终位置
            for (int i = 0; i < menuItems.Length; i++)
            {
                if (menuItems[i] != null)
                    menuItems[i].position = expandedPositions[i];
            }

            // 等待一帧让位置稳定
            yield return null;

            // ★ 子项永久锁定物理，只由 Lerp 控制位置
            // 不释放子项 — 否则 UIPhysicsElement 和 Lerp 打架导致飞走
            for (int i = 0; i < menuItems.Length; i++)
            {
                if (menuItems[i] != null)
                {
                    var pe = menuItems[i].GetComponent<UI.UIPhysicsElement>();
                    if (pe != null)
                    {
                        pe.SetKinematic(true);
                        pe.useGravity = false;
                        pe.SetVelocity(Vector2.zero);
                    }
                }
            }

            // 只释放菜单栏（父级）的物理
            if (parentPhysics != null) parentPhysics.SetKinematic(false);

            isAnimating = false;
            OnExpanded?.Invoke();
            Debug.Log("[DropdownMenu] 菜单已展开");
        }

        private System.Collections.IEnumerator AnimateCollapse()
        {
            isAnimating = true;

            // 收起前：禁用子项的拖拽和物理，防止它们自己飞走
            for (int i = 0; i < menuItems.Length; i++)
            {
                if (menuItems[i] != null)
                {
                    var go = menuItems[i].gameObject;
                    var draggable = go.GetComponent<DragSystem.DraggableUI>();
                    if (draggable != null) draggable.SetDraggable(false);
                    var pe = go.GetComponent<UI.UIPhysicsElement>();
                    if (pe != null) pe.SetVelocity(Vector2.zero);
                }
            }

            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                t = t * t; // 加速缩回

                for (int i = 0; i < menuItems.Length; i++)
                {
                    if (menuItems[i] != null)
                        menuItems[i].position = Vector3.Lerp(expandedPositions[i], collapsedPositions[i], t);
                }

                yield return null;
            }

            // 停用子项
            for (int i = 0; i < menuItems.Length; i++)
            {
                if (menuItems[i] != null)
                {
                    menuItems[i].position = collapsedPositions[i];
                    menuItems[i].gameObject.SetActive(false);
                }
            }

            // 恢复颜色
            if (menuBarImage != null)
                menuBarImage.color = originalMenuBarColor;

            isAnimating = false;
            OnCollapsed?.Invoke();
            Debug.Log("[DropdownMenu] 菜单已收起");
        }

        #endregion

        #region 上下文限制回调

        private void OnAreaViolated(UIContextConstraint constraint)
        {
            // 离开有效区域 → 自动折叠
            if (isExpanded)
            {
                Collapse();
                Debug.Log("[DropdownMenu] 离开有效区域，菜单自动折叠");
            }
        }

        private void OnAreaRestored(UIContextConstraint constraint)
        {
            // 回到有效区域 — 不自动展开，等玩家再次点击
            Debug.Log("[DropdownMenu] 回到有效区域");
        }

        #endregion

        #region 编辑器辅助

        /// <summary>
        /// 自动收集子菜单项（编辑器中使用）
        /// </summary>
        [ContextMenu("自动收集子菜单项")]
        private void AutoCollectMenuItems()
        {
            var items = new System.Collections.Generic.List<RectTransform>();
            foreach (Transform child in transform)
            {
                // 跳过菜单栏本身
                if (menuBar != null && child == menuBar.transform) continue;

                var rt = child as RectTransform;
                if (rt != null)
                    items.Add(rt);
            }
            menuItems = items.ToArray();
            Debug.Log($"[DropdownMenu] 自动收集到 {menuItems.Length} 个子菜单项");
        }

        #endregion
    }
}
