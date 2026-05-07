using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using OutOfBounds.UI;

namespace OutOfBounds.Physics
{
    /// <summary>
    /// 毒水坑组件
    /// 玩家进入 → 传送到存档点
    /// 心形 UI 元素进入 → 逐渐腐蚀缩小，作为临时踏点
    /// </summary>
    public class PoisonPit : MonoBehaviour
    {
        [Header("══ 毒水坑 ══")]
        [Tooltip("每次进入扣血量")]
        [SerializeField] private int damage = 1;
        [Tooltip("伤害冷却（秒）")]
        [SerializeField] private float damageCooldown = 1f;

        [Header("══ 腐蚀（Phase 3 新增） ══")]
        [Tooltip("心形落入毒水后的腐蚀时间（秒）— 给玩家足够时间通过")]
        [SerializeField] private float corrosionDuration = 8f;
        [Tooltip("腐蚀时每秒缩小的比例")]
        [SerializeField] private float corrosionShrinkRate = 0.06f;
        [Tooltip("腐蚀时透明度下降速度")]
        [SerializeField] private float corrosionAlphaRate = 0.1f;
        [Tooltip("腐蚀时的抖动强度")]
        [SerializeField] private float corrosionWobbleStrength = 1.5f;

        [Header("══ 视觉 ══")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color poisonColor = Color.green;

        [Header("══ 碰撞 ══")]
        [SerializeField] private Collider2D pitCollider;

        private float lastDamageTime;
        private Dictionary<UIPhysicsElement, CorrosionState> corrodingElements = new Dictionary<UIPhysicsElement, CorrosionState>();
        private HashSet<UIPhysicsElement> elementsInPit = new HashSet<UIPhysicsElement>();
        private ContactFilter2D overlapFilter;

        private class CorrosionState
        {
            public float elapsed;
            public float duration;
            public Vector3 originalScale;
            public Color originalColor;
            public CanvasGroup canvasGroup;
            public Coroutine routine;
        }

        #region Unity 生命周期

        private void Awake()
        {
            if (pitCollider == null)
                pitCollider = GetComponent<Collider2D>();

            // ★ Trigger 模式：OverlapCollider 仍能检测，但 BoxCast 不会把它当实体墙
            // 这样心形不会被弹开，腐蚀才能持续到 8 秒
            if (pitCollider != null)
                pitCollider.isTrigger = true;

            overlapFilter = new ContactFilter2D();
            overlapFilter.useTriggers = true;
            overlapFilter.NoFilter();
        }

        private void Update()
        {
            // 手动检测心形是否在毒水坑内（UIPhysicsElement 无 Rigidbody2D，不能用 OnTriggerEnter）
            DetectHeartOverlaps();
        }

        /// <summary>
        /// 手动检测哪些 UIPhysicsElement 与毒水坑重叠
        /// </summary>
        private void DetectHeartOverlaps()
        {
            if (pitCollider == null) return;

            // 用 OverlapCollider 检测所有重叠的碰撞体
            var results = new Collider2D[32];
            int count = Physics2D.OverlapCollider(pitCollider, overlapFilter, results);

            var currentInPit = new HashSet<UIPhysicsElement>();

            for (int i = 0; i < count; i++)
            {
                if (results[i] == null) continue;

                // 玩家检测
                if (results[i].CompareTag("Player"))
                {
                    var player = results[i].GetComponent<OutOfBounds.Player.PlayerController>();
                    if (player != null && Time.time - lastDamageTime > damageCooldown)
                    {
                        lastDamageTime = Time.time;
                        player.RespawnAtCheckpoint();
                        Debug.Log("[PoisonPit] 玩家掉入毒水坑，传送到存档点");
                    }
                    continue;
                }

                // 心形 UI 元素检测
                var physicsElement = results[i].GetComponent<UIPhysicsElement>();
                if (physicsElement != null)
                {
                    currentInPit.Add(physicsElement);
                    // 首次进入时打印确认
                    if (!elementsInPit.Contains(physicsElement) && !corrodingElements.ContainsKey(physicsElement))
                    {
                        Debug.Log($"[PoisonPit] 🔍 检测到心形 {physicsElement.name} 进入毒水坑，即将开始腐蚀");
                    }
                }
            }

            // 比对上一帧：新进入的 → 开始腐蚀
            foreach (var element in currentInPit)
            {
                if (!elementsInPit.Contains(element) && !corrodingElements.ContainsKey(element))
                {
                    StartCorrosion(element);
                }
            }

            // 离开的 → 暂停腐蚀
            foreach (var element in elementsInPit)
            {
                if (!currentInPit.Contains(element) && corrodingElements.ContainsKey(element))
                {
                    PauseCorrosion(element);
                }
            }

            elementsInPit = currentInPit;
        }

        #endregion

        #region 腐蚀逻辑

        /// <summary>
        /// 开始腐蚀某个心形元素
        /// </summary>
        private void StartCorrosion(UIPhysicsElement element)
        {
            var state = new CorrosionState
            {
                elapsed = 0f,
                duration = corrosionDuration,
                originalScale = element.RectTransform != null ? element.RectTransform.localScale : Vector3.one
            };

            // 获取或创建 CanvasGroup（用于控制透明度）
            state.canvasGroup = element.GetComponent<CanvasGroup>();
            if (state.canvasGroup == null)
                state.canvasGroup = element.gameObject.AddComponent<CanvasGroup>();

            // 缓存原色
            var images = element.GetComponentsInChildren<UnityEngine.UI.Image>();
            if (images.Length > 0)
                state.originalColor = images[0].color;

            corrodingElements[element] = state;
            state.routine = StartCoroutine(CorrosionRoutine(element, state));

            Debug.Log($"[PoisonPit] {element.name} 开始腐蚀，{corrosionDuration}秒后消失");
        }

        /// <summary>
        /// 暂停腐蚀（心形被拖离毒水）
        /// </summary>
        private void PauseCorrosion(UIPhysicsElement element)
        {
            if (corrodingElements.TryGetValue(element, out var state))
            {
                if (state.routine != null)
                    StopCoroutine(state.routine);
                corrodingElements.Remove(element);
                Debug.Log($"[PoisonPit] {element.name} 离开毒水，腐蚀暂停");
            }
        }

        /// <summary>
        /// 腐蚀协程 — 缩小 + 变透明 + 抖动 + 最终回收
        /// </summary>
        private IEnumerator CorrosionRoutine(UIPhysicsElement element, CorrosionState state)
        {
            Debug.Log($"[PoisonPit] 🟢 {element.name} 腐蚀开始，持续 {state.duration} 秒，当前 scale={state.originalScale}");
            
            var images = element.GetComponentsInChildren<UnityEngine.UI.Image>();
            RectTransform rect = element.RectTransform;

            // 等待 0.5 秒后再开始腐蚀，防止松手瞬间速度导致的跳动被误认为消失
            yield return new WaitForSeconds(0.5f);

            while (state.elapsed < state.duration)
            {
                state.elapsed += Time.deltaTime;
                float progress = state.elapsed / state.duration;

                // 缩放：从 1 → corrosionShrinkRate（默认 0.06，关联腐蚀速度）
                if (rect != null)
                {
                    float t = Mathf.Clamp01(progress * 2f); // 前 4 秒缩到最小
                    float scale = Mathf.Lerp(1f, 0.3f, t);
                    rect.localScale = state.originalScale * scale;
                }

                // 透明
                if (state.canvasGroup != null)
                {
                    float t = Mathf.Clamp01(progress * 1.5f);
                    state.canvasGroup.alpha = Mathf.Lerp(1f, 0.2f, t);
                }

                // 变色（绿色毒化）
                foreach (var img in images)
                {
                    if (img != null)
                        img.color = Color.Lerp(state.originalColor, poisonColor, progress);
                }

                // 微小抖动
                float wobble = Mathf.Sin(Time.time * 8f) * corrosionWobbleStrength * 0.3f * (1f - progress);
                if (rect != null)
                    rect.localPosition += new Vector3(wobble, wobble * 0.2f, 0);

                // 每秒输出一次进度
                if (Mathf.FloorToInt(state.elapsed) > Mathf.FloorToInt(state.elapsed - Time.deltaTime))
                {
                    Debug.Log($"[PoisonPit] {element.name} 腐蚀进度: {(progress*100):F0}%");
                }

                yield return null;
            }

            // 腐蚀完成 → 回收
            Debug.Log($"[PoisonPit] 🔴 {element.name} 腐蚀完成，回收中");
            corrodingElements.Remove(element);
            if (UIPhysicsManager.Instance != null)
                UIPhysicsManager.Instance.RecycleHeartElement(element);
            else
                Destroy(element.gameObject);
        }

        #endregion

        #region 状态管理

        public void ResetPit()
        {
            lastDamageTime = 0f;

            // 停止所有腐蚀协程
            foreach (var kvp in corrodingElements)
            {
                if (kvp.Value.routine != null)
                    StopCoroutine(kvp.Value.routine);
            }
            corrodingElements.Clear();
            Debug.Log("[PoisonPit] 毒水坑已重置");
        }

        /// <summary>
        /// 检查某个心形是否正在被腐蚀
        /// </summary>
        public bool IsCorroding(UIPhysicsElement element)
        {
            return corrodingElements.ContainsKey(element);
        }

        /// <summary>
        /// 获取腐蚀进度 (0~1)
        /// </summary>
        public float GetCorrosionProgress(UIPhysicsElement element)
        {
            if (corrodingElements.TryGetValue(element, out var state))
                return state.elapsed / state.duration;
            return 0f;
        }

        #endregion

        #region 公共属性

        public int Damage => damage;
        public float DamageCooldown => damageCooldown;
        public float CorrosionDuration => corrosionDuration;

        #endregion
    }
}
