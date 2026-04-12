using UnityEngine;
using OutOfBounds.Core;
using OutOfBounds.UI;

namespace OutOfBounds.Physics
{
    /// <summary>
    /// 地刺陷阱组件
    /// 检测玩家碰撞并扣血
    /// </summary>
    public class SpikeTrap : MonoBehaviour
    {
        [Header("陷阱设置")]
        [SerializeField] private int damage = 1;
        [SerializeField] private float damageCooldown = 1f;
        [SerializeField] private bool isActive = true;

        [Header("视觉效果")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color activeColor = Color.red;
        [SerializeField] private Color inactiveColor = Color.gray;

        // 状态
        private float lastDamageTime;
        private bool canDamage = true;

        #region Unity 生命周期

        private void Awake()
        {
            // 初始化
            UpdateVisualState();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // 检测玩家碰撞
            if (isActive && canDamage && collision.CompareTag("Player"))
            {
                // 扣血
                DealDamage();

                // 触发伤害冷却
                StartDamageCooldown();
            }
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            // 检测玩家持续碰撞
            if (isActive && canDamage && collision.CompareTag("Player"))
            {
                // 扣血
                DealDamage();

                // 触发伤害冷却
                StartDamageCooldown();
            }
        }

        #endregion

        #region 伤害逻辑

        /// <summary>
        /// 处理伤害
        /// </summary>
        private void DealDamage()
        {
            // 通过HealthBarManager扣血
            if (HealthBarManager.Instance != null)
            {
                HealthBarManager.Instance.TakeDamage(damage);
            }

            // 触发玩家受伤事件
            Events.OnPlayerDamaged.Invoke();

            Debug.Log("玩家触碰到地刺，扣除" + damage + "点生命值");
        }

        /// <summary>
        /// 开始伤害冷却
        /// </summary>
        private void StartDamageCooldown()
        {
            canDamage = false;
            lastDamageTime = Time.time;

            // 重置冷却
            Invoke("ResetDamageCooldown", damageCooldown);
        }

        /// <summary>
        /// 重置伤害冷却
        /// </summary>
        private void ResetDamageCooldown()
        {
            canDamage = true;
        }

        #endregion

        #region 状态管理

        /// <summary>
        /// 设置陷阱是否激活
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;
            UpdateVisualState();
        }

        /// <summary>
        /// 切换陷阱激活状态
        /// </summary>
        public void ToggleActive()
        {
            isActive = !isActive;
            UpdateVisualState();
        }

        /// <summary>
        /// 更新视觉状态
        /// </summary>
        private void UpdateVisualState()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = isActive ? activeColor : inactiveColor;
            }
        }

        #endregion

        #region 公共属性

        public bool IsActive => isActive;
        public int Damage => damage;
        public float DamageCooldown => damageCooldown;

        #endregion
    }
}
