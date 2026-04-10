using UnityEngine;
using OutOfBounds.Core;
using OutOfBounds.Data;

namespace OutOfBounds.Physics
{
/// <summary>
/// 全局物理设置管理器
/// 控制游戏中的重力和其他物理参数
/// </summary>
public class GlobalPhysicsSettings : MonoBehaviour
{
    [Header("数据配置")]
    [Tooltip("物理设置数据，如未指定则使用默认值")]
    [SerializeField] private PhysicsSettingsData settingsData;
    public static GlobalPhysicsSettings Instance { get; private set; }

    [Header("重力设置")]
    [SerializeField] private float defaultGravity = -30f;
    [SerializeField] private float minGravity = -5f;
    [SerializeField] private float maxGravity = -60f;
    [SerializeField] [Range(0f, 1f)] private float currentGravityMultiplier = 1f;

    [Header("UI物理设置")]
    [SerializeField] private float uiPhysicsGravity = -15f;
    [SerializeField] private float uiPhysicsBounce = 0.3f;
    [SerializeField] private float uiPhysicsFriction = 0.5f;

    // 事件
    public System.Action<float> OnGravityChanged;

    #region Unity 生命周期

    private void Awake()
    {
        // 单例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 应用默认重力
        ApplyGravity(defaultGravity);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region 重力控制

    /// <summary>
    /// 设置重力倍增值（0-1范围）
    /// 用于设置菜单的滑块
    /// </summary>
    public void SetGravityMultiplier(float multiplier)
    {
        currentGravityMultiplier = Mathf.Clamp01(multiplier);
        float newGravity = Mathf.Lerp(minGravity, maxGravity, currentGravityMultiplier);
        ApplyGravity(newGravity);
    }

    /// <summary>
    /// 直接设置重力值
    /// </summary>
    public void SetGravity(float gravity)
    {
        gravity = Mathf.Clamp(gravity, minGravity, maxGravity);
        ApplyGravity(gravity);
    }

    /// <summary>
    /// 重置为默认重力
    /// </summary>
    public void ResetToDefaultGravity()
    {
        SetGravityMultiplier(1f);
    }

    private void ApplyGravity(float gravity)
    {
        Physics2D.gravity = new Vector2(0, gravity);
        OnGravityChanged?.Invoke(gravity);
    }

    #endregion

    #region UI物理设置

    /// <summary>
    /// 获取UI物理的重力值（通常比主角轻）
    /// </summary>
    public float GetUIPhysicsGravity()
    {
        // UI元素的重力也受全局重力影响
        return uiPhysicsGravity * currentGravityMultiplier;
    }

    /// <summary>
    /// 获取UI物理的弹性系数
    /// </summary>
    public float GetUIPhysicsBounce() => uiPhysicsBounce;

    /// <summary>
    /// 获取UI物理的摩擦系数
    /// </summary>
    public float GetUIPhysicsFriction() => uiPhysicsFriction;

    #endregion

    #region 公共属性

    /// <summary>
    /// 当前重力值
    /// </summary>
    public float CurrentGravity => Physics2D.gravity.y;

    /// <summary>
    /// 当前重力倍增值
    /// </summary>
    public float CurrentGravityMultiplier => currentGravityMultiplier;

    /// <summary>
    /// 默认重力值
    /// </summary>
    public float DefaultGravity => defaultGravity;

    /// <summary>
    /// 最小重力值
    /// </summary>
    public float MinGravity => minGravity;

    /// <summary>
    /// 最大重力值
    /// </summary>
    public float MaxGravity => maxGravity;

    #endregion

    #region 编辑器支持

    /// <summary>
    /// 在编辑器中实时预览重力变化
    /// </summary>
    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && Instance == this)
        {
            float newGravity = Mathf.Lerp(minGravity, maxGravity, currentGravityMultiplier);
            ApplyGravity(newGravity);
        }
    }
    #endif

    #endregion
}
}
