using UnityEngine;

namespace OutOfBounds.Data
{
    /// <summary>
    /// 物理设置数据
    /// 可在编辑器中配置，运行时读取
    /// </summary>
    [CreateAssetMenu(fileName = "PhysicsSettings", menuName = "OutOfBounds/Physics Settings")]
    public class PhysicsSettingsData : ScriptableObject
    {
        [Header("全局重力")]
        [Tooltip("默认重力值")]
        public float defaultGravity = -30f;
        
        [Tooltip("最小重力")]
        public float minGravity = -5f;
        
        [Tooltip("最大重力")]
        public float maxGravity = -60f;

        [Header("UI物理")]
        [Tooltip("UI元素重力倍率")]
        public float uiGravityScale = 0.5f;
        
        [Tooltip("UI元素弹性")]
        [Range(0f, 1f)]
        public float uiBounciness = 0.2f;
        
        [Tooltip("UI元素摩擦")]
        [Range(0f, 1f)]
        public float uiFriction = 0.3f;
        
        [Tooltip("UI元素阻力")]
        public float uiDrag = 1.5f;

        [Header("性能")]
        [Tooltip("物理计算子步数（越高越精确但性能消耗越大）")]
        [Range(1, 10)]
        public int physicsSubSteps = 4;
        
        [Tooltip("最大速度限制")]
        public float maxVelocity = 50f;

        /// <summary>
        /// 获取UI物理重力值
        /// </summary>
        public float GetUIGravity(float globalGravity)
        {
            return globalGravity * uiGravityScale;
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        public static PhysicsSettingsData CreateDefault()
        {
            var data = CreateInstance<PhysicsSettingsData>();
            data.defaultGravity = -30f;
            data.minGravity = -5f;
            data.maxGravity = -60f;
            data.uiGravityScale = 0.5f;
            data.uiBounciness = 0.2f;
            data.uiFriction = 0.3f;
            data.uiDrag = 1.5f;
            data.physicsSubSteps = 4;
            data.maxVelocity = 50f;
            return data;
        }
    }
}
