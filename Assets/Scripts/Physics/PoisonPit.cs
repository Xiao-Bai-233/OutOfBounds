using UnityEngine;
using System.Collections.Generic;
using OutOfBounds.UI;
using OutOfBounds.Player;

namespace OutOfBounds.Physics
{
    /// <summary>
    /// 毒水坑组件
    /// 玩家进入时扣血，心形元素作为垫脚石
    /// </summary>
    public class PoisonPit : MonoBehaviour
    {
        [Header("毒水坑设置")]
        [SerializeField] private int damage = 1; // 每次进入毒水坑扣血
        [SerializeField] private float damageCooldown = 1f; // 伤害冷却时间

        [Header("视觉效果")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color poisonColor = Color.green;

        [Header("碰撞设置")]
        [SerializeField] private Collider2D pitCollider;

        // 伤害冷却计时器
        private float lastDamageTime;

        #region Unity 生命周期

        private void Awake()
        {
            // 初始化
            if (pitCollider != null)
            {
                // 始终保持为触发器
                pitCollider.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // 检测玩家进入
            if (collision.CompareTag("Player"))
            {
                // 玩家进入毒水坑不再扣血，而是传送
                PlayerController player = collision.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.RespawnAtCheckpoint();
                    Debug.Log("玩家掉入毒水坑，传送到存档点");
                }
            }
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            // 同上
            if (collision.CompareTag("Player"))
            {
                PlayerController player = collision.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.RespawnAtCheckpoint();
                }
            }
        }

        #endregion

        #region 状态管理

        /// <summary>
        /// 重置毒水坑
        /// </summary>
        public void ResetPit()
        {
            lastDamageTime = 0f;
            Debug.Log("毒水坑已重置");
        }

        #endregion

        #region 公共属性

        public int Damage => damage;
        public float DamageCooldown => damageCooldown;

        #endregion
    }
}
