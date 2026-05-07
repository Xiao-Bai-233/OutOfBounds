using UnityEngine;
using UnityEngine.UI;
using OutOfBounds.Core;

namespace OutOfBounds.UI
{
    /// <summary>
    /// 关卡选择按钮（极简版 — 零外部图片资产）
    ///
    /// 视觉状态全靠 文字 + Button.colorBlock 实现：
    ///   🔒 锁定中：灰色 + 不可点击
    ///   ✓ 已完成：绿色 tint
    ///   正常已解锁：白色
    ///
    /// 需要的美术资源：零。
    /// 只需一个 Unity 内置 Button (Legacy) + 一个 Text 子物体。
    /// </summary>
    public class LevelButton : MonoBehaviour
    {
        [Header("组件绑定")]
        [Tooltip("按钮主文本（显示关卡编号+名称+状态符号）")]
        [SerializeField] private Text labelText;

        [Tooltip("按钮组件（用它的 colorBlock 控制颜色）")]
        [SerializeField] private Button button;

        [Header("颜色配置")]
        [Tooltip("锁定状态的颜色乘数")]
        [SerializeField] private Color lockedMultiplier = new Color(0.5f, 0.5f, 0.5f);
        [Tooltip("完成状态的颜色乘数")]
        [SerializeField] private Color completedMultiplier = new Color(0.3f, 0.8f, 0.3f);

        // ─── 文字符号（用纯文本代替图片） ─────────────────────
        private const string LOCK_SYMBOL = "[锁]";
        private const string DONE_SYMBOL = "[✓]";

        // 内部状态
        private LevelDefinition levelDef;
        private System.Action<LevelDefinition> onClickCallback;
        private ColorBlock originalColors;

        #region 初始化

        /// <summary>
        /// 初始化关卡按钮
        /// </summary>
        public void Initialize(
            LevelDefinition definition,
            System.Action<LevelDefinition> callback,
            bool unlocked,
            bool completed)
        {
            levelDef = definition;
            onClickCallback = callback;

            // 保存按钮原始颜色，以便恢复
            if (button != null)
                originalColors = button.colors;

            // 更新显示
            UpdateLabel(unlocked, completed);
            UpdateColor(unlocked, completed);

            // 点击事件
            if (button != null)
            {
                button.interactable = unlocked;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClicked);
            }
        }

        #endregion

        #region UI 更新

        private void UpdateLabel(bool unlocked, bool completed)
        {
            if (labelText == null || levelDef == null) return;

            string line1;  // 第一行：状态符号 + 编号
            string line2 = levelDef.levelName; // 第二行：关卡名

            if (!unlocked)
            {
                // 🔒 锁定
                string num = levelDef.isTutorial ? "" : $"{levelDef.levelIndex}";
                line1 = $"{LOCK_SYMBOL} {num}";
            }
            else if (completed)
            {
                // ✓ 已完成
                string num = levelDef.isTutorial ? "" : $"{levelDef.levelIndex}";
                line1 = $"{DONE_SYMBOL} {num}";
            }
            else
            {
                // 已解锁但未完成
                line1 = levelDef.isTutorial ? "教程" : $"{levelDef.levelIndex}";
            }

            labelText.text = $"{line1}\n{line2}";
            labelText.alignment = TextAnchor.MiddleCenter;
        }

        private void UpdateColor(bool unlocked, bool completed)
        {
            if (button == null) return;

            ColorBlock colors = originalColors;

            if (!unlocked)
            {
                // 锁定：所有颜色乘以灰色系数
                colors.colorMultiplier = 1f;
                colors.normalColor = Color.gray * lockedMultiplier;
                colors.disabledColor = Color.gray * lockedMultiplier;
            }
            else if (completed)
            {
                // 已完成：绿色 tint
                colors.colorMultiplier = 1f;
                colors.normalColor = completedMultiplier;
                colors.highlightedColor = completedMultiplier * 1.2f;
                colors.pressedColor = completedMultiplier * 0.8f;
            }
            else
            {
                // 默认：使用 Unity Button 原始颜色（白色/浅灰）
                colors.colorMultiplier = 1f;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.9f, 0.9f, 1f);
                colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            }

            button.colors = colors;
        }

        #endregion

        #region 点击

        private void OnClicked()
        {
            if (!button.interactable) return;
            onClickCallback?.Invoke(levelDef);
        }

        #endregion
    }
}
