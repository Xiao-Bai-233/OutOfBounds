using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 重力滑块 - 设置菜单中的滑块控制全局重力
/// </summary>
[RequireComponent(typeof(Slider))]
public class GravitySlider : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private bool syncOnStart = true;
    [SerializeField] private bool showValueText = true;
    [SerializeField] private Text valueText;

    private Slider slider;
    private GlobalPhysicsSettings physicsSettings;

    #region Unity 生命周期

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    private void Start()
    {
        // 获取物理设置
        physicsSettings = GlobalPhysicsSettings.Instance;

        // 同步初始值
        if (syncOnStart && physicsSettings != null)
        {
            SyncWithGlobalSettings();
        }

        // 添加监听
        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    #endregion

    #region 同步

    /// <summary>
    /// 与全局物理设置同步
    /// </summary>
    public void SyncWithGlobalSettings()
    {
        if (physicsSettings == null) return;

        // 将当前重力值映射到滑块范围
        float currentGravity = physicsSettings.CurrentGravity;
        float normalizedValue = Mathf.InverseLerp(
            physicsSettings.MinGravity,
            physicsSettings.MaxGravity,
            currentGravity
        );

        slider.value = normalizedValue;
        UpdateValueText(normalizedValue);
    }

    #endregion

    #region 回调

    private void OnSliderValueChanged(float normalizedValue)
    {
        if (physicsSettings == null) return;

        // 应用重力变化
        physicsSettings.SetGravityMultiplier(normalizedValue);

        // 更新显示文本
        UpdateValueText(normalizedValue);

        // 触发变化事件（可用于其他系统）
        OnGravityChanged?.Invoke(normalizedValue);
    }

    private void UpdateValueText(float normalizedValue)
    {
        if (!showValueText || valueText == null || physicsSettings == null) return;

        float currentGravity = Mathf.Lerp(
            physicsSettings.MinGravity,
            physicsSettings.MaxGravity,
            normalizedValue
        );

        valueText.text = $"{currentGravity:F1}";
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 重置为默认值
    /// </summary>
    public void ResetToDefault()
    {
        if (physicsSettings == null) return;

        physicsSettings.ResetToDefaultGravity();
        SyncWithGlobalSettings();
    }

    #endregion

    #region 事件

    public System.Action<float> OnGravityChanged;

    #endregion

    #region 公共属性

    public float Value => slider.value;
    public float NormalizedValue => slider.normalizedValue;

    #endregion
}
