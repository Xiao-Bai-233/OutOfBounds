using UnityEngine;
using OutOfBounds.Core;

namespace OutOfBounds.Data
{
    /// <summary>
    /// UI 类型配置文件 (ScriptableObject)
    /// 策略模式：不同 UI 类型通过此配置获得不同的物理参数和谜题行为
    /// 在编辑器中创建: Assets → Create → OutOfBounds → UI Type Profile
    /// </summary>
    [CreateAssetMenu(fileName = "UITypeProfile", menuName = "OutOfBounds/UI 类型配置")]
    public class UITypeProfile : ScriptableObject
    {
        [Header("══ 类型标识 ══")]
        [Tooltip("此配置对应的 UI 类型（文本/窗口/血条/气泡/菜单/复选框）")]
        public UIType uiType;

        [Header("══ 物理参数 ══")]
        [Tooltip("质量 — 越大越重，压机关/碰撞更有力")]
        [Range(0.1f, 10f)]
        public float mass = 1f;

        [Tooltip("阻力 — 影响水平速度衰减，越大停得越快")]
        [Range(0f, 5f)]
        public float drag = 1.5f;

        [Tooltip("弹性 — 碰撞反弹力度，0=不弹，1=完全反弹")]
        [Range(0f, 1f)]
        public float bounciness = 0.2f;

        [Tooltip("摩擦 — 碰撞时速度损失，越大越黏")]
        [Range(0f, 1f)]
        public float friction = 0.3f;

        [Tooltip("重力倍率 — 1=正常，0.5=轻飘飘，2=很重")]
        [Range(0f, 3f)]
        public float gravityScale = 1f;

        [Header("══ 拖拽参数 ══")]
        [Tooltip("松手惯性倍率 — 松手时甩出速度的放大倍数")]
        [Range(0.5f, 20f)]
        public float releaseVelocityMultiplier = 5f;

        [Tooltip("跟手速度 — 越大拖拽时越紧贴鼠标")]
        [Range(5f, 50f)]
        public float dragSpeed = 15f;

        [Header("══ 行为开关 ══")]
        [Tooltip("是否可作为平台让玩家踩上去")]
        public bool isPlatform = true;

        [Tooltip("是否对上下文敏感 — 离开有效区域触发违规行为")]
        public bool contextSensitive = false;

        [Tooltip("是否默认可拖拽")]
        public bool canBeDragged = true;

        [Tooltip("是否受重力影响")]
        public bool useGravity = true;

        [Header("══ 违规后行为 ══")]
        [Tooltip("离开有效范围后触发什么效果")]
        public ConstraintViolationBehavior violationBehavior = ConstraintViolationBehavior.BecomeBroken;

        [Header("══ 视觉 ══")]
        [Tooltip("违规后变色")]
        public Color detachedColor = new Color(1f, 0.3f, 0.3f, 0.5f);

        [Tooltip("违规后透明度")]
        [Range(0f, 1f)]
        public float detachedAlpha = 0.3f;

        [Tooltip("回收动画时长（秒）")]
        [Range(0.1f, 3f)]
        public float recycleDuration = 0.5f;

        [Header("══ 文本专属 ══")]
        [Tooltip("文字乱码时显示的符号")]
        public string brokenGlyphText = "░▒▓█";

        [Header("══ 窗口专属 ══")]
        [Tooltip("窗口最小尺寸")]
        public Vector2 windowMinSize = new Vector2(200f, 150f);

        [Header("══ 血条专属 ══")]
        [Tooltip("心形被毒水腐蚀的持续时间（秒）")]
        [Range(1f, 30f)]
        public float corrosionDuration = 8f;

        [Header("══ 气泡专属 ══")]
        [Tooltip("每个字符增加的宽度")]
        [Range(1f, 20f)]
        public float characterWidth = 8f;

