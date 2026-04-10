using UnityEngine;

namespace OutOfBounds.Data
{
    /// <summary>
    /// 玩家设置数据
    /// 可在编辑器中配置玩家属性
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerSettings", menuName = "OutOfBounds/Player Settings")]
    public class PlayerSettingsData : ScriptableObject
    {
        [Header("移动")]
        [Tooltip("移动速度")]
        public float moveSpeed = 5f;
        
        [Tooltip("跳跃力度")]
        public float jumpForce = 10f;
        
        [Tooltip("空中控制系数")]
        [Range(0f, 1f)]
        public float airControl = 0.5f;

        [Header("生命值")]
        [Tooltip("最大生命值")]
        public int maxHealth = 3;
        
        [Tooltip("受伤后的无敌时间（秒）")]
        public float invincibleTime = 1f;

        [Header("物理")]
        [Tooltip("玩家质量")]
        public float mass = 1f;
        
        [Tooltip("线性阻力")]
        public float drag = 0.5f;

        [Header("交互")]
        [Tooltip("与UI交互的距离")]
        public float interactionRange = 2f;

        /// <summary>
        /// 创建默认配置
        /// </summary>
        public static PlayerSettingsData CreateDefault()
        {
            var data = CreateInstance<PlayerSettingsData>();
            data.moveSpeed = 5f;
            data.jumpForce = 10f;
            data.airControl = 0.5f;
            data.maxHealth = 3;
            data.invincibleTime = 1f;
            data.mass = 1f;
            data.drag = 0.5f;
            data.interactionRange = 2f;
            return data;
        }
    }
}
