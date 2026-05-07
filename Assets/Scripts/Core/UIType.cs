namespace OutOfBounds.Core
{
    /// <summary>
    /// UI 类型枚举
    /// 对应设计文档中定义的 6 种 UI 谜题角色
    /// </summary>
    public enum UIType
    {
        /// <summary>文本：轻型平台 + 语义指令（如 [SPACE]、LEVEL CLEAR）</summary>
        Text = 0,

        /// <summary>窗口：重型平台 + 压机关（如 Quest Log）</summary>
        Window = 1,

        /// <summary>血条：代价资源 + 临时踏点（如心形 HP）</summary>
        HealthBar = 2,

        /// <summary>对话气泡：文本长度生成地形 + 遮挡（如 NPC 对话框）</summary>
        DialogBubble = 3,

        /// <summary>下拉菜单：结构展开 + 父子依附（如 FILE 菜单）</summary>
        DropdownMenu = 4,

        /// <summary>复选框：状态切换 + 门禁逻辑（如权限面板）</summary>
        Checkbox = 5
    }

    /// <summary>
    /// 上下文约束类型
    /// 决定 UI 元素在什么条件下脱离上下文的规则
    /// </summary>
    public enum ContextConstraintType
    {
        /// <summary>无约束 — UI 可以随意拖动</summary>
        None = 0,

        /// <summary>区域触发器 — 离开指定 RectTransform 范围则失效</summary>
        AreaTrigger = 1,

        /// <summary>信号源 — 距离绑定 Transform 超过 maxRange 则失效</summary>
        SignalSource = 2,

        /// <summary>父级依附 — 距离父级 Transform 超过 maxRange 则断开并回收</summary>
        ParentAttachment = 3
    }

    /// <summary>
    /// 约束违规后的行为
    /// 定义 UI 元素脱离上下文后发生什么
    /// </summary>
    public enum ConstraintViolationBehavior
    {
        /// <summary>变成乱码 — 视觉损坏 + 禁用碰撞（Broken Glyph）</summary>
        BecomeBroken = 0,

        /// <summary>变透明并回收 — 渐隐消失，回到原位</summary>
        BecomeTransparent = 1,

        /// <summary>折叠 — 收起展开的结构（下拉菜单专用）</summary>
        Collapse = 2,

        /// <summary>冻结 — 停止物理运动，留在原地</summary>
        Freeze = 3,

        /// <summary>回收 — 销毁或回收到对象池</summary>
        Recycle = 4
    }
}