        /// <summary>
        /// 创建默认配置实例
        /// 各类型在编辑器中通过 CreateAssetMenu 创建 .asset 文件
        /// </summary>
        public static UITypeProfile CreateDefault(UIType type)
        {
            var profile = CreateInstance<UITypeProfile>();
            profile.uiType = type;

            switch (type)
            {
                case UIType.Text:
                    profile.mass = 0.3f;
                    profile.drag = 1f;
                    profile.bounciness = 0.1f;
                    profile.friction = 0.5f;
                    profile.gravityScale = 0.8f;
                    profile.releaseVelocityMultiplier = 4f;
                    profile.dragSpeed = 18f;
                    profile.isPlatform = true;
                    profile.contextSensitive = true;
                    profile.violationBehavior = ConstraintViolationBehavior.BecomeBroken;
                    profile.brokenGlyphText = "░▒▓█";
                    break;

                case UIType.Window:
                    profile.mass = 2f;
                    profile.drag = 2f;
                    profile.bounciness = 0.1f;
                    profile.friction = 0.6f;
                    profile.gravityScale = 1.2f;
                    profile.releaseVelocityMultiplier = 3f;
                    profile.dragSpeed = 10f;
                    profile.isPlatform = true;
                    profile.contextSensitive = true;
                    profile.violationBehavior = ConstraintViolationBehavior.BecomeTransparent;
                    profile.recycleDuration = 0.8f;
                    break;

                case UIType.HealthBar:
                    profile.mass = 0.5f;
                    profile.drag = 1.5f;
                    profile.bounciness = 0.3f;
                    profile.friction = 0.3f;
                    profile.gravityScale = 1f;
                    profile.releaseVelocityMultiplier = 5f;
                    profile.dragSpeed = 15f;
                    profile.isPlatform = true;
                    profile.contextSensitive = true;
                    profile.violationBehavior = ConstraintViolationBehavior.Recycle;
                    profile.corrosionDuration = 8f;
                    break;

                case UIType.DialogBubble:
                    profile.mass = 0.5f;
                    profile.drag = 1.2f;
                    profile.bounciness = 0.2f;
                    profile.friction = 0.4f;
                    profile.gravityScale = 0.9f;
                    profile.releaseVelocityMultiplier = 4f;
                    profile.dragSpeed = 14f;
                    profile.isPlatform = true;
                    profile.contextSensitive = true;
                    profile.violationBehavior = ConstraintViolationBehavior.Recycle;
                    profile.characterWidth = 8f;
                    break;

                case UIType.DropdownMenu:
                    profile.mass = 1f;
                    profile.drag = 1.8f;
                    profile.bounciness = 0.15f;
                    profile.friction = 0.5f;
                    profile.gravityScale = 1f;
                    profile.releaseVelocityMultiplier = 3f;
                    profile.dragSpeed = 12f;
                    profile.isPlatform = true;
                    profile.contextSensitive = true;
                    profile.violationBehavior = ConstraintViolationBehavior.Collapse;
                    break;

                case UIType.Checkbox:
                    profile.mass = 0.2f;
                    profile.drag = 3f;
                    profile.bounciness = 0.05f;
                    profile.friction = 0.8f;
                    profile.gravityScale = 0.5f;
                    profile.releaseVelocityMultiplier = 2f;
                    profile.dragSpeed = 20f;
                    profile.isPlatform = false;
                    profile.canBeDragged = false;
                    profile.contextSensitive = false;
                    break;
            }

            return profile;
        }

        /// <summary>
        /// 应用配置到 UIPhysicsElement
        /// </summary>
        public void ApplyTo(UI.UIPhysicsElement element)
        {
            if (element == null) return;

            element.mass = mass;
            element.drag = drag;
            element.bounciness = bounciness;
            element.friction = friction;
            element.gravityScale = gravityScale;
            element.useGravity = useGravity;
            element.isPlatform = isPlatform;
        }

        /// <summary>
        /// 应用配置到 DraggableUI
        /// </summary>
        public void ApplyTo(DragSystem.DraggableUI draggable)
        {
            if (draggable == null) return;

            draggable.canBeDragged = canBeDragged;
            draggable.dragSpeed = dragSpeed;
            draggable.releaseVelocityMultiplier = releaseVelocityMultiplier;
        }
    }
}
